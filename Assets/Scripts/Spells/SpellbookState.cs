using Assets.Scripts.Data;
using UnityEngine;
using UnityEngine.Assertions;

namespace Spells
{
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

            for (var i = 0; i < SpellCount && i < initialSpells.Count; ++i)
            {
                AddSpellToSlot(i, initialSpells[i]);
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

        public void PlaceSpell(Spell spell)
        {
            var slot = GetSpellSlot(spell);
            var pickupOptions = GetPickupOptions(spell);
            Debug.Log($"Placing spell {spell.Name} into slot {slot}. PlaceMode = {pickupOptions}");

            switch (pickupOptions)
            {
                case PlaceOptions.Place:
                    AddSpellToSlot(GetSpellSlot(spell), spell);
                    break;

                case PlaceOptions.Upgrade:
                    // Upgrade is just a stack count increase
                    UpgradeSpellInSlot(slot);
                    break;

                case PlaceOptions.Replace:
                    // Current spell is dropped
                    // Todo: Check!
                    AddSpellToSlot(GetSpellSlot(spell), spell);
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

        private void FireSpell(int index, SpellEmitterData data)
        {
            Assert.IsTrue(index >= 0 && index <= SpellCount);
            var slotState = GetSpellSlotState(index);
            if (slotState.State == SpellState.Ready)
            {
                _spellCaster.CastSpell(slotState.Spell, data);

                // Start cooldown
                SpellSlots[index].State = SpellState.Recharging;
                SpellSlots[index].RemainingCooldown = slotState.Spell.Cooldown;
            }
        }

        public void TryFireSpellToPoint(int slotIndex, Vector3 targetPosition)
        {
            CharacterState targetCharacter = null;

            // Try locate target character located in target position
            var results = Physics.OverlapSphere(targetPosition, 1f, LayerMask.GetMask("Actors"));
            foreach (var result in results)
            {
                targetCharacter = result.GetComponent<CharacterState>();
                if(targetCharacter != null)
                    break;
            }

            var data = SpellEmitterData.Create(_characterState,
                targetCharacter,
                targetPosition,
                _characterState.GetNodeTransform(CharacterState.CharacterNode.NodeRole.SpellEmitter)
            );

            FireSpell(slotIndex, data);
        }

        public void TryFireSpellToTarget(int slotIndex, CharacterState target)
        {
            var data = new SpellEmitterData
            {
                TargetPosition = target.GetNodeTransform(CharacterState.CharacterNode.NodeRole.Chest).position,
                SourceTransform = _characterState.GetNodeTransform(CharacterState.CharacterNode.NodeRole.SpellEmitter),
                SourceCharacter = _characterState,
                TargetCharacter = target
            };

            FireSpell(slotIndex, data);
        }

        private void AddSpellToSlot(int slotIndex, Spell spell)
        {
            Debug.Log($"Spell {spell.Name} placed into slot {slotIndex}");
            SpellSlots[slotIndex] = new SpellSlotState
            {
                Spell = spell,
                State = SpellState.Ready,
                RemainingCooldown = 0.0f,
                NumStacks = 1
            };
        }

        private void UpgradeSpellInSlot(int slotIndex)
        {
            var slotState = GetSpellSlotState(slotIndex);
            SpellSlots[slotIndex].NumStacks = slotState.NumStacks + 1;
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
