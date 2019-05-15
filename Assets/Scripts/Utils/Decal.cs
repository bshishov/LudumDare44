using UnityEngine;

namespace Utils
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    public class Decal : MonoBehaviour
    {
        private MeshRenderer _renderer;
        private Camera _camera;
        
        void Start()
        {
            _renderer = GetComponent<MeshRenderer>();
            _camera = Camera.main;
        }
        
        void Update()
        {
            var cam = _camera;
            var m = _renderer.localToWorldMatrix;
            var v = cam.worldToCameraMatrix;
            var p = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);
            var mvp = p * v * m;
            _renderer.sharedMaterial.SetMatrix("_invMVP", mvp.inverse);
        }
    }
}
