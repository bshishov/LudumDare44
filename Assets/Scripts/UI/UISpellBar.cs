using Actors;
using Spells;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UISpellBar : MonoBehaviour
    {
        [Header("LMB")]
        public Image Slot0Image;
        public Image Slot0Cooldown;

        [Header("RMB")]
        public Image Slot1Image;
        public Image Slot1Cooldown;

        [Header("ULT")]
        public Image Slot2Image;
        public Image Slot2Cooldown;

        private SpellbookState _spellBookState;

        void Start()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                _spellBookState = player.GetComponent<SpellbookState>();
                if (_spellBookState == null)
                {
                    Debug.LogWarning("PlayerState not found");
                }
            }
        }

        void Update()
        {
            if(_spellBookState == null)
                return;

            if (Slot0Image != null && Slot0Cooldown != null)
                UpdateSlotImage(Slot0Image, Slot0Cooldown, _spellBookState.GetSpellSlotState(0));

            if (Slot1Image != null && Slot0Cooldown != null)
                UpdateSlotImage(Slot1Image, Slot1Cooldown, _spellBookState.GetSpellSlotState(1));

            if (Slot2Image != null && Slot2Cooldown != null)
                UpdateSlotImage(Slot2Image, Slot2Cooldown, _spellBookState.GetSpellSlotState(2));
        }

        private void UpdateSlotImage(Image icon, Image cooldownMask, SpellbookState.SpellSlotState slotState)
        {
            if (slotState.Spell != null)
            {
                icon.sprite = slotState.Spell.Icon;
                cooldownMask.fillAmount = slotState.RemainingCooldown / slotState.Spell.Cooldown.GetValue(slotState.NumStacks);
            }
        }
    }
}
