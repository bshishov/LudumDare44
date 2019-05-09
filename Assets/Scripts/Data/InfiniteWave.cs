using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Data
{
    [Serializable]
    public class InfiniteWaveEntry
    {
        public SpawnEntry[] Items;
        public float DelayBeforeNextPack;
        public int Difficulty;
        public int MaxNumber = 5;

        public int TotalNumberOfEnemies(int spawnPointsCount, int difficulty)
        {
            if (Difficulty >= difficulty)
                return 0;

            var multiplier = Mathf.Min(difficulty - Difficulty, MaxNumber);
            var total = Items.Sum(e => e.GetTotal(spawnPointsCount));
            return total * multiplier;
        }
    }

    [CreateAssetMenu(fileName = "infinite_wave", menuName = "Gameplay/Infinite Wave")]
    public class InfiniteWave : ScriptableObject
    {
        public List<InfiniteWaveEntry> Packs;

        public int TotalNumberOfEnemies(int spawnPointsCount, int difficulty)
        {
            return Packs.Sum(p => p.TotalNumberOfEnemies(spawnPointsCount, difficulty));
        }
    }
}