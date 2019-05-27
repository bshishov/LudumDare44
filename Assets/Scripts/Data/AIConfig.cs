using System;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "CharacterConfig", menuName = "Mechanics/AI Config")]
    [Serializable]
    public class AIConfig : ScriptableObject
    {
        public CharacterClass Class;
        public float AggroRange;
        public float FearRange;
        public float MeleeRange;
        public float MaxMeleeRange;

        [Tooltip("Maximum range (in meters) in which an AI can cast any spell. " +
                 "Used to prevent casting way too far from target even if spell allows")]
        public float MaxCastRange;
        
        [Header("Melee")]
        public float MeleeAttackCooldown;
        public float MeleeDamageDelay;

        [Tooltip("The buff that will be applied if the ai will succeed its attack")]
        public Buff MeleeAttackBuff;

        [Serializable]
        public class AISlotConfig
        {
            public int Slot;
            public float Weight;
            public float WaitAfterCast;
        }

        [Header("Spells")]
        public float SpellCastingCooldown;
        public AISlotConfig[] SlotConfig; 
    }
}
