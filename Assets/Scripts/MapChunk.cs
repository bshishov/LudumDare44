using System;
using Assets.Scripts.Utils;
using UnityEngine;
using UnityEngine.AI;

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
        public Transform Entry;
        public Transform EntryA;
        public Transform EntryB;
        public Transform Exit;
        public Transform ExitA;
        public Transform ExitB;
        public AdjacentChunkSettings[] AdjacentChunks;
        public bool DrawGizmos;

        public int OffLinksCount = 6;

        public Vector3 WorldEntryLocation => Entry.position;
        public Vector3 WorldExitLocation => Exit.position;

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
        public MapChunk SpawnNext()
        {
            if (AdjacentChunks == null)
            {
                Debug.LogWarning("Trying to generate next chunk, but there are no adjacent chunks set");
                return null;
            }
            
            var chunkSettings = RandomUtils.Choice(AdjacentChunks, c => c.Weight);
            if (chunkSettings == null)
            {
                Debug.LogWarning("Undefined randomly selected chunk");
                return null;
            }

            var chunk = Instantiate(chunkSettings.Prefab).GetComponent<MapChunk>();
            if (chunk == null)
            {
                Debug.LogWarning("Failed to instantiate next chunk");
                return null;
            }
            
            // Move to match entry and exit
            var offset = WorldExitLocation - chunk.WorldEntryLocation;
            chunk.transform.position += offset;

            // Make seams
            for (var i = 0; i < OffLinksCount; i++)
            {
                var k = (float) i / (OffLinksCount - 1);
                var start = SampleExitLocation(k);
                var end = chunk.SampleEntryLocation(k);
                var link = gameObject.AddComponent<NavMeshLink>();
                link.startPoint = transform.InverseTransformPoint(start);
                link.endPoint = transform.InverseTransformPoint(end);
            }

            return chunk;
        }

        public Vector3 SampleEntryLocation(float k)
        {
            return Vector3.Lerp(EntryA.position, EntryB.position, k);
        }

        public Vector3 SampleExitLocation(float k)
        {
            return Vector3.Lerp(ExitA.position, ExitB.position, k);
        }

        void OnDrawGizmos()
        {
            if(!DrawGizmos)
                return;

            Gizmos.color = Color.cyan;

            // Entry edge
            if (EntryA != null && EntryB != null)
                Gizmos.DrawLine(EntryA.position, EntryB.position);

            // Exit edge
            if (ExitA != null && ExitB != null)
                Gizmos.DrawLine(ExitA.position, ExitB.position);
            

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
