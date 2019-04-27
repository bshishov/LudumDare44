using System;
using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.Scripts
{
    public class MapChunk : MonoBehaviour
    {    
        [Serializable]
        public class EnemySpawnSettings
        {
            public Vector3 Position;
            // More data will be here
        }

        [Serializable]
        public class AdjacentChunkSettings
        {
            public GameObject Prefab;
            public float Weight = 1f;
        }

        public Bounds LocalSpaceBounds;
        public EnemySpawnSettings[] EnemySpawns;
        // Local entry location
        public Vector3 EntryLocation;

        // Local exit location
        public Vector3 ExitLocation;
        public AdjacentChunkSettings[] AdjacentChunks;

        public Vector3 WorldEntryLocation => transform.TransformPoint(EntryLocation);
        public Vector3 WorldExitLocation => transform.TransformPoint(ExitLocation);

        void Start()
        {
        }
    
        [ContextMenu("Calculate Bounds")]
        public void CalculateBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                LocalSpaceBounds.Encapsulate(r.bounds);
            }
        }

        [ContextMenu("Spawn next chunk")]
        public void SpawnNext()
        {
            if (AdjacentChunks == null)
            {
                Debug.LogWarning("Trying to generate next chunk, but there are no adjacent chunks set");
                return;
            }
            
            var chunkSettings = RandomUtils.Choice(AdjacentChunks, c => c.Weight);
            if (chunkSettings == null)
            {
                Debug.LogWarning("Undefined randomly selected chunk");
                return;
            }

            var chunk = Instantiate(chunkSettings.Prefab).GetComponent<MapChunk>();
            if (chunk == null)
            {
                Debug.LogWarning("Failed to instantiate next chunk");
                return;
            }
            
            // Move to match entry and exit
            var offset = WorldExitLocation - chunk.WorldEntryLocation;
            chunk.transform.position += offset;
        }

        void OnDrawGizmos()
        {
            // Entry location
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.TransformPoint(EntryLocation), .2f);

            // Exit location
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(transform.TransformPoint(ExitLocation), .2f);

            // Enemy spawns
            Gizmos.color = Color.red;
            if (EnemySpawns != null)
            {
                foreach (var spawn in EnemySpawns)
                {
                    Gizmos.DrawSphere(transform.TransformPoint(spawn.Position), .2f);
                }
            }

            // Bounds
            // Todo: figure out local transformation (scale and rotation of BB)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.TransformPoint(LocalSpaceBounds.center), LocalSpaceBounds.size);
        }
    }
}
