using System;
using System.Linq;
using UnityEngine;
using Utils;

namespace Assets.Scripts.Data
{
    [Serializable]
    public enum Comparison : int
    {
        GreaterOrEqual,
        Exact,
        LessOrEqual
    }

    [Serializable]
    public class LevelCriteria
    {
        public Comparison Comparison;
        public int Level;
    }

    public enum ChunkType
    {
        Default,
        Start,
        Shop,
        Boss
    }

    [Serializable]
    public class EnemiesConfigurationEntry
    {
        public LevelCriteria LevelCriteria;
        public float SpawnWeight = 1f;
        public float MinSpawnWeight = 0f;
        public float MaxSpawnWeight = 10000f;
        public float SpawnWeightPerLevel = 0;
        public ChunkType ChunkType = ChunkType.Default;

        public float BudgetConsume = 1f;
        public GameObject Prefab;

        public bool MatchesLevelCriteria(int level)
        {
            if (LevelCriteria.Comparison == Comparison.Exact)
                return level == LevelCriteria.Level;

            if (LevelCriteria.Comparison == Comparison.GreaterOrEqual)
                return level >= LevelCriteria.Level;

            if (LevelCriteria.Comparison == Comparison.LessOrEqual)
                return level <= LevelCriteria.Level;

            return false;
        }

        public bool CanSpawn(int level, ChunkType cType, float budgetRemaining)
        {
            return budgetRemaining >= BudgetConsume &&
                   cType == ChunkType && 
                   MatchesLevelCriteria(level);
        }

        public float GetWeight(int level)
        {
            return Mathf.Clamp(SpawnWeight + SpawnWeightPerLevel * level, MinSpawnWeight, MaxSpawnWeight);
        }
    }

    [CreateAssetMenu(menuName = "Data/Enemies configuration", fileName = "enemies")]
    public class EnemiesConfiguration : ScriptableObject
    {
        public EnemiesConfigurationEntry[] Enemies;

        public EnemiesConfigurationEntry Sample(int level, ChunkType cType, float budgetRemaining)
        {
            var items = Enemies.Where(e => e.CanSpawn(level, cType, budgetRemaining)).ToList();
            return RandomUtils.Choice(items, enemy => enemy.GetWeight(level));
        }
    }
}
