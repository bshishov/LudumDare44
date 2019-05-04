using UnityEngine;
using System;
using Spells;
using Utils;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(fileName = "Spell", menuName = "Spells/Spell")]
    [Serializable]
    public class Spell : ScriptableObject
    {
        [Serializable]
        public enum Slot: int
        {
            LMB = 0,
            RMB = 1,
            ULT = 2,
        }

        [Serializable]
        [Flags]
        public enum CastType : int
        {
            Normal = 1 << 0,
            Delayed = 1 << 1,
            Channeling = 1 << 2
        };

        [Serializable]
        [Flags]
        public enum SpellFlags : int
        {
            None = 1 << 0,
            BreakOnFailedTargeting = 1 << 1,
            AffectsOnlyOnce = 1 << 2,
        };

        [Header("Meta")]
        public string Name;
        public Sprite Icon;
        [TextArea]
        public string Description;

        public Buff ApplyBuffOnDismantle;

        [Tooltip("LifeSteal is in range from 0 to +inf. '0' means 0% lifesteal. 1 means 100% lifesteal. 1.5 = 150%")]
        [Percentage(0, 1)]
        public float LifeSteal = 0f;
        public float BloodCost = 0f;

        public Slot DefaultSlot;
        public float Cooldown;

        [EnumFlag]
        public CastType CastingTime;
        [EnumFlag]
        public SpellFlags Flags;

        public float PreCastDelay;
        public float PostCastDelay;
        public float ChannelTime;

        public SubSpell[] SubSpells;

        [Header("FX")]
        public GameObject SpellEffect;

        public GameObject DropItem;

        private GameObject _effect = null;

        public ISpellEffect GetEffect()
        {
            if (SpellEffect == null)
                return null;

            if (_effect == null)
                _effect = Instantiate(SpellEffect);
            return _effect.GetComponent<ISpellEffect>();
        }

        public void DestoryEffectInstance()
        {
            if (_effect == null)
                return;

            Destroy(_effect, 2);
        }
    }
}