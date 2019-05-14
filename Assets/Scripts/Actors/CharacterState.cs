#define DEBUG_COMBAT

using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Data;
using Assets.Scripts.Utils;
using Assets.Scripts.Utils.Debugger;
using Data;
using Spells;
using UI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;
using Logger = Assets.Scripts.Utils.Debugger.Logger;

namespace Actors
{
    public class CharacterState : MonoBehaviour
    {
        [Serializable]
        public enum NodeRole
        {
            Default = 0,
            SpellEmitter = 1,
            Head = 2,
            Chest = 3,
            Root
        }

        [Serializable]
        public class CharacterNode
        {
            public Transform Transform;
            public NodeRole Role;
        }

        public class BuffState
        {
            public Buff Buff;
            public float TimeRemaining;
            public float TickCd;
            public int Stacks;
            public CharacterState SourceCharacter;
            public Spell Spell;

            public List<Change> ActiveChanges = new List<Change>();
            public List<GameObject> TrackedObjects = new List<GameObject>();

            public BuffState(Buff buff, CharacterState sourceCharacter, int stacks, Spell spell = null)
            {
                Stacks = stacks;
                Buff = buff;
                TimeRemaining = buff.Duration;
                TickCd = 0;
                SourceCharacter = sourceCharacter;
                Spell = spell;
            }

            public void Refresh()
            {
                TimeRemaining = Buff.Duration;
                TickCd = 0;
            }
        }

        public class ItemState
        {
            public Item Item;
            public int Stacks;

            public ItemState(Item item, int stacks = 1)
            {
                Item = item;
                Stacks = stacks;
            }
        }

        public struct Change
        {
            public ModificationParameter Parameter;
            public float Amount;
        }

        public enum Team : int
        {
            Undefined = 0,
            Player,
            Enemies,
            AgainstTheWorld
        }

        [EnumFlag] public Team CurrentTeam = Team.Undefined;

        public event Action Died;
        public event Action<float> HealthChanged;
        public event Action<Item, int> OnItemPickup;
        public event Action<Spell, int> OnSpellPickup;

#if DEBUG
        public event Action<ModificationParameter, Spell, int, float> ModifierApplied;
#endif

        public CharacterConfig character;
        public CharacterNode[] Nodes;

        public IReadOnlyList<ItemState> Items => _itemStates;
        public IReadOnlyList<BuffState> Buffs => _buffStates;
        public bool IsAlive { get; private set; }

        // ========= Hit points
        private float _hp;
        private float _maxHpBase; // Default pool
        private float _maxHpFlatModSum;
        private float _maxHpMultModSum;
        public float Health => _hp;
        public float MaxHealth => (character.Health + _maxHpFlatModSum) * (1 + ELU(_maxHpMultModSum));

        // ========= Damage
        private float _dmgFlatModSum;
        private float _dmgMultModSum;
        public float Damage => character.Damage * (1 + ELU(_dmgMultModSum)) + _dmgFlatModSum;

        // ======== Evasion
        // All sources multiplicative aggregation 
        // [0, 1] range. Multiplication product of every (1 - evasion chance mod)
        private float _evasionModMulProduct = 1f;
        public float Evasion => 1 - (1 - character.Evasion) * _evasionModMulProduct;

        // ========= Speed
        private float _speedFlatModSum;
        private float _speedMultModSum;
        public float Speed => Mathf.Max((character.Speed + _speedFlatModSum) * (1 + ELU(_speedMultModSum)), 0);

        // ========= Size
        private float _sizeFlatModSum;
        private float _sizeMultModSum;

        public float Size =>
            Mathf.Clamp(
                (character.Size + _sizeFlatModSum) * (1 + ELU(_sizeMultModSum)),
                1, 10f);

        // ========= AdditionSpellStacks
        private float _assFlatModSum = 0;
        public int AdditionSpellStacks => character.AdditionalSpellStacks + Mathf.CeilToInt(_assFlatModSum);

        // ========= SpellDamage amplification
        private float _spellDamageAmpFlatModSum = 0f;
        public float SpellDamageMultiplier => 1f + ELU(character.SpellDamageAmp + _spellDamageAmpFlatModSum);

        public float DropRate => character.DropRate;
        public List<Spell> DropSpells => character.DropSpells;

        // Internal
        private AnimationController _animationController;
        private SpellbookState _spellBook;
        private SpellCaster _spellCaster;
        private readonly List<BuffState> _buffStates = new List<BuffState>();
        private readonly List<ItemState> _itemStates = new List<ItemState>();
        private Vector3 _baseScale;
#if DEBUG_COMBAT
        private Logger _combatLog;
#endif

        void Awake()
        {
#if DEBUG_COMBAT
            _combatLog = Debugger.Default.GetLogger(gameObject.name + "/StatLog", unityLog: false);
#endif
            _baseScale = transform.localScale;
            IsAlive = true;

            _spellBook = GetComponent<SpellbookState>();
            _spellCaster = GetComponent<SpellCaster>();
            _animationController = GetComponent<AnimationController>();
            _hp = character.HealthModifier * MaxHealth;

            if (CurrentTeam == Team.Undefined)
                Debug.LogError("Team not set!", this);

            transform.localScale = _baseScale * Size;
        }

        void Start()
        {
            // Add healthbar
            UIHealthBarOverlay.Instance?.Add(this);
        }

        internal void Pickup(Spell spell, int stacks)
        {
            if (!IsAlive)
                return;

            _spellBook.PlaceSpell(spell, stacks);
            OnSpellPickup?.Invoke(spell, stacks);
        }

        public void Pickup(Item item, int stacks)
        {
            if (!IsAlive)
                return;
#if DEBUG_COMBAT
            _combatLog.Log($"<b>{gameObject.name}</b> picked up item <b>{item.name}</b>");
#endif

            var state = _itemStates.FirstOrDefault(s => s.Item.Equals(item));

            if (state != null)
                state.Stacks += stacks;
            else
                _itemStates.Add(new ItemState(item, stacks));

            OnItemPickup?.Invoke(item, stacks);

            // Todo: track picked items and their stats
            foreach (var buff in item.Buffs)
                ApplyBuff(buff, this, null, stacks = 1);
        }

        public void ApplyBuff(Buff newBuff, CharacterState sourceCharacter, Spell spell, int stacks)
        {
            if (newBuff == null)
                return;

            // Try find buff state with the same buff that is going to be applied
            var existingState = _buffStates.FirstOrDefault(s => s.Buff.Equals(newBuff));
            if (existingState != null)
            {
                // State with same buff already exists
                switch (newBuff.Behaviour)
                {
                    case BuffStackBehaviour.MaxStacksOfTwo:
                        RevertBuffChanges(existingState);
                        existingState.Stacks = Mathf.Max(stacks, existingState.Stacks);
                        ApplyBuffModifiers(existingState);
                        existingState.Refresh();

                        if (newBuff.OnRefreshBuff != null)
                            foreach (var affect in newBuff.OnRefreshBuff)
                                ApplyAffect(affect, existingState);

                        break;
                    case BuffStackBehaviour.AddNewAsSeparate:
                        var s = AddBuff(newBuff, stacks, sourceCharacter, spell);
                        if (newBuff.OnApplyBuff != null)
                            foreach (var affect in newBuff.OnApplyBuff)
                                ApplyAffect(affect, s);
                        break;
                    case BuffStackBehaviour.SumStacks:
                        RevertBuffChanges(existingState);
                        existingState.Stacks += stacks;
                        ApplyBuffModifiers(existingState);
                        existingState.Refresh();
                        if (newBuff.OnRefreshBuff != null)
                            foreach (var affect in newBuff.OnRefreshBuff)
                                ApplyAffect(affect, existingState);

                        break;
                    case BuffStackBehaviour.Discard:
                        // Do nothing. newBuff wont be added
                        break;
                }
#if DEBUG_COMBAT
                _combatLog.Log(
                    $"<b>{gameObject.name}</b> reapplied buff <b>{newBuff.name}</b> with <b>{newBuff.Behaviour}</b> behaviour. Stack after reapplied: <b>{existingState.Stacks}</b>");
#endif
            }
            else
            {
                // The buff is completely new. So create and store new buffstate and apply all effects
                var newBuffState = AddBuff(newBuff, stacks, sourceCharacter, spell);
                if (newBuff.OnApplyBuff != null)
                    foreach (var affect in newBuff.OnApplyBuff)
                        ApplyAffect(affect, newBuffState);
            }
        }

        private void RevertBuffChanges(BuffState s)
        {
            if (s.ActiveChanges == null) return;
            for (var i = s.ActiveChanges.Count - 1; i >= 0; i--)
            {
                RevertChange(s.ActiveChanges[i]);
                s.ActiveChanges.RemoveAt(i);
            }
        }

        private void ApplyBuffModifiers(BuffState state)
        {
            if (state.Buff.Modifiers == null) return;
            foreach (var mod in state.Buff.Modifiers)
            {
                ApplyModifier(
                    mod,
                    state.Stacks,
                    state.SourceCharacter,
                    state.Spell,
                    out var change);
                state.ActiveChanges.Add(new Change
                {
                    Parameter = mod.Parameter,
                    Amount = change
                });
            }
        }

        private BuffState AddBuff(Buff buff, int stacks, CharacterState sourceCharacter, Spell spell)
        {
            var s = new BuffState(buff, sourceCharacter, stacks, spell);
            _buffStates.Add(s);

#if DEBUG_COMBAT
            _combatLog.LogFormat("<b>{0}</b> received new buff <b>{1}</b> with <b>{2}</b> stacks",
                gameObject.name,
                buff.name,
                stacks);
#endif
            ApplyBuffModifiers(s);
            return s;
        }


        public void ApplyAffect(Affect affect, BuffState buffState)
        {
            // Apply affect modifiers
            if (affect.ApplyModifier != null)
                ApplyModifier(affect.ApplyModifier, buffState.Stacks, buffState.SourceCharacter, buffState.Spell,
                    out _);

            // Cast affect spells
            if (affect.CastSpell != null && affect.CastSpell.Spell != null)
            {
                TargetInfo tgt = null;
                var spellStacks = 1;
                switch (affect.CastSpell.Target)
                {
                    case Affect.SpellCastInfo.SpellTarget.Source:
                        tgt = TargetInfo.Create(buffState.SourceCharacter);
                        break;
                    case Affect.SpellCastInfo.SpellTarget.Self:
                        tgt = TargetInfo.Create(this);
                        break;
                    case Affect.SpellCastInfo.SpellTarget.CurrentSpellEmitter:
                        tgt = new TargetInfo {Transform = GetNodeTransform(NodeRole.SpellEmitter)};
                        break;
                    default:
                        break;
                }

                switch (affect.CastSpell.SpellStacks)
                {
                    case Affect.StacksBehaviour.Inherit:
                        spellStacks = buffState.Stacks;
                        break;
                    case Affect.StacksBehaviour.Override:
                        spellStacks = affect.CastSpell.StacksOverride;
                        break;
                }

                _spellCaster.CastSpell(
                    affect.CastSpell.Spell,
                    spellStacks,
                    new SpellTargets(TargetInfo.Create(this,
                        GetNodeTransform(NodeRole.SpellEmitter)), tgt),
                    null,
                    null);
            }

            if (affect.SpawnObject != null && affect.SpawnObject.Prefab != null)
            {
                var spawnAt = GetNodeTransform(affect.SpawnObject.CharacterNode);
                GameObject go;
                if (affect.SpawnObject.AttachToTransform)
                {
                    go = GameObject.Instantiate(affect.SpawnObject.Prefab, spawnAt.transform.position,
                        spawnAt.transform.rotation);
                    // TODO: Figure out scale
                    go.transform.SetParent(spawnAt);
                }
                else
                    go = GameObject.Instantiate(affect.SpawnObject.Prefab, spawnAt.position, Quaternion.identity);

                if (affect.SpawnObject.AutoDestroyAfterBuff)
                    buffState.TrackedObjects.Add(go);
            }

            if (affect.ApplyBuff.Buff != null)
            {
                CharacterState target;
                int stacks = 1;

                switch (affect.ApplyBuff.Target)
                {
                    case Affect.ApplyBuffInfo.BuffTarget.Self:
                        target = this;
                        break;
                    case Affect.ApplyBuffInfo.BuffTarget.SpellSource:
                        target = buffState.SourceCharacter;
                        break;
                    default:
                        target = null;
                        break;
                }

                switch (affect.ApplyBuff.StacksBehaviour)
                {
                    case Affect.StacksBehaviour.Inherit:
                        stacks = buffState.Stacks;
                        break;
                    case Affect.StacksBehaviour.Override:
                        stacks = affect.ApplyBuff.StacksOverride;
                        break;
                }

                if (target != null)
                {
                    target.ApplyBuff(affect.ApplyBuff.Buff, this, null, stacks);
                }
            }
        }

        public void ApplyModifier(
            Modifier modifier,
            int stacks,
            CharacterState source,
            Spell spell,
            out float change)
        {
            ApplyModifier(
                modifier.Parameter,
                modifier.Value,
                stacks,
                modifier.EffectiveStacks,
                source,
                spell,
                out change);
        }

        public void ApplyModifier(
            ModificationParameter parameter,
            float amount,
            int stacks,
            float effectiveStacks,
            CharacterState sourceCharacter,
            Spell spell,
            out float actualChange)
        {
            actualChange = 0f;
            if (parameter == ModificationParameter.None)
                return;

            var hpFraction = _hp / MaxHealth;
            switch (parameter)
            {
                case ModificationParameter.HpFlat:
                    actualChange = UpdateHp(_hp + StackedModifier(amount, stacks, effectiveStacks), sourceCharacter,
                        spell);
                    break;
                case ModificationParameter.HpMult:
                    actualChange = UpdateHp(_hp * (1 + StackedModifier(amount, stacks, effectiveStacks)),
                        sourceCharacter, spell);
                    break;
                case ModificationParameter.MaxHpFlat:
                    actualChange = StackedModifier(amount, stacks, effectiveStacks);
                    _maxHpFlatModSum += actualChange;
                    _hp = hpFraction * MaxHealth;
                    break;
                case ModificationParameter.MaxHpMult:
                    actualChange = StackedModifier(amount, stacks, effectiveStacks);
                    _maxHpMultModSum += actualChange;
                    _hp = hpFraction * MaxHealth;
                    break;
                case ModificationParameter.DmgFlat:
                    actualChange = StackedModifier(amount, stacks, effectiveStacks);
                    _dmgFlatModSum += actualChange;
                    break;
                case ModificationParameter.DmgMult:
                    actualChange = StackedModifier(amount, stacks, effectiveStacks);
                    _dmgMultModSum += actualChange;
                    break;
                case ModificationParameter.EvasionChanceFlat:
                    // TODO: FIX STACKING
                    actualChange = Mathf.Pow(1 - amount, stacks);
                    _evasionModMulProduct *= actualChange;
                    break;
                case ModificationParameter.CritChanceFlat:
                    break;
                case ModificationParameter.SpeedFlat:
                    actualChange = StackedModifier(amount, stacks, effectiveStacks);
                    _speedFlatModSum += actualChange;
                    break;
                case ModificationParameter.SpeedMult:
                    actualChange = StackedModifier(amount, stacks, effectiveStacks);
                    _speedMultModSum += actualChange;
                    break;
                case ModificationParameter.SizeFlat:
                    actualChange = StackedModifier(amount, stacks, effectiveStacks);
                    _sizeFlatModSum += actualChange;
                    break;
                case ModificationParameter.SizeMult:
                    actualChange = StackedModifier(amount, stacks, effectiveStacks);
                    _sizeMultModSum += actualChange;
                    break;
                case ModificationParameter.SpellStacksFlat:
                    actualChange = StackedModifier(amount, stacks, effectiveStacks);
                    _assFlatModSum += actualChange;
                    break;
                case ModificationParameter.SpellDamageAmpFlat:
                    actualChange = StackedModifier(amount, stacks, effectiveStacks);
                    _spellDamageAmpFlatModSum += actualChange;
                    break;
                default:
                    break;
            }

#if DEBUG_COMBAT
            _combatLog.Log(
                $"<b>{gameObject.name}</b> received modifier <b>{parameter}</b> with amount <b>{amount}</b>. Actual change: <b>{actualChange}</b>" +
                $" Stacks: <b>{stacks}</b>. EffectiveStacks: <b>{effectiveStacks}</b>");
#endif

            switch (parameter)
            {
                case ModificationParameter.SizeFlat:
                case ModificationParameter.SizeMult:
                    transform.localScale = _baseScale * Size;
                    break;
            }

#if DEBUG
            ModifierApplied?.Invoke(parameter, spell, stacks, actualChange);
#endif
        }

        private void RevertChange(Change change)
        {
            // TODO: Revert EVASION and CRIT properly by dividing and not subtracting
            // TODO: refactor this hack
            switch (change.Parameter)
            {
                case ModificationParameter.EvasionChanceFlat:
                    _evasionModMulProduct /= change.Amount;
                    break;
                default:
                    ApplyModifier(change.Parameter,
                        -change.Amount,
                        1,
                        1,
                        this,
                        null,
                        out _);
                    break;
            }
        }

        private float UpdateHp(float targetHp, CharacterState sourceCharacter, Spell spell)
        {
            targetHp = Mathf.Clamp(targetHp, -1, MaxHealth);
            var delta = targetHp - _hp;

            // If taking damage
            if (delta < 0)
            {
                if (spell != null)
                {
                    if(sourceCharacter != null)
                        delta *= sourceCharacter.SpellDamageMultiplier;
                    targetHp = Mathf.Clamp(_hp + delta, -1, MaxHealth);
                    delta = targetHp - _hp;

#if DEBUG_COMBAT
                    if(sourceCharacter != null)
                    _combatLog.Log(
                        $"Receiving in total <b>{-delta}</b> spell multiplied ({100 * sourceCharacter.SpellDamageMultiplier}%) damage from <b>{sourceCharacter.name}</b> " +
                        $"from spell <b>{spell.name}</b>");
#endif
                }

                if (sourceCharacter != null && spell != null)
                {
                    var lifesteal = -delta * spell.LifeSteal;
                    if (lifesteal > 0)
                    {
#if DEBUG_COMBAT
                        _combatLog.Log(
                            $"Returning <b>{-delta * spell.LifeSteal}</b> hp back to <b>{sourceCharacter.name}</b> " +
                            $"because spell <b>{spell.name}</b> has <b>{100 * spell.LifeSteal}%</b> lifesteal");
#endif
                        sourceCharacter.ApplyModifier(ModificationParameter.HpFlat,
                            lifesteal,
                            1,
                            1,
                            this,
                            spell,
                            out _);
                    }
                }

                if (targetHp <= 0)
                    HandleDeath();
                else
                {
                    _animationController.PlayHitImpactAnimation();
                }
            }

            // Change
            _hp = targetHp;
            HealthChanged?.Invoke(_hp);
            return delta;
        }

        public bool SpendCurrency(float amount)
        {
            if (!IsAlive)
                return false;

            if (amount > MaxHealth)
            {
                Debug.LogWarningFormat("Cant spend currency. Currency={0}, Trying to spend = {1}", MaxHealth, amount);
                return false;
            }

            ApplyModifier(
                ModificationParameter.MaxHpFlat,
                -amount,
                1,
                1,
                this,
                null,
                out _);
            return true;
        }

        void Update()
        {
            if (!IsAlive)
                return;

#if DEBUG
            if (gameObject.CompareTag(Common.Tags.Player))
                DisplayState();
#endif

            // Update buffs
            for (var i = _buffStates.Count - 1; i >= 0; i--)
            {
                var buffState = _buffStates[i];
                buffState.TickCd -= Time.deltaTime;
                buffState.TimeRemaining -= Time.deltaTime;

                // Buff tick
                if (buffState.TickCd < 0)
                {
                    if (buffState.Buff.OnTickBuff != null)
                        foreach (var affect in buffState.Buff.OnTickBuff)
                            ApplyAffect(affect, buffState);

                    buffState.TickCd = buffState.Buff.TickCooldown;
                }

                // Buff remove
                if (buffState.TimeRemaining < 0)
                {
                    if (buffState.Buff.OnRemove != null)
                        foreach (var affect in buffState.Buff.OnRemove)
                            ApplyAffect(affect, buffState);


                    if (buffState.ActiveChanges != null)
                        foreach (var change in buffState.ActiveChanges)
                            RevertChange(change);

                    if (buffState.TrackedObjects != null)
                        foreach (var trackedObject in buffState.TrackedObjects)
                            Destroy(trackedObject);

                    _buffStates.RemoveAt(i);
                }
            }
        }

#if DEBUG
        void DisplayState()
        {
            var goName = gameObject.name;
            var buffs = goName + "/Buffs states/";
            foreach (var buffState in _buffStates)
            {
                var path = buffs + buffState.Buff.name;
                Debugger.Default.Display(path + "/Stacks", buffState.Stacks);
                Debugger.Default.Display(path + "/Tick CD", buffState.TickCd);
                Debugger.Default.Display(path + "/Time remaining", buffState.TimeRemaining);
                foreach (var change in buffState.ActiveChanges)
                {
                    Debugger.Default.Display(path + "/Changes/" + change.Parameter, change.Amount);
                }
            }

            Debugger.Default.Display(goName + "/Health", Health);
            Debugger.Default.Display(goName + "/MaxHealth", MaxHealth);
            Debugger.Default.Display(goName + "/MaxHealth/MultModSum", _maxHpMultModSum);
            Debugger.Default.Display(goName + "/MaxHealth/FlatModSum", _maxHpFlatModSum);
            Debugger.Default.Display(goName + "/Speed", Speed);
            Debugger.Default.Display(goName + "/Speed/FlatModSum", _speedFlatModSum);
            Debugger.Default.Display(goName + "/Speed/MultModSum", _speedMultModSum);
            Debugger.Default.Display(goName + "/Damage", Damage);
            Debugger.Default.Display(goName + "/Damage/FlatModSum", _dmgFlatModSum);
            Debugger.Default.Display(goName + "/Damage/MultModSum", _dmgMultModSum);
            Debugger.Default.Display(goName + "/Size", Size);
            Debugger.Default.Display(goName + "/Size/FlatModSum", _sizeFlatModSum);
            Debugger.Default.Display(goName + "/Size/MultModSum", _sizeMultModSum);
            Debugger.Default.Display(goName + "/Evasion", Evasion);
            Debugger.Default.Display(goName + "/Evasion/MultProd", _evasionModMulProduct);
            Debugger.Default.Display(goName + "/SpellDamageMultiplier", SpellDamageMultiplier);
            Debugger.Default.Display(goName + "/SpellDamageMultiplier/AmpFlatModSum", _spellDamageAmpFlatModSum);
        }
#endif

        void HandleDeath()
        {
            IsAlive = false;
            if (DropSpells.Count > 0 && Random.value < character.DropRate)
            {
                var spell = RandomUtils.Choice(DropSpells);
                if (spell != null)
                {
                    DroppedSpell.InstantiateDroppedSpell(spell, GetNodeTransform(NodeRole.Chest).position);
                }
            }

            _animationController.PlayDeathAnimation();
            Debug.Log($"<b>{gameObject.name}</b> died");
            Died?.Invoke();
        }

        public void ReceiveDamage(CharacterState sourceCharacter, float amount, Spell spell)
        {
            if (IsAlive)
            {
                if (Random.value > Evasion)
                    UpdateHp(_hp - amount, sourceCharacter, spell);
                else
                    _animationController.PlayEvasionEffect(GetNodeTransform(NodeRole.Chest));
            }
        }

        internal void ApplySpell(CharacterState owner, ISpellContext spellContext)
        {
            if (!IsAlive)
                return;

            var currentSub = spellContext.CurrentSubSpell;
#if DEBUG_COMBAT
            _combatLog.Log($"<b>{gameObject.name}</b> received spell cast <b>{spellContext.Spell.name}</b>"
                           + $" (sub: <b>{currentSub.name}</b>) with <b>{spellContext.Stacks}</b>");
#endif

            Assert.IsTrue(SpellCaster.IsEnemy(owner, this, currentSub.AffectedTarget));

            {
                foreach (var buff in spellContext.CurrentSubSpell.Buffs)
                    ApplyBuff(buff, owner, spellContext.Spell, spellContext.Stacks);
            }
        }


        public Transform GetNodeTransform(NodeRole role = NodeRole.Default)
        {
            if (role == NodeRole.Root)
                return transform;

            if (Nodes == null || Nodes.Length == 0)
                return transform;

            var node = Nodes.FirstOrDefault(n => n.Role == role);
            if (node != null)
                return node.Transform;

            return Nodes[0].Transform;
        }

        /// <summary>
        /// Exponential linear unit. Used for multiplier modifiers to not go pass the -1 on the left side
        /// </summary>
        /// <param name="x"></param>
        /// <param name="alpha"></param>
        /// <returns>Returns the value in range [-1, +inf) </returns>
        public static float ELU(float x, float alpha = 1f)
        {
            if (x >= 0)
                return x;
            return alpha * (Mathf.Exp(x) - 1);
        }

        public static float StackedModifier(float modifierValue, float stacks, float effectiveStacks)
        {
            // DO NOT CHANGE THIS! unless you do not know what you are doing!
            return modifierValue * stacks / ((stacks - 1) * (1 - 0.3f) / (Mathf.Max(1, effectiveStacks)) + 1);
        }
    }
}