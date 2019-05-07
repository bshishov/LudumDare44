using System.Collections.Generic;
using Assets.Scripts.Data;
using UnityEngine;
using UnityEngine.Assertions;

namespace Spells
{
    public class ProjectileContext
    {
        public CharacterState owner;
        public ProjectileData projectileData;

        public Spell spell;
        public int Stacks;
        public int startSubContext;

        public TargetInfo origin;
        public TargetInfo target;
        public ProjectileTrajectory trajectory;

        public SubSpell GetProjectileSubSpell()
        {
            return spell.SubSpells[startSubContext];
        }
    }

    public class ProjectileBehaviour : MonoBehaviour
    {
        private List<SpellContext> _activationContexts = new List<SpellContext>();
        private SpellCaster _caster;
        private ProjectileContext _context;
        private bool _destroying;
        private float _distanceTraveled;

        private Vector3 _direction;

        private static SubSpell.ObstacleHandling _requireCollisionMask =
            SubSpell.ObstacleHandling.ExecuteSpellSequence | SubSpell.ObstacleHandling.Break | SubSpell.ObstacleHandling.IgnoreButTarget;

        private Rigidbody _rigidbody;
        private Collider _collider;

        public void Initialize(ProjectileContext context, SpellCaster caster)
        {
            if (context == null)
            {
                DestroyParticle();
                return;
            }

            _caster = caster;
            _context = context;
            _context.trajectory = _context.projectileData.Trajectory;

            var position = transform.position;
            _direction = _context.target.Position.Value - position;

            position += Quaternion.LookRotation(_direction) * _context.projectileData.Offset;
            transform.position = position;

            _direction = _context.target.Position.Value - position;
            _direction = _direction.normalized;
            transform.LookAt(_context.target.Position.Value);

            _collider = gameObject.GetComponentInChildren<Collider>();
            Assert.IsNotNull(_collider);
            Assert.IsTrue(_collider.isTrigger);

            if ((_requireCollisionMask & _context.GetProjectileSubSpell().Obstacles) != 0)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = false;
            }

            switch (_context.trajectory)
            {
                case ProjectileTrajectory.Line:
                    break;
                case ProjectileTrajectory.Follow:
                    break;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_destroying)
                return;

            var projectileSubSpell = _context.GetProjectileSubSpell();
            var character = other.gameObject.GetComponent<CharacterState>();
            if (character == null)
            {
                if (!projectileSubSpell.Obstacles.HasFlag(SubSpell.ObstacleHandling.IgnoreWorldCollision))
                    DestroyParticle();
            }
            else
            {
                var ignoreEnemyCheck = projectileSubSpell.Obstacles.HasFlag(SubSpell.ObstacleHandling.IgnoreButTarget) &&
                                       character != _context.target.Character;

                if (ignoreEnemyCheck || !SpellCaster.IsEnemy(_context.owner, character, projectileSubSpell.AffectedTarget))
                    return;

                if (projectileSubSpell.Obstacles.HasFlag(SubSpell.ObstacleHandling.ExecuteSpellSequence))
                    ContinueSpellSequence(character);

                if (projectileSubSpell.Obstacles.HasFlag(SubSpell.ObstacleHandling.Break))
                    DestroyParticle();
            }
        }

        private void DestroyParticle()
        {
            _destroying = true;
            Destroy(gameObject);
        }

        private void ContinueSpellSequence(CharacterState target)
        {
            if (_destroying)
                return;

            _caster.ContinueCastSpell(_context.spell,
                new SpellTargets(
                    TargetInfo.Create(_context.owner, transform, transform.position),
                    target != null ? TargetInfo.Create(target) : new TargetInfo {Position = transform.position}
                ), _context.startSubContext + 1, stacks:_context.Stacks);
        }

        private void Update()
        {
            if (_destroying)
                return;

            var moveDistance = _context.projectileData.Speed * Time.deltaTime;

            switch (_context.trajectory)
            {
                case ProjectileTrajectory.Line:
                    _distanceTraveled += moveDistance;
                    transform.position += _direction * moveDistance;
                    break;

                case ProjectileTrajectory.Follow:
                    _distanceTraveled += moveDistance;
                    transform.position += (_context.target.Transform.position - transform.position).normalized * moveDistance;
                    break;

                case ProjectileTrajectory.Falling:
                    if (transform.position.y < 1.0f)
                    {
                        _direction.y = 0;
                        _direction = _direction.normalized;
                        _context.trajectory = ProjectileTrajectory.Line;
                    }
                    transform.position += _direction * _context.projectileData.FallingSpeed * Time.deltaTime;
                    break;
            }

            if (_context.projectileData.MaxDistance > 0 && _distanceTraveled > _context.projectileData.MaxDistance)
            {
                if (_context.GetProjectileSubSpell().Obstacles.HasFlag(SubSpell.ObstacleHandling.ExecuteSpellSequenceOnMaxDistance))
                    ContinueSpellSequence(null);

                DestroyParticle();
            }
        }
    }
}