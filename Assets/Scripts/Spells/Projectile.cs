using Actors;
using Data;
using UnityEngine;

namespace Spells
{
    public class Projectile : MonoBehaviour
    {
        public bool IsActive { get; private set; }

        private ProjectileData _projectile;
        private SubSpellHandler _handler;
        private float _timer;
        private float _speed;
        private float _maxDistance;
        private float _distanceTraveled;
        private Vector3 _direction;
        private int _hits;

        public void Initialize(SubSpellHandler handler, ProjectileData projectileData)
        {
            _projectile = projectileData;
            _handler = handler;

            var body = GetComponentInChildren<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();

            body.isKinematic = false;
            body.useGravity = false;

            var targetPos = handler.Target.Position;
            var position = transform.position;
            _direction = (targetPos - position).normalized;

            position += Quaternion.LookRotation(_direction) * _projectile.Offset;
            transform.position = AboveGround(position, _projectile.HoverHeight);

            _direction = (targetPos - position).normalized;
            transform.LookAt(targetPos);

            _timer = projectileData.TimeToLive.GetValue(handler.SpellHandler.Stacks);
            _speed = projectileData.Speed.GetValue(handler.SpellHandler.Stacks);
            _maxDistance = projectileData.MaxDistance.GetValue(handler.SpellHandler.Stacks);

            IsActive = true;
        }

        private void Update()
        {
            if(!IsActive)
                return;

            if (_timer > 0)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                    HandleEvent(ProjectileEvents.TimeExpired, new Target(transform.position));
            }

            var moveDistance = _speed * Time.deltaTime;

            switch (_projectile.Trajectory)
            {
                case ProjectileTrajectory.StraightLine:
                    _distanceTraveled += moveDistance;
                    transform.position += _direction * moveDistance;
                    break;

                case ProjectileTrajectory.Follow:
                    _distanceTraveled += moveDistance;
                    transform.position += transform.position + (_handler.Target.Transform.position - transform.position).normalized * moveDistance;
                    break;

                case ProjectileTrajectory.Falling:
                    if (IsNearGround(transform.position, _projectile.HoverHeight))
                    {
                        _direction.y = 0;
                        _direction = _direction.normalized;
                        _projectile.Trajectory = ProjectileTrajectory.StraightLine;
                    }
                    else
                    {
                        transform.position += _direction * _projectile.FallingSpeed * Time.deltaTime;
                    }
                    break;
            }

            if (_maxDistance > 0 && _distanceTraveled > _maxDistance)
            {
                HandleEvent(ProjectileEvents.ReachedMaxDistance, new Target(transform.position));
            }
        }

        private void FixedUpdate()
        {
            // Position fix
            transform.position = AboveGround(transform.position, _projectile.HoverHeight);
        }

        private void OnTriggerEnter(Collider other)
        {
            if(!IsActive)
                return;

            var character = other.GetComponent<CharacterState>();
            if (character == null)
            {
                // Collision with non-character object.
                HandleEvent(ProjectileEvents.CollisionWithWorld, new Target(other.transform.position));
            }
            else if (character.Equals(_handler.Target.Character))
            {
                if (SpellCaster.IsValidTeam(_handler.SpellHandler.Source.Character, character, _projectile.Affects))
                {
                    // Collision with target character object
                    HandleEvent(ProjectileEvents.CollisionWithTarget, new Target(character));
                }
            }
            else
            {
                if (SpellCaster.IsValidTeam(_handler.SpellHandler.Source.Character, character, _projectile.Affects))
                {
                    // Collision with non-target character object
                    HandleEvent(ProjectileEvents.CollisionWithOtherTargets, new Target(character));
                }
            }
        }

        private void HandleEvent(ProjectileEvents e, Target target)
        {
            if(!IsActive)
                return;

            // Piercing
            if (e == ProjectileEvents.CollisionWithTarget || e == ProjectileEvents.CollisionWithOtherTargets)
            {
                _hits += 1;
                if (_hits > _projectile.MaxPiercingTargets.GetValue(_handler.Stacks))
                    HandleEvent(ProjectileEvents.ReachedMaxPiercingTargets, target);
            }

            if (_projectile.FireSubSpellCondition.HasFlag(e))
                _handler.HandleProjectileFireEvent(target);

            if (_projectile.DestroyCondition.HasFlag(e))
            {
                HandleDestroy();
                _handler.HandleProjectileDestroyEvent(target);
            }
        }

        private void HandleDestroy()
        {
            Destroy(gameObject);
            IsActive = false;
        }

        public static Vector3 AboveGround(Vector3 position, float hoverHeight = 0f)
        {
            if (Physics.Raycast(position + Vector3.up, Vector3.down, out var hit, 10f, Common.LayerMasks.Ground))
                return hit.point + Vector3.up * hoverHeight;
            
            return position;
        }

        public static bool IsNearGround(Vector3 position, float testHeight = 0.5f)
        {
            return Physics.Raycast(position + Vector3.up, Vector3.down, testHeight, Common.LayerMasks.Ground);
        }
    }
}
