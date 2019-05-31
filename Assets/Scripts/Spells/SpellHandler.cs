using System;
using System.Collections.Generic;
using System.Text;
using Actors;
using UnityEngine;
using UnityEngine.Assertions;

namespace Spells
{
    /// <summary>
    /// This class responsible for managing state of entire spell cast
    /// including all sub-fired spells and projectiles.
    /// Lifetime of SpellHandler instances covers the time until every SubSpell is finished
    /// Once there are no children in spell state, instance should be disposed.
    /// </summary>
    public class SpellHandler : ISpellHandler
    {
        public Spell Spell { get; }
        public int Stacks { get; }
        public Target Source { get; }
        public Target CastTarget { get; }
        public SpellState State => _state;
        public bool IsActive => _state != SpellState.Ended;
        public event Action<ISpellHandler, SpellEvent, ISubSpellHandler> Event;

        public readonly List<CharacterState> AffectedCharacters = new List<CharacterState>();
        private readonly Dictionary<SubSpell, int> _instances = new Dictionary<SubSpell, int>();
        private SpellState _state;
        private readonly List<SubSpellHandler> _children = new List<SubSpellHandler>();
        private bool _isFiring;
        private readonly Target _originalTarget;
        private readonly TargetLocationProvider _locationTargetProxy = new TargetLocationProvider();
        private readonly float _minRange;
        private readonly float _maxRange;

        public SpellHandler(Spell spell, Target source, Target castTarget, int stacks)
        {
            Assert.IsNotNull(spell, "spell != null");
            Assert.IsTrue(source.IsValid, "source.IsValid");
            Assert.IsTrue(castTarget.IsValid, "castTarget.IsValid");
            Assert.IsTrue(castTarget.Type == spell.TargetType, "castTarget.Type == spell.TargetType");

            Spell = spell;
            Source = source;
            _originalTarget = castTarget;
            _minRange = spell.MinRange.GetValue(Stacks);
            _maxRange = spell.MaxRange.GetValue(Stacks);

            // Create target proxy
            if (spell.RangeBehaviour == Spell.TargetRangeBehaviour.ClampToRange ||
                spell.RangeBehaviour == Spell.TargetRangeBehaviour.SetMaxRange)
            {
                if (castTarget.Type == TargetType.Location || castTarget.Type == TargetType.LocationProvider)
                    CastTarget = new Target(_locationTargetProxy);
                else
                    Debug.LogWarning($"Can't create proxy target for target of type {castTarget.Type}");
            }
            else
            {
                CastTarget = castTarget;
            }

            Stacks = stacks;
            _state = SpellState.Started;
        }

        public void Update()
        {
            switch (_state)
            {
                case SpellState.Started:
                    UpdateProxyTarget();
                    FireMainSubSpells();
                    EndIfNoSubSpellsLeft();
                    return;
                case SpellState.Firing:
                    UpdateProxyTarget();
                    FiringUpdate();
                    EndIfNoSubSpellsLeft();
                    return;
                case SpellState.Finilizing:
                    UpdateProxyTarget();
                    ProcessChildren();
                    EndIfNoSubSpellsLeft();
                    return;
                default:
                case SpellState.Ended:
                    return;
            }
        }

        private void UpdateProxyTarget()
        {
            // Update proxy target
            var proxyLocation = GetTargetLocation();
            if (proxyLocation.HasValue)
            {
                _locationTargetProxy.IsValid = true;
                _locationTargetProxy.Location = proxyLocation.Value;
            }
            else
            {
                _locationTargetProxy.IsValid = false;
            }
        }

        private void FireMainSubSpells()
        {
            _state = SpellState.Firing;

            // Fire main sub spells
            foreach (var subSpell in Spell.MainSubSpells)
                CastSubSpell(subSpell, Source, CastTarget);

            Event?.Invoke(this, SpellEvent.StartedFiring, null);
        }

        private void FiringUpdate()
        {
            // If SpellTarget is out of range - switch to abort state
            if (Spell.RangeBehaviour == Spell.TargetRangeBehaviour.AbortWhenOutOfRange &&
                !SpellManager.IsInRange(Source, CastTarget, _minRange, _maxRange))
            {
                Abort();
                return;
            }

            var canStopFiring = true;
            for (var i = _children.Count - 1; i >= 0; i--)
            {
                var subProcessor = _children[i];
                subProcessor.Update();

                // Remove inactive SubSpells
                if (!subProcessor.IsActive)
                    _children.RemoveAt(i);
                else if (subProcessor.SubSpell.SpellShouldWaitUntilEnd)
                    canStopFiring = false;
            }

            if (canStopFiring)
            {
                _state = SpellState.Finilizing;
                Event?.Invoke(this, SpellEvent.FinishedFire, null);
            }
        }

        private void ProcessChildren()
        {
            for (var i = _children.Count - 1; i >= 0; i--)
            {
                var subProcessor = _children[i];
                subProcessor.Update();
                if (!subProcessor.IsActive)
                    _children.RemoveAt(i);
            }
        }

        private void EndIfNoSubSpellsLeft()
        {
            if (_children.Count == 0)
            {
                _state = SpellState.Ended;
                Event?.Invoke(this, SpellEvent.Ended, null);
            }
        }

        public void Abort()
        {
            // If current state is not "aborted" or "Finilizing"
            // We need to switch to Finilizing state
            if (_state != SpellState.Finilizing && _state != SpellState.Ended)
            {
                _state = SpellState.Finilizing;
                foreach (var subSpellHandler in _children)
                    subSpellHandler.Abort();
                Event?.Invoke(this, SpellEvent.Aborted, null);
            }
        }

        public void CastSubSpell(SubSpell subSpell, Target source, Target target)
        {
            if (!source.IsValid)
            {
                Debug.LogWarning($"Invalid source while casting SubSpell: {subSpell}");
                return;
            }
            
            if (!target.IsValid)
            {
                Debug.LogWarning($"Invalid target while casting SubSpell: {subSpell}");
                return;
            }

            Assert.IsNotNull(subSpell);

            if (_instances.ContainsKey(subSpell))
            {
                // Max instances of this SubSpell casted
                if (_instances[subSpell] >= subSpell.MaxInstances.GetValue(Stacks))
                    return;

                _instances[subSpell] += 1;
            }
            else
            {
                _instances.Add(subSpell, 1);
            }

            var handler = new SubSpellHandler(this, subSpell, source, target);
            _children.Add(handler);
            Event?.Invoke(this, SpellEvent.SubSpellCasted, handler);
        }
        
#if DEBUG
        public void GetState(StringBuilder sb)
        {
            sb.AppendLine($"- {Spell.name} {State} active:{IsActive}");
            foreach (var sub in _children)
            {
                sb.AppendLine($"- {sub.SubSpell.name} {sub.State} active:{sub.IsActive}");
            }
        }
#endif
        // Proxy targeting for special range handling
        private Vector3? GetTargetLocation()
        {
            // If target is invalid, we can't update proxy
            if (!_originalTarget.IsValid || !_originalTarget.HasPosition)
                return null;
            
            // If source is invalid, we can't update proxy
            if (!Source.IsValid || !Source.HasPosition)
                return null;
            
            var desired = _originalTarget.Position;
            var source = Source.Position;
            var dir = desired - source;
            dir.y = 0;
            var distance = dir.magnitude;
            dir.Normalize();

            if (Spell.RangeBehaviour == Spell.TargetRangeBehaviour.ClampToRange)
                desired = source + dir * Mathf.Clamp(distance, _minRange, _maxRange);

            if (Spell.RangeBehaviour == Spell.TargetRangeBehaviour.SetMaxRange)
                desired = source + dir * _maxRange;

            // Recast to floor
            if (Physics.Raycast(desired + Vector3.up * 5f, Vector3.down, out var hit, 10f, Common.LayerMasks.Ground))
                return hit.point;

            return desired;
        }
    }
}
