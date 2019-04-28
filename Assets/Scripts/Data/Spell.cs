using UnityEngine;
using System;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(fileName = "Spell", menuName = "Mechanics/Spell")]
    [Serializable]
    public class Spell : ScriptableObject
    {
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
            BreakOnFailedTargeting = 1 << 1
        };

        public string Name;
        public Sprite Icon;

        public int BloodCost = 0;
        public int BloodBack = 0;

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

        public ISpellEffect GetEffect()
        {
            if (SpellEffect == null)
                return null;

            return GameObject.Instantiate(SpellEffect).GetComponent<ISpellEffect>();
        }
    }
}