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

        [EnumFlag]
        public AffectedTargets AffectedTarget;

        [Serializable]
        public enum SpellTargets : int
        {
            Undefined = 0,
            Direction,
            Location,
            CastOnSelf,
            CastOnClosest,
        }

        [EnumFlag]
        public SpellTargets SpellTarget;

        [Serializable]
        [Flags]
        public enum ProjectileType : int
        {
            Undefined = 0,

            None = Undefined,

            Raycast = 1 << 1,
            Projectile = 1 << 2,
        };

        [EnumFlag]
        public ProjectileType Projectile;
        public float ProjectileSpeed;

        [Serializable]
        [Flags]
        public enum ProjectileObstacles : int
        {
            Undefined = 0,

            None = Undefined,

            Ignore = 1 << 1,
            Activate = 1 << 2,
            Break = 1 << 3,
        };

        [EnumFlag]
        public ProjectileObstacles Obstacles;

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
