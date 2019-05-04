using System;
using JetBrains.Annotations;
using UnityEngine;

public enum ProjectileTrajectory
{
    None = 0,
    Line,
    Follow,
    Falling,
}

public interface IProjectileTrajectory
{
}

[Serializable]
[CreateAssetMenu(fileName = "ProjectileData", menuName = "Spells/Projectile")]
public class ProjectileData : ScriptableObject
{
    [NotNull]
    public GameObject ProjectilePrefab;

    public float Speed       = 10;
    public float MaxDistance = 100;

    public Vector3 Offset;

    public ProjectileTrajectory Trajectory;
    public float                FallingSpeed = 0.0f;
}