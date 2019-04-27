using Assets.Scripts.Data;
using UnityEngine;
using Assets.Scripts.Utils;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace Assets.Scripts
{
    [RequireComponent(typeof(NavMeshSurface))]
    public class GameManager : Singleton<GameManager>
    {
        public float CurrentLevelBudget => Level * BudgetPerLevel;

        public MapChunkData StartChunk;
        public bool AutoSpawnChunks = true;
        public float BudgetPerLevel = 100;
        public int Level = 1;

        private MapChunk _lastChunk;
        private MapChunkData _lastChunkData;
        private NavMeshSurface _surface;
        private GameObject _player;
        private int _chunksSpawned = 0;
        
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            _surface = GetComponent<NavMeshSurface>();
            _player = GameObject.FindGameObjectWithTag(Tags.Player);

            SpawnNextChunk();
        }
        
        void Update()
        {
            if (AutoSpawnChunks && 
                _player != null && 
                _lastChunk != null &&
                _lastChunkData != null &&
                _lastChunkData.HasAdjacent)
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
            if (_lastChunkData != null)
            {
                SpawnNextChunk(RandomUtils.Choice(_lastChunkData.Adjacent, cd => cd.Weight).Chunk);
            }
            else
            {
                SpawnNextChunk(StartChunk);
            }
        }

        public void SpawnNextChunk(MapChunkData chunkData)
        {
            if (chunkData == null)
            {
                Debug.LogWarningFormat("Chunk is null");
                return;
            }

            var chunk = GameObject.Instantiate(chunkData.Prefab).GetComponent<MapChunk>();
            if (chunk == null)
            {
                Debug.LogWarning("Failed to instantiate next chunk");
                return;
            }
            
            chunk.name = $"Chunk_{_chunksSpawned}";

            if (_lastChunk != null)
            {
                var offset = _lastChunk.WorldExitLocation - chunk.WorldEntryLocation;
                chunk.transform.position += offset;
            }
            
            _lastChunk = chunk;
            _lastChunkData = chunkData;

            _surface.BuildNavMesh();

            SpawnEnemies(_lastChunkData, _lastChunk);
            _chunksSpawned += 1;
        }

        void SpawnEnemies(MapChunkData chunkData, MapChunk chunk)
        {
            var budget = CurrentLevelBudget;
            if (chunkData.Enemies != null)
            {
                while (budget > 0)
                {
                    var enemy = chunkData.Enemies.Sample(Level, chunkData.Type, budget);
                    if (enemy == null)
                    {
                        Debug.LogWarning("Cant spawn more enemies");
                        break;
                    }
                    chunk.Spawn(enemy.Prefab);
                    budget -= enemy.BudgetConsume;
                }
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
