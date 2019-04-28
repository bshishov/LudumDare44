using UnityEngine;
using System.Collections;
using System;
using UnityEngine.AI;

public static class CharacterUtils
{
    public static float speedDistorshen = 0.3f;

    internal static void ApplaySettings(CharacterState characterParams, NavMeshAgent navMeshAgent, bool randomize)
    {
        navMeshAgent.speed = navMeshAgent.speed;
        if(randomize)
        {
            var speedMultiplier = UnityEngine.Random.Range(1.0f - speedDistorshen, 1.0f + speedDistorshen);
            navMeshAgent.speed *= speedMultiplier;
        }
    }
}
