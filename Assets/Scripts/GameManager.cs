using Assets.Scripts.Data;
using UnityEngine;
using Assets.Scripts.Utils;
using Assets.Scripts.Utils.Debugger;
using UnityEngine.AI;
using UnityEngine.Assertions;

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
            Assert.raiseExceptions = false;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            _surface = GetComponent<NavMeshSurface>();
            _player = GameObject.FindGameObjectWithTag(Common.Tags.Player);

            Debugger.Default.Display("GameManager/Spawn next chunk", SpawnNextChunk);
            Debugger.Default.Display("GameManager/Pause", Pause);
            
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
                if (distanceToExit < 30f)
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

            // World-spaced entry position and rotation
            Vector3 entryPosition;
            Quaternion entryRotation;

            if (_lastChunk != null)
            {
                entryPosition = _lastChunk.ExitPosition;
                entryRotation = _lastChunk.ExitRotation;
            }
            else
            {
                entryPosition = transform.position;
                entryRotation = transform.rotation;
            }
            

            var pMapChunk = chunkData.Prefab.GetComponent<MapChunk>();

            // Instantiate new chunk so that exit of the last chunk matches the entry of the new one
            // Assuming that prefabs are at zero

            /*
            var chunkObject = Instantiate(chunkData.Prefab,
                entryPosition - pMapChunk.EntryPosition,
                entryRotation * Quaternion.Inverse(pMapChunk.EntryRotation));
                */
            
            var entryOffset = pMapChunk.EntryPosition;
            var entryRotationOffset = pMapChunk.EntryRotation;

            var chunkObject = Instantiate(chunkData.Prefab,
                entryPosition + entryRotation * Quaternion.Inverse(entryRotationOffset) * (-pMapChunk.EntryPosition),
                entryRotation * Quaternion.Inverse(entryRotationOffset));

            var chunk = chunkObject.GetComponent<MapChunk>();
            
            if (chunk == null)
            {
                Debug.LogWarning("Failed to instantiate next chunk");
                return;
            }
            
            chunk.name = $"Chunk_{_chunksSpawned}";

            

            _lastChunk = chunk;
            _lastChunkData = chunkData;
            Debugger.Default.Display("Last chunk position", _lastChunk.transform.position.ToString());
            Debugger.Default.DrawCircleSphere(chunk.EntryPosition, 2f, Color.blue, 1000f);
            Debugger.Default.DrawAxis(chunk.EntryPosition, chunk.EntryRotation, 1000f);

            Debugger.Default.DrawCircleSphere(chunk.ExitPosition, 2f, Color.red, 1000f);
            Debugger.Default.DrawAxis(chunk.ExitPosition, chunk.ExitRotation, 1000f);

            Debugger.Default.DrawCircleSphere(chunk.transform.position, 2f, Color.white, 1000f);
            Debugger.Default.DrawAxis(chunk.transform.position, chunk.transform.rotation, 1000f);

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
