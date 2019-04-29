using System.Collections.Generic;
using Assets.Scripts.Data;
using UnityEngine;

namespace Spells
{
    public class ProjectileContext
    {
        public CharacterState owner;
        public ProjectileData projectileData;

        public Spell spell;
        public int startSubContext;

        public TargetInfo origin;
        public TargetInfo target;
        
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
        private float _trevaledDistance;


        public void Initialize(ProjectileContext context, SpellCaster caster)
        {
            if (context == null)
            {
                DestroyParticle();
                return;
            }

            _caster = caster;
            _context = context;
            transform.LookAt(_context.target.Position.Value);

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
            if (_destroying)
                return;

            var character = other.gameObject.GetComponent<CharacterState>();
            if (character == null)
            {
                Debug.LogWarning($"Invalid colision target {other.gameObject.name}");
                return;
            }

            if (!SpellCaster.IsEnemy(_context.owner, character, _context.GetProjectileSubSpell().AffectedTarget))
                return;

            if ((_context.GetProjectileSubSpell().Obstacles & SubSpell.ObstacleHandling.ExecuteSpellSequence) ==
                SubSpell.ObstacleHandling.ExecuteSpellSequence)
                ContinueSpellSequence(character);

            if ((_context.GetProjectileSubSpell().Obstacles & SubSpell.ObstacleHandling.Break) ==
                SubSpell.ObstacleHandling.Break)
                DestroyParticle();
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
                    TargetInfo.Create(_context.owner, transform),
                    TargetInfo.Create(target)
                ), _context.startSubContext + 1);
        }

        private void Update()
        {
            if (_destroying)
                return;

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
                if ((_context.GetProjectileSubSpell().Obstacles &
                     SubSpell.ObstacleHandling.ExecuteSpellSequenceOnMaxDistance) ==
                    SubSpell.ObstacleHandling.ExecuteSpellSequenceOnMaxDistance)
                    ContinueSpellSequence(null);
                else
                    DestroyParticle();
            }
        }

        private void ActivateProjectilePayload()
        {
            if (_destroying)
                return;
        }
    }
}