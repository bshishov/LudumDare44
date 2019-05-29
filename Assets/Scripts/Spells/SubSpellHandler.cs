using System;
using System.Collections.Generic;
using System.Linq;
using Actors;
using Data;
using UnityEngine;
using UnityEngine.Assertions;
using Utils;
using Utils.Debugger;
using Object = UnityEngine.Object;

namespace Spells
{
    public class SubSpellHandler : ISubSpellHandler
    {
        public ISpellHandler SpellHandler => _spellHandler;
        private readonly SpellHandler _spellHandler;
        public Spell Spell => _spellHandler.Spell;
        public SubSpell SubSpell { get; }
        public Target Source { get; }
        public Target Target { get; }
        public SpellState State => _state;
        public int Stacks => _spellHandler.Stacks;
        public bool IsActive => _state != SpellState.Ended;

        private SpellState _state;
        private float _timer;
        private float _fireStartedTime;
        private Projectile _projectile;

        public SubSpellHandler(
            SpellHandler spellHandler, 
            SubSpell subSpell, 
            Target source, 
            Target target)
        {
            Assert.IsNotNull(subSpell);
            Assert.IsNotNull(spellHandler);

            SubSpell = subSpell;
            _spellHandler = spellHandler;
            Source = source;
            Target = target;
            _state = SpellState.Started;
        }

        public void Update()
        {
            // If targets are invalid because of something - try to abort
            if (!Source.IsValid || !Target.IsValid)
                Abort();

            // State machine
            switch (_state)
            {
                case SpellState.Ended:
                    return;
                case SpellState.Started:
                    FireEvent(SubSpellEvent.Started);
                    _timer = SubSpell.PreCastDelay.GetValue(Stacks);
                    _state = SpellState.PreCastDelay;
                    return;
                case SpellState.PreCastDelay:
                    _timer -= Time.deltaTime;
                    if (_timer < 0)
                    {
                        if (SubSpell.IsProjectileSubSpell)
                        {
                            _state = SpellState.ServicingProjectile;
                            SpawnProjectile();
                        }
                        else
                        {
                            _state = SpellState.FireDelay;
                            _timer = SubSpell.FireDelay.GetValue(Stacks);
                            _fireStartedTime = Time.time;
                        }
                        FireEvent(SubSpellEvent.AfterPreCastDelay);
                    }
                    break;
                case SpellState.FireDelay:
                    _timer -= Time.deltaTime;
                    if (_timer < 0)
                        _state = SpellState.Firing;
                    break;
                case SpellState.Firing:
                    FireEvent(SubSpellEvent.OnFire);
                    var timeSinceFirstFire = Time.time - _fireStartedTime;
                    if (timeSinceFirstFire > SubSpell.FireDuration.GetValue(Stacks))
                    {
                        // Finished firing
                        _state = SpellState.PostCastDelay;
                        _timer = SubSpell.PostCastDelay.GetValue(Stacks);
                        FireEvent(SubSpellEvent.AfterFinishedFire);
                    }
                    else
                    {
                        // Back to delayed state. Still firing
                        _state = SpellState.FireDelay;
                        _timer = SubSpell.FireDelay.GetValue(Stacks);
                    }
                    break;
                case SpellState.ServicingProjectile:
                    // If projectile was destroyed or inactive
                    if (_projectile == null || !_projectile.IsActive)
                    {
                        _state = SpellState.PostCastDelay;
                        _timer = SubSpell.PostCastDelay.GetValue(Stacks);
                    }
                    // Otherwise do nothing. Wait.
                    break;
                case SpellState.PostCastDelay:
                    _timer -= Time.deltaTime;
                    if (_timer < 0)
                    {
                        _state = SpellState.Ended;
                        FireEvent(SubSpellEvent.Ended);
                    }
                    break;
            }
        }

        public void HandleProjectileFireEvent(Target target)
        {
            FireEvent(SubSpellEvent.ProjectileHit, new []{ target });
        }

        public void HandleProjectileDestroyEvent(Target target)
        {
            FireEvent(SubSpellEvent.ProjectileDestroy, new[] { target });
        }

        private void FireEvent(SubSpellEvent eventType, IList<Target> additionalTargets = null)
        {
            // Send non-targeted event to effect
            SubSpell.GetEffect()?.OnEvent(new SubSpellEventArgs(this, eventType));

            foreach (var e in SubSpell.FireSubSpellEvents.Where(e => e.Type.Equals(eventType)))
            {
                var targets = QueryTargets(e.Query);
                if (targets == null)
                    targets = new List<Target>();
                if(additionalTargets != null)
                    targets.AddRange(additionalTargets);

                // Targeted event
                SubSpell.GetEffect()?.OnEvent(new SubSpellEventArgs(this, eventType, e.Query, targets));
                
                foreach (var target in targets)
                {
                    Debugger.Default.DrawCircleSphere(target.Position, 0.5f, Color.green, 1f);
                    if (target.Type == TargetType.Character)
                    {
                        if (SubSpell.AffectsCharactersOnlyOncePerSpell)
                            _spellHandler.AddAffectedCharacter(SubSpell, target.Character);
                        
                        if(e.ApplyBuffToTarget != null)
                            target.Character.ApplyBuff(
                                e.ApplyBuffToTarget, 
                                SpellHandler.Source.Character, 
                                Stacks, 
                                this);
                    }

                    if (e.FireSubSpell != null)
                        CastChildSubSpell(e.FireSubSpell, Target, target);
                }
            }
        }

        public void Abort()
        {
            // If this handler cannot be aborted
            if (!SubSpell.CanBeAborted)
                return;

            // If already inactive - do nothing
            if (_state == SpellState.Ended)
                return;

            if(_projectile != null)
                Object.Destroy(_projectile.gameObject);

            _state = SpellState.Ended;
            FireEvent(SubSpellEvent.Ended, null);

            if(SubSpell.AbortSpellIfAborted)
                SpellHandler.Abort();
        }

        public List<Target> QueryTargets(Query query)
        {
            var origin = ResolveOrigin(query.Origin);
            var originPos = origin.Position;

            // Todo: Handle special target cases like "current source". Do we need to check team on them?
            switch (query.NewTargetsQueryType)
            {
                case Query.QueryType.None:
                    return null;
                case Query.QueryType.CurrentSource:
                    return new List<Target> { Source };
                    //targetChars = new[] { Source.Character };
                    //break;
                case Query.QueryType.CurrentTarget:
                    return new List<Target> { Target };
                    //targetChars = new[] { Target.Character };
                    //break;
                case Query.QueryType.OriginalSpellSource:
                    return new List<Target> { SpellHandler.Source };
                    //targetChars = new[] { SpellHandler.Source.Character };
                    //break;
                case Query.QueryType.OriginalSpellTarget:
                    return new List<Target> { SpellHandler.CastTarget };
                //targetChars = new[] { SpellHandler.CastTarget.Character };
                //break;
                case Query.QueryType.RandomLocationInAoe:
                    var randomLoc = RandomLocationInAoe(query.Area, origin);
                    return new List<Target> { new Target(randomLoc) };

                // Special cases handled below
                default:
                    break;
            }

            var targetChars = CharactersInAoe(query.Area, origin);
            if (targetChars == null)
                return null;

            // Filter by team
            // Check team for character targets
            targetChars = targetChars.Where(c =>
                SpellCaster.IsValidTeam(SpellHandler.Source.Character, c, query.AffectsTeam));

            // Filter affected
            // If we should exclude already affected characters - exclude them
            if (SubSpell.AffectsCharactersOnlyOncePerSpell)
            {
                var affected = _spellHandler.GetAffectedCharacters(SubSpell);
                if(affected != null)
                    targetChars = targetChars.Except(affected);
            }

            if (!targetChars.Any())
                return null;

            if (query.NewTargetsQueryType == Query.QueryType.RandomTargetInAoe)
                targetChars = new[] {RandomUtils.Choice(targetChars.ToList())};

            if (query.NewTargetsQueryType == Query.QueryType.ClosestToOriginInAoe)
            {
                var closest = targetChars.OrderBy(c => Vector3.Distance(originPos, c.transform.position))
                    .FirstOrDefault();
                if (closest != null)
                    targetChars = new[] { closest };
                else
                    targetChars = null;
            }

            // Convert characters to targets
            return targetChars?.Select(c => new Target(c)).ToList();
        }

        private void SpawnProjectile()
        {
            if (SubSpell.Projectile != null)
            {
                var projectileRoot = new GameObject("Projectile", typeof(Projectile));
                projectileRoot.transform.position = Source.Position;
                projectileRoot.transform.rotation = Quaternion.LookRotation(Source.Forward);

                var go = Object.Instantiate(SubSpell.Projectile.Prefab, projectileRoot.transform);
                _projectile = projectileRoot.GetComponent<Projectile>();
                _projectile.Initialize(this, SubSpell.Projectile);
            }
        }

        private IEnumerable<CharacterState> CharactersInAoe(AreaOfEffect area, Target origin)
        {
            if (area == null)
            {
                Debug.LogWarning("Area is null");
                return null;
            }
            
            switch (area.Area)
            {
                case AreaOfEffect.AreaType.Cone:
                    return AoeUtility.CharactersInsideCone(origin.Position, 
                        origin.Forward, 
                        area.Angle.GetValue(SpellHandler.Stacks), 
                        area.Size.GetValue(SpellHandler.Stacks),
                        area.MinSize.GetValue(SpellHandler.Stacks));
                case AreaOfEffect.AreaType.Sphere:
                    return AoeUtility.CharactersInsideSphere(origin.Position, 
                        area.Size.GetValue(SpellHandler.Stacks), 
                        area.MinSize.GetValue(SpellHandler.Stacks));
                case AreaOfEffect.AreaType.Line:
                    return AoeUtility.CharactersLine(origin.Position, Target.Position);
                default:
                    throw new InvalidOperationException($"Invalid area type: {area.Area}");
            }
        }

        private Vector3 RandomLocationInAoe(AreaOfEffect area, Target origin)
        {
            if (area == null)
            {
                Debug.LogWarning("Area is null");
                return origin.Position;
            }

            switch (area.Area)
            {
                case AreaOfEffect.AreaType.Cone:
                    return AoeUtility.RandomInCone(
                        origin.Position, 
                        origin.Forward, 
                        area.Angle.GetValue(SpellHandler.Stacks), 
                        area.Size.GetValue(SpellHandler.Stacks),
                        area.MinSize.GetValue(SpellHandler.Stacks));
                case AreaOfEffect.AreaType.Sphere:
                    return AoeUtility.RandomInSphere(origin.Position,
                        area.Size.GetValue(SpellHandler.Stacks),
                        area.MinSize.GetValue(SpellHandler.Stacks));
                case AreaOfEffect.AreaType.Line:
                    return AoeUtility.RandomInLine(origin.Position, Target.Position);
                default:
                    throw new InvalidOperationException($"Invalid area type: {area.Area}");
            }
        }

        public Target ResolveOrigin(Query.QueryOrigin originType)
        {
            switch (originType)
            {
                case Query.QueryOrigin.OriginalSpellSource:
                    return SpellHandler.Source;
                case Query.QueryOrigin.OriginalSpellTarget:
                    return SpellHandler.CastTarget;
                case Query.QueryOrigin.CurrentSource:
                    return Source;
                case Query.QueryOrigin.CurrentTarget:
                    return Target;
                default:
                    throw new InvalidOperationException("Unknown origin type");
            }
        }
        
        private void CastChildSubSpell(SubSpell subSpell, Target defaultSource, Target defaultTarget)
        {
            // Before adding SubSpell we first need to figure out what
            // new sources and targets will be. 
            // Because some SubSpells overrides targeting
            var source = ResolveTarget(subSpell.Source, defaultSource);
            var target = ResolveTarget(subSpell.Target, defaultTarget);

            // Fire child sub spell
            // Queue it to the spell processor
            _spellHandler.CastSubSpell(subSpell, source, target);
        }

        private Target ResolveTarget(SubSpell.Targeting targeting, Target defaultTarget)
        {
            switch (targeting)
            {
                case SubSpell.Targeting.OriginalSpellSource:
                    return SpellHandler.Source;
                case SubSpell.Targeting.OriginalSpellTarget:
                    return SpellHandler.CastTarget;
                case SubSpell.Targeting.PreviousSource:
                    return Source;
                case SubSpell.Targeting.PreviousTarget:
                    return Target;
                case SubSpell.Targeting.None:
                    return Target.None;
                default:
                case SubSpell.Targeting.Default:
                    return defaultTarget;
            }
        }
    }
}