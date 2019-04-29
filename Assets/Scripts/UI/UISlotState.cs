using System;
using Assets.Scripts.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class UISlotState : MonoBehaviour
    {
        public Spell.Slot Slot;
        public Image SpellIcon;
        public Image CooldownOverlay;
        public Text CooldownText;
        public Text StacksText;

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
            var slotState = _spellBookState.GetSpellSlotState((int)Slot);
            
            if (slotState.Spell != null)
            {
                SpellIcon.sprite = slotState.Spell.Icon;
                CooldownOverlay.fillAmount = slotState.RemainingCooldown / slotState.Spell.Cooldown;
                CooldownText.text = string.Format("{0:#.#}", slotState.RemainingCooldown);

                if (slotState.NumStacks > 1)
                {
                    StacksText.text = string.Format("x{0}", slotState.NumStacks);
                }
                else
                {
                    StacksText.text = String.Empty;
                }
            }
        }
    }
}
