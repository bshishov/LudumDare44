using System;
using Assets.Scripts.Data;
using UnityEngine;

public class SpellbookState : MonoBehaviour
{
    public enum PlaceOtions : int
    {
        None = 0,
        Place,
        Upgrade,
        Replace
    }

    public enum SpellState : int
    {
        None = 0,
        Recharging,
        Chaneling,
        Ready
    }

    public struct SpellSlotState
    {
        public Spell Spell;
        public SpellState State;
        public float RemainingCooldown;
    };

    private CharacterState _characterState;

    public static readonly int SpellCount = 3;

    public readonly SpellSlotState[] SpellSlots = new SpellSlotState[SpellCount];

    private void Start()
    {
        _characterState = GetComponent<CharacterState>();
        var initialSpells = _characterState.character.UseSpells;
        if (initialSpells.Count > 3)
        {
            Debug.LogWarning("To much spells!");
        }

        for (int i = 0; i < SpellCount && i < initialSpells.Count; ++i)
        {
            AddSpell(i, initialSpells[i]);
        }
    }

    internal PlaceOtions GetPickupOptions(Spell spell)
    {
        var slotIndex = GetSpellSlot(spell);
        if(slotIndex == -1)
        {
            Debug.LogError($"No slot for spell {spell.Name}");
            return PlaceOtions.None;
        }

        var slot = SpellSlots[slotIndex];
        if (slot.State == SpellState.None)
            return PlaceOtions.Place;

        if (slot.Spell.Name == spell.Name)
        {
            Debug.Log($"Upgrade spell {spell.Name}");
            return PlaceOtions.Upgrade;
        }

        Debug.Log($"Replace spell {slot.Spell.Name}");
        return PlaceOtions.Replace;
    }

    public int GetSpellSlot(Spell spell)
    {
        return 0;
    }

    internal void FireSpell(int index)
    {
        if (index >= SpellCount)
        {
            Debug.LogWarning($"Index {index} is invalid to cast spell!");
            return;
        }

        CheckAndFireSpell(SpellSlots[index]);
    }

    internal void Pickup(Spell spell)
    {

    }

    private void AddSpell(int i, Spell spell) => 
        SpellSlots[i] = new SpellSlotState
        {
            Spell = spell,
            State = SpellState.Ready,
            RemainingCooldown = 0.0f
        };

    private void CheckAndFireSpell(SpellSlotState spell)
    {

    }
}
