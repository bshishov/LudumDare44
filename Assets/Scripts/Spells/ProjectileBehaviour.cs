using System.Collections.Generic;
using Assets.Scripts.Data;
using UnityEngine;

namespace Spells
{
    public class ProjectileContext
    {
        public Vector3 origin;
        public CharacterState owner;
        public ProjectileData projectileData;

        public Spell spell;
        public int startSubContext;
        public Vector3 target;

        public CharacterState targetCharacter;

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
            if(!SpellCaster.IsEnemy(_context.owner, other.gameObject.GetComponent<CharacterState>(), _context.GetProjectileSubSpell().AffectedTarget))
                return;

            if ((_context.GetProjectileSubSpell().Obstacles & SubSpell.ObstacleHandling.Activate) ==
                SubSpell.ObstacleHandling.Activate)
                ContinueSpellSequence();

            if ((_context.GetProjectileSubSpell().Obstacles & SubSpell.ObstacleHandling.Break) ==
                SubSpell.ObstacleHandling.Break)
                DestroyParticle();
        }

        private void DestroyParticle()
        {
            Destroy(gameObject);
        }

        private void ContinueSpellSequence()
        {
            _caster.ContinueCastSpell(_context.spell, SpellEmitterData.Create(
                    _context.owner,
                    transform
                ),
                ++_context.startSubContext);

            DestroyParticle();
        }

        private void Update()
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
                if ((_context.GetProjectileSubSpell().Obstacles & SubSpell.ObstacleHandling.ContinueOnMaxDistance) ==
                    SubSpell.ObstacleHandling.ContinueOnMaxDistance)
                    ActivateProjectilePayload();
                else
                    DestroyParticle();
            }
        }

        private void ActivateProjectilePayload()
        {
        }
    }
}