using System;
using Attributes;
using Spells;
using UnityEngine;

namespace Data
{
    public enum ProjectileTrajectory
    {
        StraightLine,
        Follow,
        Falling,
    }

    [Flags]
    public enum ProjectileEvents
    {
        CollisionWithTarget = 1 << 1,
        CollisionWithOtherTargets = 1 << 2,
        CollisionWithWorld = 1 << 3,
        ReachedMaxDistance = 1 << 4,
        TimeExpired = 1 << 5,
        ReachedMaxPiercingTargets = 1 << 6
    }

    [Serializable]
    [CreateAssetMenu(fileName = "ProjectileData", menuName = "Spells/Projectile")]
    public class ProjectileData : ScriptableObject
    {
        public GameObject Prefab;
        public StackableFloat Speed = new StackableFloat(10f);
        public StackableFloat MaxDistance = new StackableFloat(100f);
        public StackableFloat TimeToLive = new StackableFloat(100);
        public StackableFloat MaxPiercingTargets = new StackableFloat(1);
        public Vector3 Offset;
        public ProjectileTrajectory Trajectory;
        public float FallingSpeed = 0.0f;
        public float HoverHeight = 1f;

        [Header("Logic")]
        public Query.QueryTeam Affects = Query.QueryTeam.Enemy;
        [EnumFlag] public ProjectileEvents FireSubSpellCondition;
        [EnumFlag] public ProjectileEvents DestroyCondition = ProjectileEvents.ReachedMaxDistance | ProjectileEvents.TimeExpired | ProjectileEvents.ReachedMaxPiercingTargets;
    }
}