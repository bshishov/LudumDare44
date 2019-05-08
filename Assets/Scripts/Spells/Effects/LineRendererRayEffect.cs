using UnityEngine;

namespace Spells.Effects
{
    [RequireComponent(typeof(LineRenderer))]
    public class LineRendererRayEffect : MonoBehaviour, IRayEffect
    {
        [Header("Visuals")]
        public int Points = 10;
        public float SmoothTime = 0.1f;

        [Header("Lifecycle")]
        public bool AutoDestroy = false;
        public float DestroyAfter = 1f;

        private LineRenderer _renderer;
        private Vector3 _target;
        private Vector3 _velocity;
        private Vector3[] _points;

        void Awake()
        {
            _renderer = GetComponent<LineRenderer>();
            _renderer.positionCount = Points;
            _points = new Vector3[Points];

            if(AutoDestroy)
                Destroy(gameObject, DestroyAfter);
        }

        private void SetupLine(Vector3 from, Vector3 to)
        {
            for (var i = 0; i < Points; i++)
                _points[i] = Vector3.Lerp(from, to, i / (Points - 1f));
            
            _renderer.SetPositions(_points);
        }

        public void RayStarted(Vector3 source, Vector3 destination)
        {
            _target = destination;
            SetupLine(source, _target);
        }

        public void RayUpdated(Vector3 source, Vector3 destination)
        {
            _target = Vector3.SmoothDamp(_target, destination, ref _velocity, SmoothTime);
            SetupLine(source, destination);
        }

        public void RayEnded()
        {
            // TODO: Fadeout
            //Destroy(gameObject, 0.1f);
        }
    }
}
