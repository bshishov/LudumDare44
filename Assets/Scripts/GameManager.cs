using Assets.Scripts.Data;
using UnityEngine;
using Assets.Scripts.Utils;
using Assets.Scripts.Utils.Debugger;
using UnityEngine.AI;

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
            _player = GameObject.FindGameObjectWithTag(Common.Tags.Player);

            SpawnNextChunk();

            Debugger.Default.Display("GameManager/Spawn next chunk", SpawnNextChunk);
            Debugger.Default.Display("GameManager/Pause", Pause);
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

            var position = Vector3.zero;
            var rotation = Quaternion.identity;

            if (_lastChunk != null)
            {
                position = _lastChunk.Exit.position;
                rotation = _lastChunk.Exit.rotation;
            }

            var pMapChunk = chunkData.Prefab.GetComponent<MapChunk>();

            var chunk = Instantiate(chunkData.Prefab, 
                    position - pMapChunk.EntryPosition,
                    rotation * Quaternion.Inverse(pMapChunk.EntryRotation))
                .GetComponent<MapChunk>();
            if (chunk == null)
            {
                Debug.LogWarning("Failed to instantiate next chunk");
                return;
            }
            
            chunk.name = $"Chunk_{_chunksSpawned}";
            
            _lastChunk = chunk;
            _lastChunkData = chunkData;
            Debugger.Default.Display("Last chunk position", _lastChunk.transform.position.ToString());

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

        [ContextMenu("Pause")]
        public void Pause()
        {
            Time.timeScale = 1f - Time.timeScale;
        }
    }
}
