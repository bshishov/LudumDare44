using System;
using Assets.Scripts.Data;
using UnityEngine;
using UnityEngine.Assertions;

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

    private SpellCaster _spellCaster;
    private CharacterState _characterState;

    public static readonly int SpellCount = 3;

    public readonly SpellSlotState[] SpellSlots = new SpellSlotState[SpellCount];

    private void Start()
    {
        _spellCaster = GetComponent<SpellCaster>();
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
        Assert.IsTrue(slotIndex >= 0 && slotIndex <= SpellCount);

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

    internal SpellSlotState GetSpellSlotStatus(int index)
    {
        Assert.IsTrue(index >= 0 && index <= SpellCount);
        return SpellSlots[index];
    }

    internal void PlaceSpell(Spell spell)
    {
        Debug.Log($"Place spell {spell.Name}");

        if (GetPickupOptions(spell) != PlaceOtions.Place)
        {
            Assert.IsFalse(true);
            return;
        }

        AddSpell(GetSpellSlot(spell), spell);
    }

    public static int GetSpellSlot(Spell spell)
    {
        return 0;
    }

    internal void FireSpell(int index, SpellEmitterData data)
    {
        Assert.IsTrue(index >= 0 && index <= SpellCount);
        var status = GetSpellSlotStatus(index);
        Assert.IsTrue(status.State == SpellState.Ready);

        _spellCaster.CastSpell(status.Spell, data);
    }

    internal void Pickup(Spell spell)
    {

    }

    private void AddSpell(int slot, Spell spell)
    {
        Debug.Log($"Spell {spell.Name} placed into slot {slot}");
        SpellSlots[slot] = new SpellSlotState
        {
            Spell = spell,
            State = SpellState.Ready,
            RemainingCooldown = 0.0f
        };
    }

    private void CheckAndFireSpell(SpellSlotState spell)
    {

    }

    internal void DrawSpellGizmos(int slot, Vector3 target) => Debug.Log("");
    //    _spellCaster.DrawSpellGizmos(SpellSlots[slot].Spell, target);
}
