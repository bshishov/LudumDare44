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
        public SubSpellState State => _state;
        public int Stacks => _spellHandler.Stacks;
        public bool IsActive => _state != SubSpellState.Ended;

        private SubSpellState _state;
        private float _timer;
        private float _fireStartedTime;
        private Projectile _projectile;

        public SubSpellHandler(
            SpellHandler spellHandler, 
            SubSpell subSpell, 
            Target source, 
            Target target)
        {
            Assert.IsTrue(source.IsValid);
            Assert.IsTrue(target.IsValid);
            Assert.IsNotNull(subSpell);
            Assert.IsNotNull(spellHandler);

            SubSpell = subSpell;
            _spellHandler = spellHandler;
            Source = source;
            Target = target;
            _state = SubSpellState.Started;
        }

        public void Update()
        {
            // If targets are invalid because of something - try to abort
            if (!Source.IsValid || !Target.IsValid)
                Abort();

            // State machine
            switch (_state)
            {
                case SubSpellState.Ended:
                    return;
                case SubSpellState.Started:
                    FireEvent(SubSpellEvent.Started);
                    _timer = SubSpell.PreCastDelay.GetValue(Stacks);
                    _state = SubSpellState.PreCastDelay;
                    return;
                case SubSpellState.PreCastDelay:
                    _timer -= Time.deltaTime;
                    if (_timer < 0)
                    {
                        if (SubSpell.IsProjectileSubSpell)
                        {
                            _state = SubSpellState.ServicingProjectile;
                            SpawnProjectile();
                        }
                        else
                        {
                            _state = SubSpellState.FireDelay;
                            _timer = 0;
                            _fireStartedTime = Time.time;
                        }
                        FireEvent(SubSpellEvent.AfterPreCastDelay);
                    }
                    break;
                case SubSpellState.FireDelay:
                    _timer -= Time.deltaTime;
                    if (_timer < 0)
                        _state = SubSpellState.Firing;
                    break;
                case SubSpellState.Firing:
                    FireEvent(SubSpellEvent.OnFire);
                    var timeSinceFirstFire = Time.time - _fireStartedTime;
                    if (timeSinceFirstFire > SubSpell.FireDuration.GetValue(Stacks))
                    {
                        // Finished firing
                        _state = SubSpellState.PostCastDelay;
                        _timer = SubSpell.PostCastDelay.GetValue(Stacks);
                        FireEvent(SubSpellEvent.AfterFinishedFire);
                    }
                    else
                    {
                        // Back to delayed state. Still firing
                        _state = SubSpellState.FireDelay;
                        _timer = SubSpell.FireDelay.GetValue(Stacks);
                    }
                    break;
                case SubSpellState.ServicingProjectile:
                    // If projectile was destroyed or inactive
                    if (_projectile == null || !_projectile.IsActive)
                    {
                        _state = SubSpellState.PostCastDelay;
                        _timer = SubSpell.PostCastDelay.GetValue(Stacks);
                    }
                    // Otherwise do nothing. Wait.
                    break;
                case SubSpellState.PostCastDelay:
                    _timer -= Time.deltaTime;
                    if (_timer < 0)
                    {
                        _state = SubSpellState.Ended;
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
            SubSpell.EffectHandler?.OnEvent(new SubSpellEventArgs(this, eventType));

            foreach (var e in SubSpell.FireSubSpellEvents.Where(e => e.Type.Equals(eventType)))
            {
                var targets = QueryTargets(e.Query);
                if (targets == null)
                    targets = new List<Target>();
                if(additionalTargets != null)
                    targets.AddRange(additionalTargets);
                
                
                // Get only valid targets
                targets = targets.Where(t => t.IsValid).ToList();

                // Targeted event
                SubSpell.EffectHandler?.OnEvent(new SubSpellEventArgs(this, eventType, e.Query, targets));
                
                // New source of SubSpell
                var newSsSource = ResolveTarget(e.SubSpellSource, ResolveTarget(e.Query.Origin, Source));
                
                foreach (var target in targets)
                {
                    #if DEBUG
                    if (target.HasPosition)
                    {
                        var tPos = target.Position;
                        Debugger.Default.DrawCircle(tPos, Vector3.up, 0.5f, Color.yellow, 1f);
                        Debugger.Default.DrawRay(new Ray(tPos, target.Forward), Color.yellow, 0.5f, 1f);
                    }
                    #endif
                    if (target.Type == TargetType.Character)
                    {
                        if (e.TrackAffectedCharacters)
                            _spellHandler.AffectedCharacters.Add(target.Character);
                        
                        if(e.ApplyBuffToTarget != null)
                            target.Character.ApplyBuff(
                                e.ApplyBuffToTarget, 
                                SpellHandler.Source.Character, 
                                Stacks, 
                                this);
                    }

                    // SubSpell firing
                    if (e.FireSubSpell != null)
                    {
                        // Before adding SubSpell we first need to figure out what
                        // new sources and targets will be. 
                        // Because some SubSpells overrides targeting
                        var newTarget = ResolveTarget(e.SubSpellTarget, target);

                        // Fire child sub spell
                        // Queue it to the spell processor
                        _spellHandler.CastSubSpell(e.FireSubSpell, newSsSource, newTarget);
                    }
                }
            }
        }

        public void Abort()
        {
            // If this handler cannot be aborted
            if (!SubSpell.CanBeAborted)
                return;

            // If already inactive - do nothing
            if (_state == SubSpellState.Ended)
                return;

            if(_projectile != null)
                Object.Destroy(_projectile.gameObject);

            _state = SubSpellState.Ended;
            FireEvent(SubSpellEvent.Ended, null);

            if(SubSpell.AbortSpellIfAborted)
                SpellHandler.Abort();
        }

        public List<Target> QueryTargets(Query query)
        {
            if (query.NewTargetsQueryType == Query.QueryType.None)
                return null;
            
            var origin = ResolveTarget(query.Origin, Source);

            if (query.NewTargetsQueryType == Query.QueryType.AsOrigin)
                return new List<Target>() { origin };
            
            if(query.NewTargetsQueryType == Query.QueryType.RandomLocationInAoe)
            {
                var randomLoc = RandomLocationInAoe(query.Area, origin);
                return new List<Target> { new Target(randomLoc) };
            }

            var targetChars = CharactersInAoe(query.Area, origin);
            if (targetChars == null)
                return null;

            // Filter by team
            // Check team for character targets
            targetChars = targetChars.Where(c =>
                SpellManager.IsValidTeam(SpellHandler.Source.Character, c, query.AffectsTeam));

            // Filter affected
            // If we should exclude already affected characters - exclude them
            if (query.ExcludeAlreadyAffected && _spellHandler.AffectedCharacters != null)
                targetChars = targetChars.Except(_spellHandler.AffectedCharacters);

            if (!targetChars.Any())
                return null;

            if (query.NewTargetsQueryType == Query.QueryType.RandomTargetInAoe)
                targetChars = new[] {RandomUtils.Choice(targetChars.ToList())};

            if (query.NewTargetsQueryType == Query.QueryType.ClosestToOriginInAoe)
            {
                var originPos = origin.Position;
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

        public Target ResolveTarget(TargetResolution targetResolution, Target defaultTarget)
        {
            switch (targetResolution)
            {
                case TargetResolution.OriginalSpellSource:
                    return SpellHandler.Source;
                case TargetResolution.OriginalSpellTarget:
                    return SpellHandler.CastTarget;
                case TargetResolution.CurrentSource:
                    return Source;
                case TargetResolution.CurrentTarget:
                    return Target;
                case TargetResolution.SourceLocation:
                    return Source.ToLocationTarget();
                case TargetResolution.TargetLocation:
                    return Target.ToLocationTarget();
                case TargetResolution.OriginalSpellSourceLocation:
                    return SpellHandler.Source.ToLocationTarget();
                case TargetResolution.OriginalSpellTargetLocation:
                    return SpellHandler.CastTarget.ToLocationTarget();
                case TargetResolution.None:
                    return Target.None;
                case TargetResolution.Default:
                default:
                    return defaultTarget;
            }
        }
    }
}