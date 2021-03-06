﻿using System;
using Actors;
using Data;
using Spells;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UISlotState : MonoBehaviour
    {
        public SpellSlot Slot;
        public Sprite DefaultSpellIcon;
        public Image SpellIcon;
        public Image CooldownOverlay;
        public Image ActiveOverlay;
        public TextMeshProUGUI CooldownText;
        public TextMeshProUGUI StacksText;

        private SpellbookState _spellBookState;
        private Color _activeColor;

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

            SpellIcon.sprite = DefaultSpellIcon;
            StacksText.text = String.Empty;
            CooldownText.text = String.Empty;

            if(ActiveOverlay != null)
                _activeColor = ActiveOverlay.color;
        }
        
        void Update()
        {
            var slotState = _spellBookState.GetSpellSlotState((int)Slot);
            if(slotState == null)
                return;
            
            if (slotState.Spell != null && slotState.State != SpellbookState.SlotState.None)
            {
                SpellIcon.sprite = slotState.Spell.Icon;
                CooldownOverlay.fillAmount = slotState.RemainingCooldown / slotState.Spell.Cooldown.GetValue(slotState.NumStacks);

                if(slotState.RemainingCooldown > 0)
                    CooldownText.text = string.Format("{0:0.#}", slotState.RemainingCooldown);
                else
                    CooldownText.text = String.Empty;

                if (slotState.NumStacks > 1)
                {
                    StacksText.text = string.Format("x{0}", slotState.NumStacks);
                }
                else
                {
                    StacksText.text = String.Empty;
                }
            }
            else
            {
                SpellIcon.sprite = DefaultSpellIcon;
            }

            // Todo: make it more fancy, animations and stuff
            if (ActiveOverlay != null)
            {
                if (slotState.State == SpellbookState.SlotState.Firing)
                    ActiveOverlay.color = _activeColor;
                else
                    ActiveOverlay.color = new Color(0, 0, 0, 0);
            }
        }
    }
}
