using System;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [CreateAssetMenu(menuName="Data/Map Chunk", fileName="chunk")]
    public class MapChunkData : ScriptableObject
    {
        [Serializable]
        public class MapChunkDataSpawnSettings
        {
            public MapChunkData Chunk;
            public float Weight = 1f;
        }

        public GameObject Prefab;

        public ChunkType Type = ChunkType.Default;
        public float EnemyBudgetModifier = 1f;
        public EnemiesConfiguration Enemies;
        public MapChunkDataSpawnSettings[] Adjacent;

        public bool HasAdjacent
        {
            get
            {
                if (Adjacent == null)
                    return false;
                if (Adjacent.Length == 0)
                    return false;
                return true;
            }
        }
    }
}
