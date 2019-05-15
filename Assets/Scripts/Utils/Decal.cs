using UnityEngine;

namespace Utils
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    public class Decal : MonoBehaviour
    {
        private MeshFilter _meshFilter;
        
        void Start()
        {
            _meshFilter = GetComponent<MeshFilter>();
        }

        void OnDrawGizmosSelected()
        {
            if (_meshFilter != null)
            {
                Gizmos.DrawWireMesh(_meshFilter.sharedMesh,
                    0,
                    transform.position,
                    transform.rotation,
                    transform.lossyScale);
            }
            else
            {
                _meshFilter = GetComponent<MeshFilter>();
            }
        }
    }
}
