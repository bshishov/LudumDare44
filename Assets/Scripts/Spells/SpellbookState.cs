using Actors;
using Data;
using UnityEngine;
using UnityEngine.Assertions;

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
            None,
            Ready,
            Preparing,
            Firing,
            Recharging
        }

        public class SpellSlotState
        {
            public Spell Spell;
            public SpellState State;
            public float RemainingCooldown;
            public int NumStacks;
            public ISpellHandler SpellHandler;
        };

        private SpellCaster _spellCaster;
        private CharacterState _characterState;
        private AnimationController _animationController;

        public SpellSlotState[] SpellSlots;
        public bool NoCooldowns = false;

        private void Awake()
        {
            _spellCaster = GetComponent<SpellCaster>();
            _animationController = GetComponent<AnimationController>();
            _characterState = GetComponent<CharacterState>();

            var totalSlots = new SpellSlotState[Mathf.Max(3, _characterState.character.UseSpells.Count)];
            SpellSlots = totalSlots;
            for (var i = 0; i < SpellSlots.Length; i++)
                SpellSlots[i] = new SpellSlotState();

            var initialSpells = _characterState.character.UseSpells;
            for (var i = 0; i < initialSpells.Count; ++i)
            {
                if (initialSpells[i] != null)
                    AddSpellToSlot(i, initialSpells[i], 1);
            }
        }

        internal PlaceOptions GetPickupOptions(Spell spell)
        {
            var slot = GetSpellSlotState((int) spell.DefaultSlot);

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
            Assert.IsTrue(slotIndex >= 0 && slotIndex <= SpellSlots.Length);
            return SpellSlots[slotIndex];
        }

        public void PlaceSpell(Spell spell, int stacks)
        {
            var slot = (int) spell.DefaultSlot;
            var pickupOptions = GetPickupOptions(spell);
            Debug.Log($"Placing spell {spell.name} into slot {slot}. PlaceMode = {pickupOptions}");

            switch (pickupOptions)
            {
                case PlaceOptions.Place:
                    AddSpellToSlot(slot, spell, stacks);
                    break;

                case PlaceOptions.Upgrade:
                    // Upgrade is just a stack count increase
                    UpgradeSpellInSlot(slot, stacks);
                    break;

                case PlaceOptions.Replace:
                    // Current spell is dropped
                    // Todo: Check!
                    AddSpellToSlot(slot, spell, stacks);
                    break;
            }
        }

        private bool FireSpell(int index, Target target)
        {
            Assert.IsTrue(index >= 0 && index < SpellSlots.Length);
            var slotState = GetSpellSlotState(index);
            
            if (slotState.State != SpellState.Ready)
                return false;

            // If already casting
            var handler = _spellCaster.Cast(
                slotState.Spell, 
                new Target(_characterState), 
                target,
                slotState.NumStacks + _characterState.AdditionSpellStacks);
            if (handler == null)
                return false;

            handler.Event += HandlerOnStateChanged;

            // Start cooldown
            slotState.State = SpellState.Preparing;
            slotState.RemainingCooldown = 0;
            slotState.SpellHandler = handler;

            // Animation
            if (_animationController != null)
                _animationController.PlayCastAnimation();

            return true;
        }

        private void HandlerOnStateChanged(ISpellHandler handler, SpellEvent e, ISubSpellHandler subHandler)
        {
            // Even when event if from spell that is already not in slot - decrease hp for cast
            if (e == SpellEvent.SubSpellCasted && subHandler != null)
            {
                _characterState.ApplyModifier(
                    ModificationParameter.HpFlat,
                    -subHandler.SubSpell.BloodCost.GetValue(subHandler.Stacks),
                    _characterState,
                    null);
            }


            for (var i = 0; i < SpellSlots.Length; i++)
            {
                if (handler.Equals(SpellSlots[i].SpellHandler))
                {
                    OnSlotHandlerStateChanged(i, handler, e, subHandler);
                    return;
                }
            }
            
            // If the handler (finished or aborted state) is inactive - unsubscribe
            if (!handler.IsActive)
            {
                // Unsubscribe
                handler.Event -= HandlerOnStateChanged;
            }
        }

        private void OnSlotHandlerStateChanged(int slotIndex, ISpellHandler handler, SpellEvent e, ISubSpellHandler subHandler)
        {
            var slotState = GetSpellSlotState(slotIndex);
            if (e == SpellEvent.StartedFiring)
            {
                slotState.State = SpellState.Firing;
            }
            else if (e == SpellEvent.FinishedFire || e == SpellEvent.Aborted || e == SpellEvent.Ended)
            {
                // If we are ending spell that has not yet started
                if (slotState.State == SpellState.Preparing)
                    slotState.RemainingCooldown = 0.1f;
                else
                    slotState.RemainingCooldown = slotState.Spell.Cooldown.GetValue(slotState.NumStacks);

                // Remove handler from state and start recharging
                slotState.SpellHandler = null;
                slotState.State = SpellState.Recharging;

                if (NoCooldowns)
                    slotState.RemainingCooldown = 0.1f;
            }
        }

        public bool TryFireSpellToTarget(int slotIndex, CharacterState target)
        {
            return TryFireSpellToTarget(slotIndex, new Target(target));
        }

        public bool TryFireSpellToTarget(int slotIndex, Target target)
        {
            return FireSpell(slotIndex, target);
        }

        private void AddSpellToSlot(int slotIndex, Spell spell, int stacks)
        {
            Debug.Log($"Spell {spell.name} placed into slot {slotIndex}");
            var slotState = GetSpellSlotState(slotIndex);
            slotState.Spell = spell;
            slotState.State = SpellState.Ready;
            slotState.RemainingCooldown = 0f;
            slotState.NumStacks = stacks;
        }

        private void UpgradeSpellInSlot(int slotIndex, int stacks)
        {
            // Number of stacks increased
            var slotState = GetSpellSlotState(slotIndex);
            slotState.NumStacks += stacks;
        }

        void Update()
        {
            // Update cooldowns
            for (var slotIndex = 0; slotIndex < SpellSlots.Length; slotIndex++)
            {
                var slotState = GetSpellSlotState(slotIndex);

                if (slotState.RemainingCooldown > 0)
                    slotState.RemainingCooldown = slotState.RemainingCooldown - Time.deltaTime;
                if (slotState.RemainingCooldown <= 0f && slotState.State == SpellState.Recharging)
                {
                    slotState.State = SpellState.Ready;
                }
            }
        }
    }
}