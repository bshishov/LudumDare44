﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Data;
using System.Linq;
using Random = UnityEngine.Random;
using System;
using Actors;
using Assets.Scripts;
using Assets.Scripts.Utils;
using Assets.Scripts.Utils.Debugger;
using Spells;
using UnityEngine.Assertions;
using Logger = Assets.Scripts.Utils.Debugger.Logger;

public class CharacterState : MonoBehaviour
{
    [Serializable]
    public enum NodeRole
    {
        Default = 0,
        SpellEmitter = 1,
        Head = 2,
        Chest = 3
    }

    [Serializable]
    public class CharacterNode
    {
        public Transform Transform;
        public NodeRole Role;
    }

    class BuffState
    {
        public Buff Buff;
        public float TimeRemaining;
        public float TickCd;
        public int Stacks = 1;

        public List<Change> ActiveChanges = new List<Change>();

        public BuffState(Buff buff, int stacks = 1)
        {
            Stacks = 1;
            Buff = buff;
            TimeRemaining = buff.Duration;
            TickCd = 0;
        }

        public void Refresh()
        {
            TimeRemaining = Buff.Duration;
            TickCd = 0;
        }
    }

    struct Change
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

    [EnumFlag]
    public Team CurrentTeam = Team.Undefined;

    public event Action OnDeath;
    public CharacterConfig character;
    public CharacterNode[] Nodes;
   
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
    private float _assFlatMod = 0;
    public int AdditionSpellStacks => character.AdditionalSpellStacks + Mathf.CeilToInt(_assFlatMod);
    public float DropRate => character.DropRate;
    public List<Spell> DropSpells => character.DropSpells;

    // Internal
    private float _timeBeforeNextAttack;
    private AnimationController _animationController;
    private SpellbookState _spellBook;
    private readonly List<BuffState> _states = new List<BuffState>();
    private Vector3 _baseScale;
    private Logger _combatLog;

    void Start()
    {
        _combatLog = Debugger.Default.GetLogger(gameObject.name + "/StatLog", unityLog:false);
        _baseScale = transform.localScale;
        IsAlive = true;

        _spellBook = GetComponent<SpellbookState>();
        _animationController = GetComponent<AnimationController>();
        _timeBeforeNextAttack = 0f;
        _hp = character.HealthModifier * MaxHealth;

        if (CurrentTeam == Team.Undefined)
            Debug.LogError("Team not set!", this);

        transform.localScale = _baseScale * Size;
    }

    public bool CanDealDamage()
    {
        if (!IsAlive)
            return false;

        if (_timeBeforeNextAttack > character.AttackCooldown)
        {
            _timeBeforeNextAttack = 0f;
            return true;
        }
        
        return false;
    }

    internal void Pickup(Spell spell)
    {
        if (!IsAlive)
            return;
        
        _spellBook.PlaceSpell(spell);
    }

    public void Pickup(Item item)
    {
        if (!IsAlive)
            return;

        _combatLog.Log($"<b>{gameObject.name}</b> picked up item <b>{item.name}</b>");

        // Todo: track picked items and their stats
        foreach (var buff in item.Buffs)
            ApplyBuff(buff);
    }

    public void ApplyBuff(Buff buff, int stacks=1)
    {
        if(buff == null)
            return;

        if(buff.OnApplyBuff != null)
            foreach (var affect in buff.OnApplyBuff)
                ApplyAffect(affect, stacks);

        var state = _states.FirstOrDefault(s => s.Buff.Equals(buff));
        if (state != null)
        {
            // State with same buff already exists
            switch (buff.Behaviour)
            {
                case BuffStackBehaviour.MaxStacksOfTwo:
                    RevertBuffChanges(state);
                    state.Stacks = Mathf.Max(stacks, state.Stacks);
                    ApplyBuffModifiers(state);
                    state.Refresh();
                    break;
                case BuffStackBehaviour.AddNewAsSeparate:
                    AddBuff();
                    break;
                case BuffStackBehaviour.SumStacks:
                    RevertBuffChanges(state);
                    state.Stacks += stacks;
                    ApplyBuffModifiers(state);
                    state.Refresh();
                    break;
                case BuffStackBehaviour.Discard:
                    break;
            }

            _combatLog.Log($"<b>{gameObject.name}</b> reapplied buff <b>{buff.name}</b> with <b>{buff.Behaviour}</b> behaviour. Stack after reapplied: <b>{state.Stacks}</b>");
        }
        else
        {
            AddBuff();
        }

        void RevertBuffChanges(BuffState s)
        {
            if (s.ActiveChanges != null)
            {
                for (var i = s.ActiveChanges.Count - 1; i >= 0; i--)
                {
                    RevertChange(s.ActiveChanges[i]);
                    s.ActiveChanges.RemoveAt(i);
                }
            }
        }

        void ApplyBuffModifiers(BuffState s)
        {
            if (s.Buff.Modifiers != null)
                foreach (var mod in s.Buff.Modifiers)
                {
                    ApplyModifier(mod, s.Stacks, out var change);
                    s.ActiveChanges.Add(new Change
                    {
                        Parameter = mod.Parameter,
                        Amount = change
                    });
                }
        }

        void AddBuff()
        {
            var s = new BuffState(buff, stacks);
            _states.Add(s);
            _combatLog.LogFormat("<b>{0}</b> received buff <b>{1}</b> with <b>{2}</b> stacks", 
                gameObject.name,
                buff.name,
                stacks);
            ApplyBuffModifiers(s);
        }
    }

    public void ApplyAffect(Affect affect, int stacks)
    {
        if (affect.ApplyModifier != null)
            ApplyModifier(affect.ApplyModifier, stacks, out _);

        if (affect.CastSpell != null)
            throw new NotImplementedException();

        if (affect.SpawnObject != null)
            GameObject.Instantiate(
                affect.SpawnObject, 
                GetNodeTransform(NodeRole.Chest), 
                false);
    }

    public void ApplyModifier(Modifier modifier, int stacks, out float change)
    {
        ApplyModifier(modifier.Parameter, modifier.Value, stacks, modifier.EffectiveStacks, out change);
    }

    public void ApplyModifier(ModificationParameter parameter, float amount, int stacks, float effectiveStacks, out float actualChange)
    {
        actualChange = 0f;
        
        var hpFraction = _hp / MaxHealth;
        switch (parameter)
        {
            case ModificationParameter.HpFlat:
                actualChange = SetHp(_hp + StackedModifier(amount, stacks, effectiveStacks));
                break;
            case ModificationParameter.HpMult:
                actualChange = SetHp(_hp * (1 + StackedModifier(amount, stacks, effectiveStacks)));
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
                _evasionModMulProduct *= Mathf.Pow(1 - amount, stacks);
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
                _assFlatMod += actualChange;
                break;
            default:
                break;
        }

        _combatLog.Log($"<b>{gameObject.name}</b> received modifier <b>{parameter}</b> with amount <b>{amount}</b>. Actual change: <b>{actualChange}</b>" +
                  $" Stacks: <b>{stacks}</b>. EffectiveStacks: <b>{effectiveStacks}</b>");

        switch (parameter)
        {
            case ModificationParameter.SizeFlat:
            case ModificationParameter.SizeMult:
                transform.localScale = _baseScale * Size;
                break;
        }
    }

    private void RevertChange(Change change)
    {
        // TODO: Revert EVASION and CRIT properly by dividing and not subtracting
        ApplyModifier(change.Parameter, -change.Amount, 1, 1, out _);
    }

    private float SetHp(float targetHp)
    {
        targetHp = Mathf.Clamp(targetHp, -1, MaxHealth);
        var delta = targetHp - _hp;
        if (delta < 0)
        {
            if (targetHp <= 0)
                HandleDeath();
            else
                _animationController.PlayHitImpactAnimation();
        }
        // Change
        _hp = targetHp;
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

        ApplyModifier(ModificationParameter.MaxHpFlat, -amount, 1, 1, out _);
        return true;
    }
    
    void Update()
    {
        if (IsAlive)
        {
            _timeBeforeNextAttack += Time.deltaTime;
        }
        else
        {
            return;
        }
        
#if DEBUG
        if(gameObject.CompareTag(Tags.Player))
            DisplayState();
#endif

        // Update buffs
        for (var i = _states.Count - 1; i >= 0; i--)
        {
            var buffState = _states[i];
            buffState.TickCd -= Time.deltaTime;
            buffState.TimeRemaining -= Time.deltaTime;
            
            // Buff tick
            if (buffState.TickCd < 0)
            {
                if (buffState.Buff.OnTickBuff != null)
                    foreach (var affect in buffState.Buff.OnTickBuff)
                        ApplyAffect(affect, buffState.Stacks);
                
                buffState.TickCd = buffState.Buff.TickCooldown;
            }

            // Buff remove
            if (buffState.TimeRemaining < 0)
            {
                if(buffState.Buff.OnRemove != null)
                    foreach (var affect in buffState.Buff.OnRemove)
                        ApplyAffect(affect, buffState.Stacks);


                if(buffState.ActiveChanges != null)
                    foreach (var change in buffState.ActiveChanges)
                        RevertChange(change);

                _states.RemoveAt(i);
            }
        }
    }

#if DEBUG
    void DisplayState()
    {
        var buffs = gameObject.name + "/Buffs states/";
        foreach (var buffState in _states)
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

        Debugger.Default.Display(gameObject.name + "/Health", Health);
        Debugger.Default.Display(gameObject.name + "/MaxHealth", MaxHealth);
        Debugger.Default.Display(gameObject.name + "/MaxHealth/MultModSum", _maxHpMultModSum);
        Debugger.Default.Display(gameObject.name + "/MaxHealth/FlatModSum", _maxHpFlatModSum);
        Debugger.Default.Display(gameObject.name + "/Speed", Speed);
        Debugger.Default.Display(gameObject.name + "/Speed/FlatModSum", _speedFlatModSum);
        Debugger.Default.Display(gameObject.name + "/Speed/MultModSum", _speedMultModSum);
        Debugger.Default.Display(gameObject.name + "/Damage", Damage);
        Debugger.Default.Display(gameObject.name + "/Damage/FlatModSum", _dmgFlatModSum);
        Debugger.Default.Display(gameObject.name + "/Damage/MultModSum", _dmgMultModSum);
        Debugger.Default.Display(gameObject.name + "/Evasion", Evasion);
        Debugger.Default.Display(gameObject.name + "/Evasion/MultProd", _evasionModMulProduct);


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
        OnDeath?.Invoke();
    }

    public void ReceiveDamage(float amount)
    {
        if (IsAlive)
        {
            if (Random.value > Evasion)
            {
                SetHp(_hp - amount);

                // Because health changed
                if (Health <= 0)
                {
                    HandleDeath();
                }
                else
                {
                    _animationController.PlayHitImpactAnimation();
                }
            }
        }
    }
    
    internal void ApplySpell(CharacterState owner, SubSpell spell)
    {
        if(!IsAlive)
            return;

        if (owner.CurrentTeam != CurrentTeam)
        {
            foreach (var buff in spell.Buffs)
                ApplyBuff(buff);
        }
    }

    
    public Transform GetNodeTransform(NodeRole role = NodeRole.Default)
    {
        if (Nodes == null || Nodes.Length == 0)
            return transform;

        var node = Nodes.FirstOrDefault(n => n.Role == role);
        if (node != null)
            return node.Transform;

        return Nodes[0].Transform;
    }

    void OnDrawGizmos()
    {
        var tSpell = GetNodeTransform(NodeRole.SpellEmitter);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(tSpell.position, .1f);

        var tDefault = GetNodeTransform(NodeRole.Default);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(tDefault.position, .1f);
    }

    /// <summary>
    /// Exponential linear unit. Used for multiplier modifiers to not go pass the -1 on the left side
    /// </summary>
    /// <param name="x"></param>
    /// <param name="alpha"></param>
    /// <returns>Returns the value in range [-1, +inf) </returns>
    public static float ELU(float x, float alpha=1f)
    {
        if (x >= 0)
            return x;
        return alpha * (Mathf.Exp(x) - 1);
    }

    public static float StackedModifier(float modifierValue, float stacks, float effectiveStacks)
    {
        // DO NOT CHANGE THIS! unless you do not know what you are doing!
        return modifierValue * stacks / ((stacks - 1) * (1 - 0.3f) / (Mathf.Min(1,effectiveStacks)) + 1);
    }
}
