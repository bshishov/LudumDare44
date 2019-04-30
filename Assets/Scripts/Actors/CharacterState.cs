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
    public InventoryState InventoryState { get; private set; }
    public float MaxHealth { get; private set; }    
    public float Health { get; private set; }
    public float Speed { get; private set; }
    public float Evasion { get; private set; }
    public float Size { get; private set; }
    public float Damage { get; private set; }
    public float DropRate { get; private set; }
    public IReadOnlyList<Spell> DropSpells { get; private set; }
    public Dictionary<Buff, float> AppliedBuffs = new Dictionary<Buff, float>();
    public float HealthRegen { get; private set; }
    public float Currency
    {
        get { return MaxHealth; }
        set
        {
            MaxHealth = Mathf.Max(1, value);
            Health = Mathf.Min(Health, MaxHealth);
            _animationController.PlayHitImpactAnimation();
        }
    }
    public bool IsAlive => Health > 0;

    private float _timeBeforeNextAttack;
    private AnimationController _animationController;
    private SpellbookState _spellbook;

    void Start()
    {
        _spellbook = GetComponent<SpellbookState>();
        InventoryState = GetComponent<InventoryState>();
        _animationController = GetComponent<AnimationController>();

        MaxHealth = character.Health;
        Health = character.Health * character.HealthModifier;
        Speed = character.Speed;
        Damage = character.Damage;
        DropRate = character.DropRate;
        DropSpells = character.DropSpells;
        Evasion = character.Evasion;
        Size = character.Size;
        InvokeRepeating("UpdatePerSecond", 1.0f, 1.0f);

        if (CurrentTeam == Team.Undefined)
            Debug.LogError("Team not setted!", this);
        _timeBeforeNextAttack = 0f;
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
        {
            ApplyBuffProperty(buff);
        }
    }
    

    public bool SpendCurrency(float amount)
    {
        if (!IsAlive)
            return false;

        if (amount > Currency)
        {
            Debug.LogWarningFormat("Cant spend currency. Currency={0}, Trying to spend = {1}", Currency, amount);
            return false;
        }

        Currency -= amount;
        return true;
    }
    
    void Update()
    {
        if (IsAlive)
        {
            _timeBeforeNextAttack += Time.deltaTime;
        }
        
#if DEBUG
        if(gameObject.CompareTag(Tags.Player))
            DisplayState();
#endif
    }

#if DEBUG
    void DisplayState()
    {
        Debugger.Default.Display(gameObject.name + "/HP", Health);
        Debugger.Default.Display(gameObject.name + "/MAX HP", MaxHealth);
        Debugger.Default.Display(gameObject.name + "/HP REGEN", HealthRegen);
        Debugger.Default.Display(gameObject.name + "/Speed", Speed);
        Debugger.Default.Display(gameObject.name + "/Damage", Damage);
        Debugger.Default.Display(gameObject.name + "/Evasion", Evasion);
    }
#endif

    void HandleDeath()
    {
        if (DropSpells.Count > 0 && Random.value < DropRate)
        {
            var spell = RandomUtils.Choice((IList<Spell>)DropSpells);
            if (spell != null)
            {
                DroppedSpell.InstantiateDroppedSpell(spell, GetNodeTransform(NodeRole.Chest).position);
            }
        }

        AppliedBuffs.Clear();
        _animationController.PlayDeathAnimation();
        Debug.LogFormat("{0} died", gameObject.name);
    }

    void UpdatePerSecond()
    {
        if (!IsAlive)
            return;

        foreach (var buff in AppliedBuffs.Keys.ToList())
        {
#if DEBUG
            Debugger.Default.Display(gameObject.name + "/Buffs/" + buff.name, AppliedBuffs[buff]);
#endif
            AppliedBuffs[buff] -= 1f;

            if (AppliedBuffs[buff] >= 0)
            {
                if (buff.PerSecond)
                {
                    ApplyBuffProperty(buff);
                }
            }
            else
            {
                if (!buff.PerSecond)
                {
                    RevertBuffProperty(buff);
                }
                AppliedBuffs.Remove(buff);
            }
        }
        
        Health = Mathf.Min(Health + HealthRegen, MaxHealth);
    }

    public void ReceiveDamage(float amount)
    {
        if (IsAlive)
        {
            if(Random.value > Evasion)
                Health -= Mathf.Abs(amount);
        }

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
    
    internal void ApplySpell(CharacterState owner, SubSpell spell)
    {
        if(!IsAlive)
            return;

        if (owner.CurrentTeam != CurrentTeam)
        {
            foreach (var buff in spell.Buffs)
            {
                ApplyBuff(buff);
            }
        }
    }

    public void ApplyBuff(Buff buff)
    {
        if (!IsAlive)
            return;

        if (AppliedBuffs.ContainsKey(buff))
        {
            // If there is a buff already exist just renew the cooldown
            AppliedBuffs[buff] = buff.Duration;
        }
        else
        {
            if (buff.OnAppliedEffect != null)
            {
                GameObject.Instantiate(buff.OnAppliedEffect, GetNodeTransform(NodeRole.Chest), false);
            }

            if (buff.Permanent)
            {
                ApplyBuffProperty(buff);
            }
            else {
                if (!buff.PerSecond)
                {
                    ApplyBuffProperty(buff);
                }
                AppliedBuffs.Add(buff, buff.Duration);
            }            
        }
    }

    void ApplyBuffProperty(Buff buff)
    {
        switch (buff.ChangedProperty)
        {
            case Buff.ChangedProperties.Speed:
                Speed = (Speed + buff.Addition) * buff.Multiplier;
                break;
            case Buff.ChangedProperties.Damage:
                ReceiveDamage(buff.Addition * buff.Multiplier);
                break;
            case Buff.ChangedProperties.Power:
                Damage = (Damage + buff.Addition) * buff.Multiplier;
                break;
            case Buff.ChangedProperties.HealthRegen:
                HealthRegen = (HealthRegen + buff.Addition) * buff.Multiplier;
                break;
            case Buff.ChangedProperties.HealthCap:
                MaxHealth = (MaxHealth + buff.Addition) * buff.Multiplier;
                break;
            case Buff.ChangedProperties.Evasion:
                Evasion = (Evasion + buff.Addition) * buff.Multiplier;
                break;
            case Buff.ChangedProperties.Size:
                Size = (Size + buff.Addition) * buff.Multiplier;
                break;
        }
    }

    void RevertBuffProperty(Buff buff)
    {
        switch (buff.ChangedProperty)
        {
            case Buff.ChangedProperties.Speed:
                Speed = Speed / buff.Multiplier - buff.Addition;
                break;
            case Buff.ChangedProperties.Damage:
                Health = Health / buff.Multiplier + buff.Addition; 
                if (Health > MaxHealth)
                {
                    Health = MaxHealth;
                }
                break;
            case Buff.ChangedProperties.Power:
                Damage = Damage / buff.Multiplier - buff.Addition; 
                break;
            case Buff.ChangedProperties.HealthRegen:
                HealthRegen = HealthRegen / buff.Multiplier - buff.Addition; 
                break;
            case Buff.ChangedProperties.HealthCap:
                MaxHealth = MaxHealth / buff.Multiplier - buff.Addition; 
                break;
            case Buff.ChangedProperties.Evasion:
                Evasion = Evasion / buff.Multiplier - buff.Addition;
                break;
            case Buff.ChangedProperties.Size:
                Size = Size / buff.Multiplier - buff.Addition; 
                break;
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
}
