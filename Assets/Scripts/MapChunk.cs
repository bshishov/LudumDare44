using System;
using Assets.Scripts.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

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
        public Transform Exit;
        public bool DrawGizmos;

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
       
        void OnDrawGizmos()
        {
            if(!DrawGizmos)
                return;

            Gizmos.color = Color.red;
            DrawGizmosTransform(Entry);

            Gizmos.color = Color.cyan;
            DrawGizmosTransform(Exit);

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

        void DrawGizmosTransform(Transform t)
        {
            if(t == null)
                return;

            var o = t.position;
            var f = o + t.forward * 2f;
            var r = o + t.right;
            var l = o - t.right;

            Gizmos.DrawSphere(o, 0.2f);
            Gizmos.DrawLine(l, r);
            Gizmos.DrawLine(o, f);
        }

        public void Spawn(GameObject enemy)
        {
            var offset = new Vector3(Random.value * 2f, 0, Random.value * 2f);

            // Todo: Modify spawn
            GameObject.Instantiate(enemy, transform.position + offset, Quaternion.identity);
        }
    }
}
