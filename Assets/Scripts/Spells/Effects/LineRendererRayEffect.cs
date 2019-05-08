using UnityEngine;

namespace Spells.Effects
{
    [RequireComponent(typeof(LineRenderer))]
    public class LineRendererRayEffect : MonoBehaviour, IRayEffect
    {
        public int Points = 10;
        public float SmoothTime = 0.1f;

        private LineRenderer _renderer;
        private Vector3 _target;
        private Vector3 _velocity;

        void Awake()
        {
            _renderer = GetComponent<LineRenderer>();
        }

        private void SetupLine(Vector3 from, Vector3 to)
        {
            var positions = new Vector3[Points];
            for (var i = 0; i < Points; i++)
            {
                positions[i] = Vector3.Lerp(from, to, i / (Points - 1f));
            }

            _renderer.positionCount = Points;
            _renderer.SetPositions(positions);
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
            Destroy(gameObject, 0.1f);
        }
    }
}
