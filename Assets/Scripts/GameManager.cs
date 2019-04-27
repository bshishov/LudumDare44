using UnityEngine;
using Assets.Scripts.Utils;

namespace Assets.Scripts
{
    public class GameManager : Singleton<GameManager>
    {
        public MapChunk StartChunk;
        private MapChunk _lastChunk;

        void Start()
        {
            if (StartChunk == null)
            {
                StartChunk = FindObjectOfType<MapChunk>();
            }

            _lastChunk = StartChunk;
        }
        
        void Update()
        {
        
        }

        [ContextMenu("Spawn Next Chunk")]
        public void SpawnNextChunk()
        {
            if (_lastChunk != null)
            {
                _lastChunk = _lastChunk.SpawnNext();
            }
        }
    }
}
