using Actors;
using Data;
using UnityEngine;
using UnityEngine.AI;
using Utils.Debugger;

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
        private int _numberOfHits;
        private Vector3 _spawnPoint;
        private Target _target;
        private Vector3 _lastValidTargetPosition;

        private const float DestinationThreshold = 0.2f;

        public void Initialize(SubSpellHandler handler, ProjectileData projectileData)
        {
            _projectile = projectileData;
            _handler = handler;
            
            // Setup RigidBody to handle collisions
            var body = GetComponentInChildren<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = false;

            // Bake source point
            if (handler.Source.Type == TargetType.Character)
                _spawnPoint = handler.Source.Character.GetNodeTransform(_projectile.SpawnNode).position;
            else
                _spawnPoint = handler.Source.Transform.position;
            
            if (_projectile.Type == ProjectileType.Targeted && handler.Target.IsValid)
            {
                TargetUtility.DebugDraw(handler.Target, Color.blue);
                
                // ReTargeting to specific transform of character
                if (handler.Target.Type == TargetType.Character)
                    _target = new Target(handler.Target.Character.GetNodeTransform(_projectile.TargetNode));
                else
                    _target = handler.Target;
            }

            // Get initial direction
            if (_projectile.Type == ProjectileType.Directional)
            {
                var direction = handler.Source.Forward;
                
                // If target is specified, use it
                if (handler.Target.HasPosition)
                {
                    direction = handler.Target.Position - _spawnPoint;
                    direction.y = 0;
                    direction.Normalize();
                }
                
                NavMesh.Raycast(_spawnPoint, _spawnPoint + direction * 100f, out var hit, NavMesh.AllAreas);
                _target = new Target(hit.position);
            }

            // Resolve stacked properties
            _timer = projectileData.TimeToLive.GetValue(handler.Stacks);
            _speed = projectileData.Speed.GetValue(handler.Stacks);
            _maxDistance = projectileData.MaxDistance.GetValue(handler.Stacks);
            
            // Initial positioning
            transform.position = _spawnPoint;
            //transform.rotation = Quaternion.LookRotation(_direction);
            
            IsActive = true;
        }

        private void Update()
        {
            if(!IsActive)
                return;
                
            // Lifetime check
            if (_timer > 0)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                    HandleEvent(ProjectileEvents.TimeExpired, new Target(transform.position));
            }
            
            // If target become invalid (i.e. character died) - retarget to its last location
            if (!_target.IsValid)
                _target = new Target(_lastValidTargetPosition);

            // Sample new target since the target can move (i.e. character)
            _lastValidTargetPosition = _target.Position;
            var currentPosition = transform.position;
            
            // If we have reached destination than no additional movement required
            if (TargetUtility.XZDistance(currentPosition, _lastValidTargetPosition) < DestinationThreshold)
                return;

            // Calculate direction and distance
            var xzDir = _lastValidTargetPosition - currentPosition;
            xzDir.y = 0;
            var xzDistance = xzDir.magnitude;
            
            // XZ direction only
            xzDir.Normalize();

            // Calculate desired position
            var nextPosition = currentPosition + _speed * Time.deltaTime * xzDir;
            _distanceTraveled += _speed * Time.deltaTime * xzDir.magnitude;

            // Update height by calculating relative progress using traveled and remaining distance
            var progress = Mathf.Clamp01(_distanceTraveled / (_distanceTraveled + xzDistance));
            
            // And sampling height from Height curve
            float baseY;
            if (_projectile.HoverGround)
            {
                baseY = TargetUtility.AboveGround(nextPosition).y;
            }
            else
            {
                baseY = Mathf.Lerp(_spawnPoint.y, _lastValidTargetPosition.y, progress);
            }
            
            nextPosition.y = baseY + _projectile.HeightProfile.Evaluate(progress);

            // If projectile have reached the destination
            var targetDir = nextPosition - _lastValidTargetPosition;
            
            // Update position and rotation 
            transform.position = nextPosition;
            transform.rotation = Quaternion.LookRotation(targetDir.normalized);    
            
            if (TargetUtility.XZDistance(nextPosition, _lastValidTargetPosition) < DestinationThreshold)
            {
                var eventTarget = _handler.Target;
                
                // If eventTarget (destination) is not valid
                // Raise event with repositioned target
                if (!eventTarget.IsValid)
                    eventTarget = _target;
                    
                HandleEvent(ProjectileEvents.ReachedDestination, eventTarget);
            }

            // Distance check
            if (_maxDistance > 0 && _distanceTraveled > _maxDistance)
                HandleEvent(ProjectileEvents.ReachedMaxDistance, new Target(transform.position));
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if(!IsActive)
                return;

            Target eventTarget;
            switch (_projectile.CollisionTarget)
            {
                case ProjectileData.EventTarget.CollisionPosition:
                    eventTarget = new Target(transform.position);
                    break;
                case ProjectileData.EventTarget.OtherColliderTransform:
                    eventTarget = new Target(other.transform);
                    break;
                case ProjectileData.EventTarget.OtherColliderPosition:
                    eventTarget = new Target(other.transform.position);
                    break;
                case ProjectileData.EventTarget.OtherColliderCharacter:
                    var otherChar = other.GetComponent<CharacterState>();
                    if(otherChar != null)
                        eventTarget = new Target(otherChar);
                    else
                        eventTarget = new Target(other.transform);
                    break;
                case ProjectileData.EventTarget.SelfPosition:
                    eventTarget = new Target(transform.position);
                    break;
                default:
                case ProjectileData.EventTarget.SelfTransform:
                    eventTarget = new Target(transform);
                    break;
            }
            
            var character = other.GetComponent<CharacterState>();
            if (character == null)
            {
                // Collision with non-character object.
                HandleEvent(ProjectileEvents.CollisionWithWorld, eventTarget);
            }
            else if (character.Equals(_handler.Target.Character))
            {
                if (TargetUtility.IsValidTeam(_handler.SpellHandler.Source.Character, character, _projectile.Affects))
                {
                    // Collision with target character object
                    HandleEvent(ProjectileEvents.CollisionWithTarget, eventTarget);
                }
            }
            else
            {
                if (TargetUtility.IsValidTeam(_handler.SpellHandler.Source.Character, character, _projectile.Affects))
                {
                    // Collision with non-target character object
                    HandleEvent(ProjectileEvents.CollisionWithOtherTargets, eventTarget);
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
                _numberOfHits += 1;
                if (_numberOfHits > _projectile.MaxPiercingTargets.GetValue(_handler.Stacks))
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
            Destroy(gameObject, _projectile.DestroyDelay);
            IsActive = false;
        }
    }
}
