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
            Direction = 1 << 1,
            Target = 1 << 2,
            CastOnSelf = 1 << 3,
            CastOnClosest = 1 << 4,
            DirectionCast = 1 << 5,
            PointCast = 1 << 6,
            TargetCast = 1 << 7,

            Raycast = 1 << 8,
            Projectile = 1 << 9,

            FirstHit = 1 << 10,

            RaycastToFirst = Raycast | FirstHit | DirectionCast,
            RaycastToClosest = Raycast | FirstHit | CastOnClosest,

            ProjectileToFirst = Projectile | FirstHit | DirectionCast,
        };

        [EnumFlag]
        public SpellTargets SpellTarget;

        [EnumFlag]
        public AffectedTargets AffectedTarget;

        public AreaOfEffect Area;

        public float PreCastDelay;
        public float PostCastDelay;

        [Header("Buffs")]
        [SerializeField]
        public List<Buff> Buffs;

        // TODO: set later
        [Header("Visual Effects")]
        public GameObject VisualEffect;
    }
}
