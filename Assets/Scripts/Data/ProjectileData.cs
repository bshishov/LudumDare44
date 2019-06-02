using System;
using Actors;
using Attributes;
using Spells;
using UnityEngine;

namespace Data
{
    public enum ProjectileType
    {
        Directional,
        Targeted,
    }
    
    [Flags]
    public enum ProjectileEvents
    {
        CollisionWithTarget = 1 << 1,
        CollisionWithOtherTargets = 1 << 2,
        CollisionWithWorld = 1 << 3,
        ReachedMaxDistance = 1 << 4,
        TimeExpired = 1 << 5,
        ReachedMaxPiercingTargets = 1 << 6,
        ReachedDestination = 1 << 7
    }

    [Serializable]
    [CreateAssetMenu(fileName = "ProjectileData", menuName = "Spells/Projectile")]
    public class ProjectileData : ScriptableObject
    {
        [Header("Spawn")]
        public GameObject Prefab;
        public CharacterState.NodeRole SpawnNode;
        public CharacterState.NodeRole TargetNode;

        [Header("Movement")]
        public ProjectileType Type;
        public StackableFloat Speed = new StackableFloat(10f);
        public StackableFloat MaxDistance = new StackableFloat(100f);
        public StackableFloat TimeToLive = new StackableFloat(100);
        public StackableFloat MaxPiercingTargets = new StackableFloat(1);
        public bool HoverGround;
        public AnimationCurve HeightProfile = AnimationCurve.Linear(0, 1, 1, 1);

        [Header("Logic")]
        public Query.QueryTeam Affects = Query.QueryTeam.Enemy;
        [EnumFlag] public ProjectileEvents FireSubSpellCondition;
        [EnumFlag] public ProjectileEvents DestroyCondition = ProjectileEvents.ReachedMaxDistance | ProjectileEvents.TimeExpired | ProjectileEvents.ReachedMaxPiercingTargets;
        public float DestroyDelay = 0.1f;
    }
}