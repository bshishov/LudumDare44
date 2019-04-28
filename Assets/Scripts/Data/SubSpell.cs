using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(fileName = "SubSpell", menuName = "Mechanics/SubSpell")]
    [Serializable]
    public class SubSpell : ScriptableObject
    {
        [Serializable]
        [Flags]
        public enum AffectedTargets : int
        {
            Self = 1 << 1,
            Friend = 1 << 2,
            Enemy = 1 << 3
        };

        [Serializable]
        [Flags]
        public enum SpellTargets : int
        {
            Self = 1 << 1,
            Raycast = 1 << 2,
            FirstHit = 1 << 3,
            Closest = 1 << 3,
        };

        [EnumMask]
        public SpellTargets SpellTarget;

        [EnumFlag]
        public AffectedTargets AffectedTarget;

        public AreaOfEffect Area;

        [Header("Buffs")]
        [SerializeField]
        public List<Buff> Buffs;

        // TODO: set later
        [Header("Visual Effects")]
        public GameObject VisualEffect;
    }
}
