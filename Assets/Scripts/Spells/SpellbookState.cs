using Assets.Scripts.Data;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Assertions.Must;

namespace Spells
{
[RequireComponent(typeof(SpellCaster))]
public class SpellbookState : MonoBehaviour, ISpellCastListener
{
    public enum PlaceOptions : int
    {
        None = 0,
        Place,
        Upgrade,
        Replace
    }

    public enum SpellState : int
    {
        None = 0,
        Ready,
        Preparing,
        Firing,
        Recharging
    }

    public struct SpellSlotState
    {
        public Spell      Spell;
        public SpellState State;
        public float      RemainingCooldown;
        public int        NumStacks;
    };

    private SpellCaster         _spellCaster;
    private CharacterState      _characterState;
    private AnimationController _animationController;

    public static readonly int SpellCount = 3;

    public          bool             IsCasting { get; private set; }
    public readonly SpellSlotState[] SpellSlots = new SpellSlotState[SpellCount];

    private void Start()
    {
        _spellCaster         = GetComponent<SpellCaster>();
        _animationController = GetComponent<AnimationController>();
        _characterState      = GetComponent<CharacterState>();

        var initialSpells = _characterState.character.UseSpells;
        if (initialSpells.Count > 3)
        {
            Debug.LogWarning("To much spells!");
        }

        for (var i = 0; i < SpellCount && i < initialSpells.Count; ++i)
        {
            AddSpellToSlot(i, initialSpells[i], 1);
        }
    }

    internal PlaceOptions GetPickupOptions(Spell spell)
    {
        var slot = GetSpellSlotState(GetSpellSlot(spell));

        // If slot state is empty, should just add spell to a slot
        if (slot.State == SpellState.None)
            return PlaceOptions.Place;

        // If same spell - should upgrade
        if (slot.Spell.Equals(spell))
        {
            Debug.Log($"Upgrade spell {spell.Name}");
            return PlaceOptions.Upgrade;
        }

        // Replace existing slot with a picked up one
        Debug.Log($"Replace spell {slot.Spell.Name}");
        return PlaceOptions.Replace;
    }

    public SpellSlotState GetSpellSlotState(int slotIndex)
    {
        Assert.IsTrue(slotIndex >= 0 && slotIndex <= SpellCount);
        return SpellSlots[slotIndex];
    }

    public void PlaceSpell(Spell spell, int stacks)
    {
        var slot          = GetSpellSlot(spell);
        var pickupOptions = GetPickupOptions(spell);
        Debug.Log($"Placing spell {spell.name} into slot {slot}. PlaceMode = {pickupOptions}");

        switch (pickupOptions)
        {
            case PlaceOptions.Place:
                AddSpellToSlot(GetSpellSlot(spell), spell, stacks);
                break;

            case PlaceOptions.Upgrade:
                // Upgrade is just a stack count increase
                UpgradeSpellInSlot(slot, stacks);
                break;

            case PlaceOptions.Replace:
                // Current spell is dropped
                // Todo: Check!
                AddSpellToSlot(GetSpellSlot(spell), spell, stacks);
                break;
        }
    }

    public bool IsSpellReady(int slotIndex)
    {
        var slotState = GetSpellSlotState(slotIndex).State;
        if (slotState == SpellbookState.SpellState.Ready)
            return true;

        return false;
    }

    public static int GetSpellSlot(Spell spell) { return (int) spell.DefaultSlot; }

    private bool FireSpell(int index, SpellTargets targets, IChannelingInfo channelingInfo)
    {
        if (IsCasting == true)
        {
            Debug.Log("Already casting");
            return false;
        }

        // Disable self cast
        if (_characterState.Equals(targets.Destinations[0].Character))
            return false;

        Assert.IsTrue(index >= 0 && index <= SpellCount);
        var slotState = GetSpellSlotState(index);

        if (slotState.State != SpellState.Ready || !SpellCaster.IsValidTarget(slotState.Spell, targets))
            return false;

        if (!_spellCaster.CastSpell(slotState.Spell, slotState.NumStacks + _characterState.AdditionSpellStacks, targets, channelingInfo, this))
            return false;

        // Start cooldown
        SpellSlots[index].State             = SpellState.Preparing;
        SpellSlots[index].RemainingCooldown = 0;

        IsCasting = true;

        // Animation
        if (_animationController != null)
            _animationController.PlayCastAnimation();

        return true;
    }


    public bool TryFireSpellToTarget(int slotIndex, CharacterState target, IChannelingInfo channelingInfo)
    {
        return TryFireSpellToTarget(slotIndex, TargetInfo.Create(target, target.GetNodeTransform(CharacterState.NodeRole.Chest)), channelingInfo);
    }

    public bool TryFireSpellToTarget(int slotIndex, TargetInfo target, IChannelingInfo channelingInfo)
    {
        return FireSpell(slotIndex,
                         new SpellTargets(TargetInfo.Create(_characterState, _characterState.GetNodeTransform(CharacterState.NodeRole.SpellEmitter)), target),
                         channelingInfo);
    }

    private void AddSpellToSlot(int slotIndex, Spell spell, int stacks)
    {
        Debug.Log($"Spell {spell.name} placed into slot {slotIndex}");
        SpellSlots[slotIndex] = new SpellSlotState
                                {
                                    Spell             = spell,
                                    State             = SpellState.Ready,
                                    RemainingCooldown = 0.0f,
                                    NumStacks         = stacks
                                };
    }

    private void UpgradeSpellInSlot(int slotIndex, int stacks)
    {
        // Number of stacks increased
        var slotState = GetSpellSlotState(slotIndex);
        SpellSlots[slotIndex].NumStacks = slotState.NumStacks + stacks;
    }

    void Update()
    {
        // Update cooldowns
        for (var slotIndex = 0; slotIndex < SpellSlots.Length; slotIndex++)
        {
            var slotState = SpellSlots[slotIndex];

            if (SpellSlots[slotIndex].RemainingCooldown > 0)
                SpellSlots[slotIndex].RemainingCooldown = slotState.RemainingCooldown - Time.deltaTime;
            if (slotState.RemainingCooldown <= 0f && SpellSlots[slotIndex].State == SpellState.Recharging)
            {
                SpellSlots[slotIndex].State = SpellState.Ready;
            }
        }
    }

    public void OnAbortedFiring(Spell spell)
    {
        Assert.IsTrue(IsCasting);

        var slotIndex = GetSpellSlot(spell);

        Assert.IsTrue(SpellSlots[slotIndex].Spell == spell);
        if (SpellSlots[slotIndex].State == SpellState.Preparing)
        {
            Debug.Log("Spell was aborted at preparing stage");
            SpellSlots[slotIndex].State             = SpellState.Ready;
            SpellSlots[slotIndex].RemainingCooldown = 0;

            return;
        }

        Debug.Log("Spell was aborted after preparing stage");
        SpellSlots[slotIndex].State = SpellState.Recharging;
            SpellSlots[slotIndex].RemainingCooldown = spell.Cooldown;

        }

    public void OnStartFiring(Spell spell, SubSpell subSpell)
    {
        Debug.Log("OnStartFiring");
        Assert.IsTrue(IsCasting);

        var slotIndex = GetSpellSlot(spell);

        Assert.IsTrue(SpellSlots[slotIndex].Spell == spell);
        Assert.IsTrue(SpellSlots[slotIndex].State == SpellState.Preparing || SpellSlots[slotIndex].State == SpellState.Firing);

        SpellSlots[slotIndex].State             = SpellState.Firing;

        // TODO: refactor this. Move it to another place
        _characterState.ApplyModifier(ModificationParameter.HpFlat, -subSpell.BloodCost, 1, 1, _characterState, null, out _);
    }

    public void OnEndFiring(Spell spell)
    {
        Debug.Log("OnEndFiring");
        Assert.IsTrue(IsCasting);

        var slotIndex = GetSpellSlot(spell);

        Assert.IsTrue(SpellSlots[slotIndex].Spell == spell);
        Assert.IsTrue(SpellSlots[slotIndex].State != SpellState.Recharging);

        SpellSlots[slotIndex].State             = SpellState.Recharging;
        SpellSlots[slotIndex].RemainingCooldown = spell.Cooldown;
    }

    public void OnEndCasting(Spell spell)
    {
        Debug.Log("OnEndCasting");
        Assert.IsTrue(IsCasting);

        var slotIndex = GetSpellSlot(spell);

        Assert.IsTrue(SpellSlots[slotIndex].Spell == spell);
        Assert.IsTrue(SpellSlots[slotIndex].State == SpellState.Recharging || SpellSlots[slotIndex].State == SpellState.Ready);
        IsCasting = false;
    }
}
}
