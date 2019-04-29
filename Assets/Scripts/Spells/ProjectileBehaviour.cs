using UnityEngine;
using UnityEngine.Serialization;

namespace Spells
{
    public class ProjectileBehaviour : MonoBehaviour
    {
        private ProjectileContext _context;
        private float _trevaledDistance;

        public void Initialize(ProjectileContext context)
        {
            if (context == null)
            {
                Destroy(gameObject);
                return;
            }

            _context = context;
            transform.LookAt(_context.target);

            var sphere = gameObject.AddComponent<Rigidbody>();
            sphere.isKinematic = false;
            sphere.useGravity = false;

            switch (_context.projectileData.Trajectory)
            {
                case ProjectileTrajectory.Line:
                    break;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("Particle Collision");
        }

        void Update()
        {
            switch (_context.projectileData.Trajectory)
            {
                case ProjectileTrajectory.Line:
                    var moveDistance = _context.projectileData.Speed * Time.deltaTime;
                    _trevaledDistance += moveDistance;
                    transform.position += transform.forward * moveDistance;
                    break;
            }

            if (_context.projectileData.MaxDistance > 0 && _trevaledDistance > _context.projectileData.MaxDistance)
            {
                Destroy(gameObject);
            }
        }
    }
}
