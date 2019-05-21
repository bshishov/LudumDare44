using System;
using Assets.Scripts.Utils;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace Assets.Scripts
{
    public class MapChunk : MonoBehaviour
    {
        [Serializable]
        public class EnemySpawner
        {
            public Vector3 Position;

            [Range(0.1f, 10f)]
            public float Radius = 1f;

            public float Weight = 1f;
        }

        public Bounds LocalSpaceBounds;
        public EnemySpawner[] EnemySpawns;
        public Transform Entry;
        public Transform Exit;
        public bool DrawGizmos;

        public Vector3 EntryPosition
        {
            get
            {
                if (Entry != null)
                    return Entry.position;
                return Vector3.zero;
            }
        }

        public Quaternion EntryRotation
        {
            get
            {
                if (Entry != null)
                    return Entry.rotation;
                return Quaternion.identity;
            }
        }

        public Vector3 ExitPosition
        {
            get
            {
                if (Exit != null)
                    return Exit.position;
                return Vector3.zero;
            }
        }

        public Quaternion ExitRotation
        {
            get
            {
                if (Exit != null)
                    return Exit.rotation;
                return Quaternion.identity;
            }
        }

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
       
        void OnDrawGizmosSelected()
        {
            if(!DrawGizmos)
                return;

            Gizmos.color = Color.red;
            DrawGizmosTransform(Entry);

            Gizmos.color = Color.cyan;
            DrawGizmosTransform(Exit);

            // Bounds
            // Todo: figure out local transformation (scale and rotation of BB)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.TransformPoint(LocalSpaceBounds.center), LocalSpaceBounds.size);

#if UNITY_EDITOR
            if (EnemySpawns != null)
            {
                foreach (var enemySpawner in EnemySpawns)
                {
                    UnityEditor.Handles.color = Color.blue;
                    UnityEditor.Handles.DrawWireDisc(transform.TransformPoint(enemySpawner.Position), Vector3.up,
                        enemySpawner.Radius);
                }
            }
#endif
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

        public void Spawn(GameObject enemyPrefab)
        {
            if (EnemySpawns == null || EnemySpawns.Length == 0)
            {
                Debug.LogWarningFormat("Enemy Spawns is not set for: {0}", name);
                return;
            }

            var spawner = RandomUtils.Choice(EnemySpawns, s => s.Weight);

            var offset2d = Random.insideUnitCircle;
            var offset = new Vector3(offset2d.x, 0, offset2d.y) * spawner.Radius;

            // Todo: Modify spawn
            var enemy = Instantiate(enemyPrefab, transform.TransformPoint(spawner.Position) + offset, Quaternion.identity);
        }
    }
}
