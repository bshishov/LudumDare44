using Assets.Scripts.Data;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Assertions.Must;

namespace Spells
{
    [RequireComponent(typeof(SpellCaster))]
    public class SpellbookState : MonoBehaviour
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
            Recharging,
            Channeling,
            Ready
        }

        public struct SpellSlotState
        {
            public Spell Spell;
            public SpellState State;
            public float RemainingCooldown;
            public int NumStacks;
        };

        private SpellCaster _spellCaster;
        private CharacterState _characterState;
        private AnimationController _animationController;

        public static readonly int SpellCount = 3;

        public readonly SpellSlotState[] SpellSlots = new SpellSlotState[SpellCount];

        private void Start()
        {
            _spellCaster = GetComponent<SpellCaster>();
            _animationController = GetComponent<AnimationController>();
            _characterState = GetComponent<CharacterState>();

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
            var slot = GetSpellSlot(spell);
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

        public static int GetSpellSlot(Spell spell)
        {
            return (int)spell.DefaultSlot;
        }

        private void FireSpell(int index, SpellTargets targets)
        {
            // Disable self cast
            if (_characterState.Equals(targets.Destinations[0].Character))
                return;

            Assert.IsTrue(index >= 0 && index <= SpellCount);
            var slotState = GetSpellSlotState(index);
            if (slotState.State == SpellState.Ready)
            {
                if (_spellCaster.CastSpell(slotState.Spell, slotState.NumStacks + _characterState.AdditionSpellStacks, targets))
                {
                    // Start cooldown
                    SpellSlots[index].State = SpellState.Recharging;
                    SpellSlots[index].RemainingCooldown = slotState.Spell.Cooldown;

                    // Animation
                    if(_animationController != null)
                        _animationController.PlayCastAnimation();
                }
            }
        }


        public void TryFireSpellToTarget(int slotIndex, CharacterState target)
            =>  TryFireSpellToTarget(slotIndex, TargetInfo.Create(target, target.GetNodeTransform(CharacterState.NodeRole.Chest)));

        public void TryFireSpellToTarget(int slotIndex, TargetInfo target)
            => FireSpell(slotIndex, 
                new SpellTargets(
                    TargetInfo.Create(_characterState, _characterState.GetNodeTransform(CharacterState.NodeRole.SpellEmitter))
                    , target));

        private void AddSpellToSlot(int slotIndex, Spell spell, int stacks)
        {
            Debug.Log($"Spell {spell.name} placed into slot {slotIndex}");
            SpellSlots[slotIndex] = new SpellSlotState
            {
                Spell = spell,
                State = SpellState.Ready,
                RemainingCooldown = 0.0f,
                NumStacks = stacks
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
                if (slotState.State == SpellState.Recharging)
                {
                    SpellSlots[slotIndex].RemainingCooldown = slotState.RemainingCooldown - Time.deltaTime;
                    if (slotState.RemainingCooldown <= 0f)
                    {
                        SpellSlots[slotIndex].State = SpellState.Ready;
                    }
                }
            }
        }
    }
}
