#define DEBUG_COMBAT

using System;
using System.Collections.Generic;
using System.Linq;
using Attributes;
using Data;
using Spells;
using UI;
using UnityEngine;
using Utils;
using Utils.Debugger;
using Random = UnityEngine.Random;
using Logger = Utils.Debugger.Logger;

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
            public ISpellContext SpellContext;
            public SpellTargets SpellTargets;

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

            

            public void RefreshTime()
            {
                TimeRemaining = Buff.Duration;
                TickCd = 0;
            }
        }

        public class ItemState
        {
            public Item Item;
            public int Stacks;

            public ItemState(Item item, int stacks)
            {
                Item = item;
                Stacks = stacks;
            }
        }

        public struct Change
        {
            public ModificationParameter Parameter;
            public float Amount;

            public Change Inverse()
            {
                switch (Parameter)
                {
                    // Multiplicative changes
                    case ModificationParameter.EvasionChanceFlat:
                    case ModificationParameter.CritChanceFlat:
                        return new Change
                        {
                            Parameter = Parameter,
                            Amount = 1 / Amount,
                        };
                    // Additive changes
                    default:
                        return new Change
                        {
                            Parameter = Parameter,
                            Amount = -Amount
                        };
                }
            }

            public static Change operator+(Change a, Change b)
            {
                if(!a.Parameter.Equals(b.Parameter))
                    throw new InvalidOperationException();

                switch (a.Parameter)
                {
                    // Multiplicative changes
                    case ModificationParameter.EvasionChanceFlat:
                    case ModificationParameter.CritChanceFlat:
                        return new Change
                        {
                            Parameter = a.Parameter,
                            Amount = a.Amount * b.Amount,
                        };
                    // Additive changes
                    default:
                        return new Change
                        {
                            Parameter = a.Parameter,
                            Amount = a.Amount + b.Amount
                        };
                }
            }
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
        public float MaxHealth => (character.Health + _maxHpFlatModSum) * (1 + MathUtils.ELU(_maxHpMultModSum));

        // ========= Damage
        private float _dmgFlatModSum;
        private float _dmgMultModSum;
        public float Damage => character.Damage * (1 + MathUtils.ELU(_dmgMultModSum)) + _dmgFlatModSum;

        // ======== Evasion
        // All sources multiplicative aggregation 
        // [0, 1] range. Multiplication product of every (1 - evasion chance mod)
        private float _evasionModMulProduct = 1f;
        public float Evasion => 1 - (1 - character.Evasion) * _evasionModMulProduct;

        // ========= Speed
        private float _speedFlatModSum;
        private float _speedMultModSum;
        public float Speed => Mathf.Max((character.Speed + _speedFlatModSum) * (1 + MathUtils.ELU(_speedMultModSum)), 0);

        // ========= Size
        private float _sizeFlatModSum;
        private float _sizeMultModSum;

        public float Size =>
            Mathf.Clamp(
                (character.Size + _sizeFlatModSum) * (1 + MathUtils.ELU(_sizeMultModSum)),
                1, 10f);

        // ========= AdditionSpellStacks
        private float _assFlatModSum = 0;
        public int AdditionSpellStacks => character.AdditionalSpellStacks + Mathf.CeilToInt(_assFlatModSum);

        // ========= SpellDamage amplification
        private float _spellDamageAmpFlatModSum = 0f;
        public float SpellDamageMultiplier => 1f + MathUtils.ELU(character.SpellDamageAmp + _spellDamageAmpFlatModSum);

        // ========= Stun
        private float _stunScale = 0f;
        public float StunScale => _stunScale;
        public bool IsStunned => _stunScale > 1f;
        public bool IsControllable => IsAlive && !IsStunned;

        public float DropRate => character.DropRate;
        public List<Spell> DropSpells => character.DropSpells;

        // Internal
        private AnimationController _animationController;
        private SpellbookState _spellBook;
        private SpellCaster _spellCaster;
        private MovementController _movement;
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

            _movement = GetComponent<MovementController>();
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
            // Register healthbar
            UIHealthBarOverlay.Instance?.Add(this);
        }

        public void Pickup(Spell spell, int stacks)
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

        public void ApplyBuff(
            Buff newBuff, 
            CharacterState sourceCharacter, 
            Spell spell, 
            int stacks, 
            ISpellContext spellContext = null,
            SpellTargets targets = null)
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
                        existingState.Stacks = Mathf.Max(stacks, existingState.Stacks);
                        ApplyBuffModifiers(existingState);
                        existingState.RefreshTime();
                        existingState.SpellContext = spellContext;
                        existingState.SpellTargets = targets;
                        HandleBuffEvents(existingState, BuffEventType.OnRefresh);
                        break;
                    case BuffStackBehaviour.AddNewAsSeparate:
                        AddNewBuff(newBuff, stacks, sourceCharacter, spell, spellContext, targets);
                        break; 
                    case BuffStackBehaviour.SumStacks:
                        existingState.Stacks += stacks;
                        ApplyBuffModifiers(existingState);
                        existingState.SpellContext = spellContext;
                        existingState.SpellTargets = targets;
                        existingState.RefreshTime();
                        HandleBuffEvents(existingState, BuffEventType.OnRefresh);
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
                // The buff is completely new. So create and store new BuffState and apply all effects
                AddNewBuff(newBuff, stacks, sourceCharacter, spell, spellContext, targets);
            }
        }

        private void ApplyBuffModifiers(BuffState state)
        {
            if (state.Buff.Modifiers == null) return;

            var oldChanges = state.ActiveChanges.ToArray();
            state.ActiveChanges.Clear();

            // For each modifier in temporary buff modifiers
            // Apply each one and save actual change to revert it on buff removal
            foreach (var mod in state.Buff.Modifiers)
            {
                var change = ApplyModifier(
                    mod.Parameter, 
                    mod.Value, state.Stacks, 
                    mod.EffectiveStacks, 
                    state.SourceCharacter, 
                    state.Spell,
                    false);

                // If there is an actual change
                var activeChange = oldChanges.FirstOrDefault(ch => ch.Parameter.Equals(mod.Parameter));
                if (activeChange.Parameter != ModificationParameter.None)
                    ApplyChange(change + activeChange.Inverse());
                else
                    ApplyChange(change);
                
                state.ActiveChanges.Add(change);
            }
        }

        private BuffState AddNewBuff(
            Buff buff, 
            int stacks, 
            CharacterState sourceCharacter, 
            Spell spell, 
            ISpellContext spellContext, 
            SpellTargets targets)
        {
            var buffState = new BuffState(buff, sourceCharacter, stacks, spell)
            {
                SpellContext = spellContext,
                SpellTargets = targets
            };
            _buffStates.Add(buffState);

#if DEBUG_COMBAT
            _combatLog.LogFormat("<b>{0}</b> received new buff <b>{1}</b> with <b>{2}</b> stacks",
                gameObject.name,
                buff.name,
                stacks);
#endif
            ApplyBuffModifiers(buffState);
            HandleBuffEvents(buffState, BuffEventType.OnApply);
            return buffState;
        }

        private void HandleBuffEvents(BuffState buffState, BuffEventType eventType)
        {
            if (buffState.Buff.AffectByEventType.TryGetValue(eventType, out var affects))
            {
                foreach (var affect in affects)
                {
                    ApplyAffect(buffState, affect);
                }
            }
        }

        private void ApplyAffect(BuffState buffState, Affect affect)
        {
            // Apply affect modifiers
            if (affect.Type == Affect.AffectType.ApplyModifier)
                ApplyModifier(
                    affect.ApplyModifier.Parameter, 
                    affect.ApplyModifier.Value, 
                    buffState.Stacks, 
                    affect.ApplyModifier.EffectiveStacks, 
                    buffState.SourceCharacter, 
                    buffState.Spell);

            // Cast affect spells
            if (affect.Type == Affect.AffectType.CastSpell)
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

            if (affect.Type == Affect.AffectType.SpawnObject)
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

            if (affect.Type == Affect.AffectType.ApplyBuff)
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

            if (affect.Type == Affect.AffectType.Move)
            {
                TargetInfo moveTarget;
                switch (affect.Move.RelativeTo)
                {
                    case Affect.MoveInfo.MoveRelation.SourceCharacter:
                        moveTarget = TargetInfo.Create(buffState.SourceCharacter);
                        break;
                    case Affect.MoveInfo.MoveRelation.SpellSource:
                        moveTarget = TargetInfo.Create(buffState.SpellContext.InitialSource);
                        break;
                    case Affect.MoveInfo.MoveRelation.ChanellingTarget:
                        moveTarget = buffState.SpellContext?.ChannelingInfo?.GetNewTarget();
                        break;
                    case Affect.MoveInfo.MoveRelation.SpellTargetSource:
                        moveTarget = buffState.SpellTargets?.Source;
                        break;
                    case Affect.MoveInfo.MoveRelation.SpellTarget:
                        moveTarget = buffState.SpellTargets?.Destinations[0];
                        break;
                    default:
                    case Affect.MoveInfo.MoveRelation.LookDirection:
                        moveTarget = TargetInfo.Create(null, null, transform.position + transform.forward * 10);
                        break;
                }

                if (moveTarget != null)
                {
                    _movement?.ForceMove(
                        affect.Move.Type,
                        moveTarget,
                        affect.Move.Speed.GetValue(buffState.Stacks),
                        affect.Move.MovementDuration.GetValue(buffState.Stacks),
                        affect.Move.BreakOnDestination,
                        affect.Move.MaxDistanceFromOrigin.GetValue(buffState.Stacks));
                }
                else
                {
                    Debug.LogError("Movement target is null", this);
                }
            }
        }

        public Change ApplyModifier(
            ModificationParameter parameter,
            float amount,
            int stacks,
            float effectiveStacks,
            CharacterState sourceCharacter,
            Spell spell, 
            bool applyChange=true)
        {
            // First - calculate an actual change of modifier
            // using all spell amplification things, evasions and other stuff
            var change = CalcChange(parameter, amount, stacks, effectiveStacks, sourceCharacter, spell);

#if DEBUG_COMBAT
            _combatLog.Log(
                $"<b>{gameObject.name}</b> received modifier <b>{parameter}</b> with amount <b>{amount}</b>. Actual change: <b>{change.Amount}</b>" +
                $" Stacks: <b>{stacks}</b>. EffectiveStacks: <b>{effectiveStacks}</b>");
#endif

            if (applyChange)
            {
#if DEBUG
                ModifierApplied?.Invoke(parameter, spell, stacks, change.Amount);
#endif
                // Apply an actual change
                ApplyChange(change);
            }

            return change;
        }

        private Change CalcChange(ModificationParameter parameter,
            float amount,
            int stacks,
            float effectiveStacks,
            CharacterState sourceCharacter,
            Spell spell)
        {
            // Zero-change
            if (parameter == ModificationParameter.None || Mathf.Abs(amount) < 1e-6)
                return new Change
                {
                    Parameter = ModificationParameter.None,
                    Amount = 0
                };

            if (parameter == ModificationParameter.HpFlat || parameter == ModificationParameter.HpMult)
            {
                var targetHp = _hp;
                if (parameter == ModificationParameter.HpFlat)
                    targetHp += MathUtils.StackedModifier(amount, stacks, effectiveStacks);
                if (parameter == ModificationParameter.HpMult)
                    targetHp *= 1 + MathUtils.StackedModifier(amount, stacks, effectiveStacks);
                targetHp = Mathf.Min(targetHp, MaxHealth);
                var delta = targetHp - _hp;
                // If taking damage
                if (delta < 0)
                {
                    // If the damage comes from spell, amplify it buy character source damage multiplier
                    if (spell != null && sourceCharacter != null)
                    {
                        // Damage amplification
                        delta *= sourceCharacter.SpellDamageMultiplier;
#if DEBUG_COMBAT
                        _combatLog.Log(
                            $"Receiving in total <b>{-delta}</b> spell multiplied ({100 * sourceCharacter.SpellDamageMultiplier}%) damage from <b>{sourceCharacter.name}</b> " +
                            $"from spell <b>{spell.name}</b>");
#endif

                        // Lifesteal
                        // TODO: Shouldn't we apply lifesteal afterwards?
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
                                spell);
                        }
                    }
                }

                // Notice that we are returning HpFlat even if the original modifier was HpMult.
                // Because there was some additional modifer applied to damage (if any)
                return new Change
                {
                    Parameter = ModificationParameter.HpFlat,
                    Amount = delta
                };
            }

            switch (parameter)
            {                
                case ModificationParameter.CritChanceFlat:
                case ModificationParameter.EvasionChanceFlat:
                    return new Change
                    {
                        Parameter = parameter,
                        Amount = Mathf.Pow(1 - amount, stacks)
                    };
            }

            // Default change computation for stats like speed and other relatively simple stats
            return new Change
            {
                Parameter = parameter,
                Amount = MathUtils.StackedModifier(amount, stacks, effectiveStacks)
            };
        }

        private void ApplyChange(Change change)
        {
            // Zero-change
            if(change.Parameter == ModificationParameter.None || Mathf.Abs(change.Amount) < 1e-6)
                return;

            var oldHp = _hp;
            var hpFraction = _hp / MaxHealth;
            switch (change.Parameter)
            {
                case ModificationParameter.HpFlat:
                    _hp = Mathf.Min(_hp + change.Amount, MaxHealth);
                    break;
                case ModificationParameter.HpMult:
                    _hp = Mathf.Min(_hp * (1 + change.Amount), MaxHealth);
                    break;
                case ModificationParameter.MaxHpFlat:
                    _maxHpFlatModSum += change.Amount;
                    _hp = hpFraction * MaxHealth;
                    break;
                case ModificationParameter.MaxHpMult:
                    _maxHpMultModSum += change.Amount;
                    _hp = hpFraction * MaxHealth;
                    break;
                case ModificationParameter.DmgFlat:
                    _dmgFlatModSum += change.Amount;
                    break;
                case ModificationParameter.DmgMult:
                    _dmgMultModSum += change.Amount;
                    break;
                case ModificationParameter.EvasionChanceFlat:
                    _evasionModMulProduct *= change.Amount;
                    break;
                case ModificationParameter.CritChanceFlat:
                    throw new NotImplementedException();
                case ModificationParameter.SpeedFlat:
                    _speedFlatModSum += change.Amount;
                    break;
                case ModificationParameter.SpeedMult:
                    _speedMultModSum += change.Amount;
                    break;
                case ModificationParameter.SizeFlat:
                    _sizeFlatModSum += change.Amount;
                    break;
                case ModificationParameter.SizeMult:
                    _sizeMultModSum += change.Amount;
                    break;
                case ModificationParameter.SpellStacksFlat:
                    _assFlatModSum += change.Amount;
                    break;
                case ModificationParameter.SpellDamageAmpFlat:
                    _spellDamageAmpFlatModSum += change.Amount;
                    break;
                case ModificationParameter.Stun:
                    _stunScale += change.Amount;
                    break;
            }
            
            switch (change.Parameter)
            {
                // Update transform scale if changed
                case ModificationParameter.SizeFlat:
                case ModificationParameter.SizeMult:
                    transform.localScale = _baseScale * Size;
                    break;
                // Process hp change events
                case ModificationParameter.HpFlat:
                case ModificationParameter.HpMult:
                case ModificationParameter.MaxHpMult:
                case ModificationParameter.MaxHpFlat:
                    // If taking damage
                    if (_hp < oldHp)
                    {
                        if (_hp < 0)
                            HandleDeath();
                        else
                            _animationController.PlayHitImpactAnimation();
                    }
                    
                    HealthChanged?.Invoke(_hp);
                    break;
            }
        }
        
        public bool SpendCurrency(float amount)
        {
            if (!IsAlive)
            {
                // Dead character can't spend currency
                return false;
            }

            // If after spending we will have less than 1 than the action is illegal
            if (MaxHealth - amount < 1f)
            {
                UIInvalidAction.Instance?.Show(UIInvalidAction.InvalidAction.NotEnoughBlood);
                Debug.LogWarningFormat("Cant spend currency. Currency={0}, Trying to spend = {1}", MaxHealth, amount);
                return false;
            }

            ApplyModifier(
                ModificationParameter.MaxHpFlat,
                -amount,
                1,
                1,
                this,
                null);
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
                    HandleBuffEvents(buffState, BuffEventType.OnTick);
                    buffState.TickCd = buffState.Buff.TickCooldown;
                }

                // Buff remove
                if (buffState.TimeRemaining < 0)
                {
                    HandleBuffEvents(buffState, BuffEventType.OnRemove);
                    if (buffState.ActiveChanges != null)
                        foreach (var change in buffState.ActiveChanges)
                            ApplyChange(change.Inverse());

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

        public void ReceiveMeleeDamage(float amount)
        {
            if (IsAlive)
            {
                // ToDo: remove evasion from here
                if (Random.value > Evasion)
                    ApplyChange(new Change
                    {
                        Parameter = ModificationParameter.HpFlat,
                        Amount = -amount
                    });
                else
                    _animationController.PlayEvasionEffect(GetNodeTransform(NodeRole.Chest));
            }
        }

        internal void ApplySpell(ISpellContext spellContext, SpellTargets targets)
        {
            if (!IsAlive)
                return;

            var currentSub = spellContext.CurrentSubSpell;
#if DEBUG_COMBAT
            _combatLog.Log($"<b>{gameObject.name}</b> received spell cast <b>{spellContext.Spell.name}</b>"
                           + $" (sub: <b>{currentSub.name}</b>) with <b>{spellContext.Stacks}</b>");
#endif
            
            // Apply SubSpell payload
            foreach (var buff in spellContext.CurrentSubSpell.Buffs)
            {
                //ApplyBuff(buff, spellContext.InitialSource, spellContext.Spell, spellContext.Stacks);
                ApplyBuff(buff, 
                    spellContext.InitialSource, 
                    spellContext.Spell, 
                    spellContext.Stacks, 
                    spellContext,
                    targets);
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
    }
}