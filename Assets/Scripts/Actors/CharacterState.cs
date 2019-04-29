using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Data;
using System.Linq;
using Random = UnityEngine.Random;
using System;
using Assets.Scripts;
using Assets.Scripts.Utils.Debugger;
using UnityEngine.Assertions;

public class CharacterState : MonoBehaviour
{
    [Serializable]
    public class CharacterNode
    {
        [Serializable]
        public enum NodeRole
        {
            Default
        }

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
    public SpellbookState SpellbookState { get; private set; }
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
        }
    }

    internal void Pickup(Spell spell)
    {
        Assert.IsNotNull(SpellbookState);
        var option = SpellbookState.GetPickupOptions(spell);

        switch (option)
        {
            case SpellbookState.PlaceOptions.Place:
                SpellbookState.PlaceSpell(spell);
                break;

            default:
                Debug.Log("Unhandled Pickup option");
                break;
        }
    }

    public void FireSpell(int slotIndex, SpellEmitterData data)
    {
        Assert.IsNotNull(SpellbookState);

        var status = SpellbookState.GetSpellSlotState(slotIndex);

        switch(status.State)
        {
            case SpellbookState.SpellState.None:
                Debug.Log("SpellSlot is empty");
                break;

            case SpellbookState.SpellState.Ready:
                SpellbookState.FireSpell(slotIndex, data);
                break;

            default:
                Debug.Log("Unhandled spell state");
                break;
        }
    }

    public void Pickup(Item item)
    {
        // Todo: track picked items and their stats
        foreach (var buff in item.Buffs)
        {
            BuffPropertyNew(buff);
        }
    }
    
    void Start()
    {
        SpellbookState = GetComponent<SpellbookState>();
        InventoryState = GetComponent<InventoryState>();

        MaxHealth = character.Health;
        Health = character.Health*character.HealthModifier;
        Speed = character.Speed;
        Damage = character.Damage;
        DropRate = character.DropRate;
        DropSpells = character.DropSpells;
        Evasion = character.Evasion;
        Size = character.Size;
        InvokeRepeating("UpdatePerSecond", 1.0f, 1.0f);

        if (CurrentTeam == Team.Undefined)
            Debug.LogError("Team not setted!", this);
    }

    public bool SpendCurrency(float amount)
    {
        if (amount < Currency)
        {
            Debug.LogWarningFormat("Cant spend currency. Currency={0}, Trying to spend = {1}", Currency, amount);
            return false;
        }

        Currency -= amount;
        return true;
    }
    
    void Update()
    {
        if (Health <= 0)
        {
            if ((DropSpells.Count) > 0){
                var DroppedSpell = DropSpells[Mathf.FloorToInt(Random.value*DropSpells.Count)];      
                // TODO: Drop spell
            }
            Debug.Log("GG");
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

    void UpdatePerSecond()
    {        
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
                    BuffPropertyNew(buff);
                }
            }
            else
            {
                if (!buff.PerSecond)
                {
                    BuffPropertyReturn(buff);
                }
                AppliedBuffs.Remove(buff);
            }
        }

        Health = Mathf.Min(Health + HealthRegen, MaxHealth);
    }
    
    internal void ApplySpell(CharacterState owner, SubSpell spell)
    {
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
        if (AppliedBuffs.ContainsKey(buff))
        {
            // If there is a buff already exist just renew the cooldown
            AppliedBuffs[buff] = buff.Duration;
        }
        else
        {
            if (buff.Permanent)
            {
                BuffPropertyNew(buff);
            }
            else {
                if (!buff.PerSecond)
                {
                    BuffPropertyNew(buff);
                }
                AppliedBuffs.Add(buff, buff.Duration);
            }            
        }
    }

    void BuffPropertyNew(Buff buff)
    {
        switch (buff.ChangedProperty)
        {
            case Buff.ChangedProperties.Speed:
                Speed = (Speed + buff.Addition) * buff.Multiplier;
                break;
            case Buff.ChangedProperties.Damage:
                Health = (Health - buff.Addition) * buff.Multiplier;
                if (Health > MaxHealth)
                {
                    Health = MaxHealth;
                }
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

    void BuffPropertyReturn(Buff buff)
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
       
    internal void DrawSpellGizmos(int slot, Vector3 target) => SpellbookState.DrawSpellGizmos(slot, target);

    public Transform GetDefaultNodeTransform()
    {
        if (Nodes == null || Nodes.Length == 0)
            return transform;

        return Nodes[0].Transform;
    }
}
