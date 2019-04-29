using System;
using JetBrains.Annotations;
using UnityEngine;

public enum ProjectileTrajectory
{
    None = 0,
    Line
}

public interface IProjectileTrajectory
{
}

[Serializable]
[CreateAssetMenu(fileName = "ProjectileData", menuName = "Spells/Projectile")]
public class ProjectileData : ScriptableObject
{
    [NotNull] public Transform ProjectilePrefab;

    public float Speed;

    public ProjectileTrajectory Trajectory;
}