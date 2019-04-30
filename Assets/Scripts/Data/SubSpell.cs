using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(fileName = "SubSpell", menuName = "Spells/SubSpell")]
    [Serializable]
    public class SubSpell : ScriptableObject
    {
        [Serializable]
        [Flags]
        public enum AffectedTargets
        {
            Self = 1 << 1,
            Friend = 1 << 2,
            Enemy = 1 << 3
        }

        [Serializable]
        [Flags]
        public enum ObstacleHandling
        {
            None = 0,

            ExecuteSpellSequence = 1 << 1,
            Break = 1 << 2,
            ExecuteSpellSequenceOnMaxDistance = 1 << 3,
            IgnoreButTarget = 1 << 4
        }

        [Serializable]
        [Flags]
        public enum SpellFlags
        {
            Undefined = 0,

            HaveDirection = 1 << 1,

            Raycast = 1 << 2,
            Projectile = 1 << 3,

            Special = 1 << 4,
            SelfTarget,
            ClosestTarget,
            SpecialEnd = 1 << (5 - 1)
        }

        [Serializable]
        public enum SpellOrigin
        {
            None = 0,
            Self,
            Cursor
        }

        [Serializable]
        public enum SpellTargeting
        {
            None = 0,
            Target,
            Location
        }

        public SpellOrigin Origin;
        public float PostCastDelay;
        public float PreCastDelay;

        [Header("Flags")]
        [EnumFlag] public SpellFlags Flags;
        [EnumFlag] public AffectedTargets AffectedTarget;
        [EnumFlag] public ObstacleHandling Obstacles;
        [EnumFlag] public SpellTargeting Targeting;

        [Header("Data")]
        public AreaOfEffect Area;
        public ProjectileData Projectile;
        
        [Header("Buffs")]
        [SerializeField] public List<Buff> Buffs;
    }
}