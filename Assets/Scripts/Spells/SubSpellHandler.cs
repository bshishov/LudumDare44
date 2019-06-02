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
                    FireEvent(SubSpellEvent.Started, Source);
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
                        FireEvent(SubSpellEvent.AfterPreCastDelay, Source);
                    }
                    break;
                case SubSpellState.FireDelay:
                    _timer -= Time.deltaTime;
                    if (_timer < 0)
                        _state = SubSpellState.Firing;
                    break;
                case SubSpellState.Firing:
                    FireEvent(SubSpellEvent.OnFire, Source);
                    var timeSinceFirstFire = Time.time - _fireStartedTime;
                    if (timeSinceFirstFire > SubSpell.FireDuration.GetValue(Stacks))
                    {
                        // Finished firing
                        _state = SubSpellState.PostCastDelay;
                        _timer = SubSpell.PostCastDelay.GetValue(Stacks);
                        FireEvent(SubSpellEvent.AfterFinishedFire, Source);
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
                        FireEvent(SubSpellEvent.Ended, Source);
                    }
                    break;
            }
        }

        public void HandleProjectileFireEvent(Target hitTarget)
        {
            FireEvent(SubSpellEvent.ProjectileHit, hitTarget);
        }

        public void HandleProjectileDestroyEvent(Target hitTarget)
        {
            FireEvent(SubSpellEvent.ProjectileDestroy, hitTarget);
        }

        // Note this is a shared static buffer of targets
        private static readonly List<Target> Queried = new List<Target>(1);
        private static readonly List<CharacterState> QueriedCharactersBuffer = new List<CharacterState>(1);
        
        private void FireEvent(SubSpellEvent eventType, Target defaultOrigin)
        {
            // Send non-targeted event to effect
            SubSpell.EffectHandler?.OnEvent(new SubSpellEventArgs(this, eventType));

            for (var eventIndex = 0; eventIndex < SubSpell.FireSubSpellEvents.Length; eventIndex++)
            {
                // Filter SubSpell events
                var e = SubSpell.FireSubSpellEvents[eventIndex];
                if(e.Type != eventType)
                    continue;
                
                QueryTargets(Queried, e.Query, defaultOrigin);

                // Targeted event
                SubSpell.EffectHandler?.OnEvent(new SubSpellEventArgs(this, eventType, e.Query, Queried));
                
                // New source of SubSpell
                var newSsSource = ResolveTarget(e.SubSpellSource, ResolveTarget(e.Query.Origin, defaultOrigin));
                
                foreach (var target in Queried)
                {
                    #if DEBUG
                    TargetUtility.DebugDraw(target, Color.yellow);
                    #endif
                    if (target.Type == TargetType.Character)
                    {
                        if (e.Query.ExcludeAlreadyAffected &&
                            _spellHandler.AffectedCharacters.Contains(target.Character))
                            continue;
                        
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
            FireEvent(SubSpellEvent.Ended, Source);

            if(SubSpell.AbortSpellIfAborted)
                SpellHandler.Abort();
        }

        private void QueryTargets(List<Target> queried, Query query, Target defaultOrigin)
        {
            queried.Clear();
            var origin = ResolveTarget(query.Origin, defaultOrigin);
            switch (query.NewTargetsQueryType)
            {
                case Query.QueryType.None:
                    return;
                case Query.QueryType.OriginAsTarget:
                    if(origin.IsValid)
                        queried.Add(origin);
                    return;
                case Query.QueryType.RandomLocationInAoe:
                    var randomLoc = RandomLocationInAoe(query.Area, origin);
                    queried.Add(new Target(randomLoc, origin.Forward));
                    return;
                default:
                    break;
            }
            
            // Find all characters inside AoE and put them inside buffer
            CharactersInAoe(QueriedCharactersBuffer, query.Area, origin);
            
            // Filter characters
            for (var i = QueriedCharactersBuffer.Count - 1; i >= 0; i--)
            {
                var c = QueriedCharactersBuffer[i];
                
                // Team filtering
                if (!TargetUtility.IsValidTeam(SpellHandler.Source.Character, c, query.AffectsTeam))
                {
                    QueriedCharactersBuffer.RemoveAt(i);
                    continue;
                }
                
                // Filter affected
                if (query.ExcludeAlreadyAffected &&
                    _spellHandler.AffectedCharacters != null &&
                    _spellHandler.AffectedCharacters.Contains(c))
                {
                    QueriedCharactersBuffer.RemoveAt(i);
                }
            }
            
            // If we are empty after filtering
            if (QueriedCharactersBuffer.Count == 0)
                return;

            if (query.NewTargetsQueryType == Query.QueryType.RandomTargetInAoe)
            {
                queried.Add(new Target(RandomUtils.Choice(QueriedCharactersBuffer)));
                return;
            }
            
            if (query.NewTargetsQueryType == Query.QueryType.AllTargetsInAoe)
            {
                for (var i = 0; i < QueriedCharactersBuffer.Count; i++)
                    queried.Add(new Target(QueriedCharactersBuffer[i]));
                return;
            }

            if (query.NewTargetsQueryType == Query.QueryType.ClosestToOriginInAoe)
            {
                var minDistance = 1e8f;
                var minIndex = 0;
                var originPos = origin.Position;
                for (var i = 1; i < QueriedCharactersBuffer.Count; i++)
                {
                    var c = QueriedCharactersBuffer[i];
                    var distance = Vector3.Distance(originPos, c.transform.position); 
                    if (distance < minDistance)
                    {
                        minIndex = i;
                        minDistance = distance;
                    }
                }
                
                queried.Add(new Target(QueriedCharactersBuffer[minIndex]));
                return;
            }
        }

        private void SpawnProjectile()
        {
            if (SubSpell.Projectile != null)
            {
                var go = Object.Instantiate(SubSpell.Projectile.Prefab, 
                    Source.Position, 
                    Quaternion.LookRotation(Source.Forward));
                _projectile = go.AddComponent<Projectile>();
                _projectile.Initialize(this, SubSpell.Projectile);
            }
        }

        private void CharactersInAoe(
            List<CharacterState> characters, 
            AreaOfEffect area, 
            Target origin)
        {
            // Clear buffer
            characters.Clear();
            
            if (area == null)
            {
                Debug.LogWarning("Area is not set");
                return;
            }
            
            switch (area.Area)
            {
                case AreaOfEffect.AreaType.Cone:
                    AoeUtility.CharactersInsideConeNonAlloc(
                        characters,
                        origin.Position, 
                        origin.Forward, 
                        area.Angle.GetValue(SpellHandler.Stacks), 
                        area.Size.GetValue(SpellHandler.Stacks),
                        area.MinSize.GetValue(SpellHandler.Stacks));
                    return;
                case AreaOfEffect.AreaType.Sphere:
                    AoeUtility.CharactersInsideSphereNonAlloc(
                        characters,
                        origin.Position, 
                        area.Size.GetValue(SpellHandler.Stacks), 
                        area.MinSize.GetValue(SpellHandler.Stacks));
                    return;
                case AreaOfEffect.AreaType.Line:
                    AoeUtility.CharactersInLineNonAlloc(characters, origin.Position, Target.Position);
                    return;
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
                    return AoeUtility.RandomInsideSphere(origin.Position,
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