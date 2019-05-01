using System.Collections;
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

    public enum Team : int
    {
        Undefined = 0,
        Player,
        Enemies,
        AgainstTheWorld
    }

    [EnumFlag]
    public Team CurrentTeam = Team.Undefined;

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
    public float Size => (character.Size + _sizeFlatModSum) * (1 + ELU(_sizeMultModSum));

    // ========= AdditionSpellStacks
    private float _assFlatMod;
    public float AdditionSpellStacks => _assFlatMod;
    public float DropRate => character.DropRate;
    public List<Spell> DropSpells => character.DropSpells;

    // Internal
    private float _timeBeforeNextAttack;
    private AnimationController _animationController;
    private SpellbookState _spellbook;
    private readonly List<BuffState> _states = new List<BuffState>();

    void Start()
    {
        IsAlive = true;

        _spellbook = GetComponent<SpellbookState>();
        _animationController = GetComponent<AnimationController>();
        _timeBeforeNextAttack = 0f;
        _hp = character.HealthModifier * MaxHealth;

        if (CurrentTeam == Team.Undefined)
            Debug.LogError("Team not set!", this);
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
        
        _spellbook.PlaceSpell(spell);
    }

    public void Pickup(Item item)
    {
        if (!IsAlive)
            return;

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
                    RevertBuffModifiers(state.Buff, state.Stacks);
                    state.Stacks = Mathf.Max(stacks, state.Stacks);
                    ApplyBuffModifiers(state.Buff, state.Stacks);
                    state.Refresh();
                    break;
                case BuffStackBehaviour.AddNewAsSeparate:
                    AddBuff();
                    break;
                case BuffStackBehaviour.SumStacks:
                    RevertBuffModifiers(state.Buff, state.Stacks);
                    state.Stacks += stacks;
                    ApplyBuffModifiers(state.Buff, state.Stacks);
                    state.Refresh();
                    break;
                case BuffStackBehaviour.Discard:
                    break;
            }
        }
        else
        {
            AddBuff();
        }

        void RevertBuffModifiers(Buff b, int s)
        {
            if (b.Modifiers != null)
                foreach (var mod in b.Modifiers)
                    RevertModifier(mod, s);
        }

        void ApplyBuffModifiers(Buff b, int s)
        {
            if (b.Modifiers != null)
                foreach (var mod in b.Modifiers)
                    ApplyModifier(mod, s);
        }

        void AddBuff()
        {
            ApplyBuffModifiers(buff, stacks);
            _states.Add(new BuffState(buff, stacks));
            Debug.LogFormat("<b>{0}</b> received buff <b>{1}</b> with <b>{2}</b> stacks", 
                gameObject.name,
                buff.name,
                stacks);
        }
    }

    public void ApplyAffect(Affect affect, int stacks)
    {
        if (affect.ApplyModifier != null)
            ApplyModifier(affect.ApplyModifier, stacks);

        if (affect.CastSpell != null)
            throw new NotImplementedException();

        if (affect.SpawnObject != null)
            GameObject.Instantiate(
                affect.SpawnObject, 
                GetNodeTransform(NodeRole.Chest), 
                false);
    }

    public void ApplyModifier(Modifier modifier, int stacks)
    {
        Debug.LogFormat("Applied modifier <b>{0}</b> with value <b>{1}</b>. Stacks: <b>{2}</b>. StackMultiplier: <b>{3}</b>", 
            modifier.Parameter, 
            modifier.Value, 
            stacks, 
            modifier.PerStackMultiplier);
        var hpFraction = _hp / MaxHealth;
        switch (modifier.Parameter)
        {
            case ModificationParameter.HpFlat:
                SetHp(_hp + modifier.Value * stacks * modifier.PerStackMultiplier);
                break;
            case ModificationParameter.HpMult:
                SetHp(_hp * (1 + modifier.Value * stacks * modifier.PerStackMultiplier));
                break;
            case ModificationParameter.MaxHpFlat:
                _maxHpFlatModSum += modifier.Value * stacks * modifier.PerStackMultiplier;
                _hp = hpFraction * MaxHealth;
                break;
            case ModificationParameter.MaxHpMult:
                _maxHpMultModSum += modifier.Value * stacks * modifier.PerStackMultiplier;
                _hp = hpFraction * MaxHealth;
                break;
            case ModificationParameter.DmgFlat:
                _dmgFlatModSum += modifier.Value * stacks * modifier.PerStackMultiplier;
                break;
            case ModificationParameter.DmgMult:
                _dmgMultModSum += modifier.Value * stacks * modifier.PerStackMultiplier;
                break;
            case ModificationParameter.EvasionChanceFlat:
                _evasionModMulProduct *= Mathf.Pow(1 - modifier.Value, stacks * modifier.PerStackMultiplier);
                break;
            case ModificationParameter.CritChanceFlat:
                break;
            case ModificationParameter.SpeedFlat:
                _speedFlatModSum += modifier.Value * stacks * modifier.PerStackMultiplier;
                break;
            case ModificationParameter.SpeedMult:
                _speedMultModSum += modifier.Value * stacks * modifier.PerStackMultiplier;
                break;
            case ModificationParameter.SizeFlat:
                _sizeFlatModSum += modifier.Value * stacks * modifier.PerStackMultiplier;
                break;
            case ModificationParameter.SizeMult:
                _sizeMultModSum += modifier.Value * stacks * modifier.PerStackMultiplier;
                break;
            case ModificationParameter.SpellStacksFlat:
                _assFlatMod += modifier.Value * stacks * modifier.PerStackMultiplier;
                break;
            default:
                break;
        }
    }

    public void RevertModifier(Modifier modifier, int stacks)
    {
        ApplyModifier(new Modifier
        {
            Parameter = modifier.Parameter,
            PerStackMultiplier = modifier.PerStackMultiplier,
            Value = -modifier.Value
        }, stacks);
    }

    private void SetHp(float targetHp)
    {
        targetHp = Mathf.Clamp(targetHp, -1, MaxHealth);
        var delta = targetHp - _hp;
        if (delta < 0)
        {
            _animationController.PlayHitImpactAnimation();
            if (targetHp <= 0)
                HandleDeath();
        }
        // Change
        _hp = targetHp;
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

        ApplyModifier(new Modifier
        {
            Parameter = ModificationParameter.MaxHpFlat,
            PerStackMultiplier = 1f,
            Value = -amount
        }, 1);
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


                if(buffState.Buff.Modifiers != null)
                    foreach (var buffModifier in buffState.Buff.Modifiers)
                        RevertModifier(buffModifier, buffState.Stacks);

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
            Debugger.Default.Display(buffs + buffState.Buff.name + "/Stacks", buffState.Stacks);
            Debugger.Default.Display(buffs + buffState.Buff.name + "/Tick CD", buffState.TickCd);
            Debugger.Default.Display(buffs + buffState.Buff.name + "/Time remaining", buffState.TimeRemaining);
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
        Debug.LogFormat("{0} died", gameObject.name);
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

    public static float ELU(float x, float alpha=1f)
    {
        if (x >= 0)
            return x;
        return alpha * (Mathf.Exp(x) - 1);
    }
}
