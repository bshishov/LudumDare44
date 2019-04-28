﻿using System.Collections;
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

    public SpellbookState SpellbookState { get; private set; }
    public InventoryState InventoryState { get; private set; }

    public float MaxHealth { get; private set; }    
    public float Health { get; private set; }
    public float Speed { get; private set; }
    public float Evasion { get; private set; }
    public float Size { get; private set; }

    public float Damage { get; private set; }

    internal void Pickup(Spell spell)
    {
        Assert.IsNotNull(SpellbookState);
        var option = SpellbookState.GetPickupOptions(spell);

        switch (option)
        {
            case SpellbookState.PlaceOtions.Place:
                SpellbookState.PlaceSpell(spell);
                break;

            default:
                Debug.Log("Unhandled Pickup option");
                break;
        }
    }

    internal void FireSpell(int index, SpellEmitterData data)
    {
        Assert.IsNotNull(SpellbookState);

        var status = SpellbookState.GetSpellSlotStatus(index);

        switch(status.State)
        {
            case SpellbookState.SpellState.None:
                Debug.Log("SpellSlot is empty");
                break;

            case SpellbookState.SpellState.Ready:
                SpellbookState.FireSpell(index, data);
                break;

            default:
                Debug.Log("Unhandled spell state");
                break;
        }
    }

    internal void Pickup(Item item)
    {

    }

    public float DropRate { get; private set; }

    public IReadOnlyList<Spell> DropSpells { get; private set; }

    public Dictionary<Buff, float> BuffsOn = new Dictionary<Buff, float>();

    public float HealthRegen { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        SpellbookState = GetComponent<SpellbookState>();
        InventoryState = GetComponent<InventoryState>();

        MaxHealth = character.Health;
        Health = character.Health;
        Speed = character.Speed;
        Damage = character.Damage;
        DropRate = character.DropRate;
        DropSpells = character.DropSpells;
        Evasion = character.Evasion;
        Size = character.Size;
        InvokeRepeating("SecondsUpdate", 0.0f, 1.0f);

        if (CurrentTeam == Team.Undefined)
            Debug.LogError("Team not setted!", this);
    }

    private float _secondTickReminingTime = 1f;
    
    void Update()
    {
        if (Health <= 0)
        {
            if ((DropSpells.Count) > 0){
                Spell DroppedSpell = DropSpells[Mathf.FloorToInt(Random.value*DropSpells.Count)];      
                // TODO: Drop spell
            }
            Debug.Log("GG");
        }

        _secondTickReminingTime -= Time.deltaTime;
        if (_secondTickReminingTime < 0f)
        {
            SecondsUpdate();
            _secondTickReminingTime = 1f;
        }

#if DEBUG
        if(gameObject.CompareTag(Tags.Player))
            DisplayState();
#endif
    }

    void DisplayState()
    {
#if DEBUG
        Debugger.Default.Display(gameObject.name + "/HP", Health);
        Debugger.Default.Display(gameObject.name + "/MAX HP", MaxHealth);
        Debugger.Default.Display(gameObject.name + "/HP REGEN", HealthRegen);
        Debugger.Default.Display(gameObject.name + "/Speed", Speed);
        Debugger.Default.Display(gameObject.name + "/Damage", Damage);
        Debugger.Default.Display(gameObject.name + "/Evasion", Evasion);
#endif
    }

    void SecondsUpdate()
    {        
        foreach (var buff in BuffsOn.Keys.ToList())
        {
#if DEBUG
            Debugger.Default.Display(gameObject.name + "/Buffs/" + buff.name, BuffsOn[buff]);
#endif
            BuffsOn[buff] -= 1f;

            if (BuffsOn[buff] >= 0)
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
                BuffsOn.Remove(buff);
            }
        }
    }

    //TODO: @artemy implement me
    internal void ApplySpell(CharacterState owner, SubSpell spell)
    {
        if (owner.CurrentTeam != CurrentTeam)
        {
            foreach (Buff buff in spell.Buffs)
            {
                CastBuff(buff);
            }
        }
    }
    

    public void CastBuff(Buff buff)
    {
        if (BuffsOn.ContainsKey(buff))
        {
            BuffsOn[buff] = buff.Duration;
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
                BuffsOn.Add(buff, buff.Duration);
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
                Health = Health / buff.Multiplier - buff.Addition; 
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
}
