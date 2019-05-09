using System;
using UnityEngine;

namespace Data
{
    [Serializable]
    public class SpawnEntry
    {
        public GameObject EnemyPrefab;
        public int NumberOfThisType = 1;
        public float DelayBetween = 0.1f;
        public float DelayBeforeNext = 0.5f;
        public int SpawnIndex = 0;

        public int GetTotal(int spawnPoints)
        {
            if (SpawnIndex < 0)
                return NumberOfThisType * spawnPoints;
            return NumberOfThisType;
        }
    }
}