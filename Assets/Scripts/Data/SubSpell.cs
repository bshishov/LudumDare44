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
        public enum SpellOrigin : int
        {
            None = 0,
            Self,
            Cursor,
        };

        public SpellOrigin Origin;

        [Serializable]
        public enum SpellTargeting : int
        {
            None = 0,
            Target = 1 << 1,
            Location = 1 << 2,
        };

        [EnumFlag]
        public SpellTargeting Targeting;

        [Serializable]
        [Flags]
        public enum SpellFlags : int
        {
            Undefined = 0,

            HaveDirection = 1 << 1,

            Raycast = 1 << 2,
            Projectile = 1 << 3,

            Special = 1 << 4,
            SelfTarget,
            ClosestTarget,
            SpecialEnd = 1 << 5 - 1,
        }

        [EnumFlag]
        public SpellFlags Flags;        
        
        public float ProjectileSpeed;

        [Serializable]
        [Flags]
        public enum ObstacleHandling : int
        {
            Undefined = 0,

            None = Undefined,

            Activate = 1 << 1,
            Break = 1 << 2,
        };

        [EnumFlag]
        public ObstacleHandling Obstacles;

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
