using UnityEngine;
using Assets.Scripts.Utils;
using UnityEngine.AI;

namespace Assets.Scripts
{
    [RequireComponent(typeof(NavMeshSurface))]
    public class GameManager : Singleton<GameManager>
    {
        public MapChunk StartChunk;
        public bool AutoSpawnChunks = true;

        private MapChunk _lastChunk;
        private NavMeshSurface _surface;
        private GameObject _player;

        void Start()
        {
            if (StartChunk == null)
            {
                StartChunk = FindObjectOfType<MapChunk>();
            }

            _lastChunk = StartChunk;
            _surface = GetComponent<NavMeshSurface>();
            _surface.BuildNavMesh();
            _player = GameObject.FindGameObjectWithTag(Tags.Player);
        }
        
        void Update()
        {
            if (AutoSpawnChunks && _player != null && _lastChunk != null && _lastChunk.HasAdjacent)
            {
                var distanceToExit = Vector3.Distance(_lastChunk.Exit.position, _player.transform.position);
                if (distanceToExit < 20f)
                {
                    SpawnNextChunk();
                }
            }
        }

        [ContextMenu("Spawn Next Chunk")]
        public void SpawnNextChunk()
        {
            if (_lastChunk != null)
            {
                _lastChunk = _lastChunk.SpawnNext();
                _surface.BuildNavMesh();
            }
        }

        void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 200, 40), "NEXT"))
            {
                SpawnNextChunk();
            }
        }
    }
}
