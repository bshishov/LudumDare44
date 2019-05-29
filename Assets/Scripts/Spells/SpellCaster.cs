using System;
using System.Collections.Generic;
using System.Text;
using Actors;
using UnityEngine;
using UnityEngine.Assertions;
using Utils.Debugger;

namespace Spells
{
    public class SpellCaster : MonoBehaviour
    {
        private readonly List<SpellHandler> _activeSpellStates = new List<SpellHandler>();

        private void Update()
        {
#if DEBUG
            var stringState = new StringBuilder();
#endif
            // Process all active spell casts
            for (var i = _activeSpellStates.Count - 1; i >= 0; i--)
            {
                // Update active
                _activeSpellStates[i].Update();

#if DEBUG
                _activeSpellStates[i].GetState(stringState);
                stringState.AppendLine();
#endif

                // Remove inactive
                if (!_activeSpellStates[i].IsActive)
                    _activeSpellStates.RemoveAt(i);
            }

#if DEBUG
            {
                var nodePath = $"{gameObject.name}_{gameObject.GetInstanceID()}/SpellCaster";
                Debugger.Default.Display($"{nodePath}/ActiveSpells", _activeSpellStates.Count);
                Debugger.Default.Display($"{nodePath}/State",
                    new Vector2(600, 600), rect => { GUI.Label(rect, stringState.ToString()); });
            }
#endif
        }

        public ISpellHandler Cast(Spell spell, Target source, Target target, int stacks)
        {
            Assert.IsNotNull(spell);
            Assert.IsTrue(source.Character != null);
            Assert.IsTrue(source.IsValid);

            if (target.Type != spell.TargetType)
            {
                Debug.LogWarning($"Can't cast {spell.name}. Invalid target, required type: {spell.TargetType}, got {target.Type}", this);
                return null;
            }

            var minRange = spell.MinRange.GetValue(stacks);
            var maxRange = spell.MaxRange.GetValue(stacks);

#if DEBUG
            Debugger.Default.DrawCircle(source.Position, Vector3.up, minRange, Color.yellow, 1f);
            Debugger.Default.DrawCircle(source.Position, Vector3.up, maxRange, Color.yellow, 1f);
#endif

            if (spell.CheckRangeOnCast && !IsInRange(source, target, minRange, maxRange))
            {
                Debug.LogWarning($"Can't cast {spell.name}. Out of range", this);
                return null;
            }

            var state = new SpellHandler(spell, source, target, stacks);
            _activeSpellStates.Add(state);
            return state;
        }

        public static bool IsValidTeam(CharacterState origin, CharacterState other, Query.QueryTeam query)
        {
            switch (query)
            {
                case Query.QueryTeam.Self:
                    return origin.Equals(other);
                case Query.QueryTeam.EveryoneExceptSelf:
                    return !origin.Equals(other);
                case Query.QueryTeam.Everyone:
                    return true;
                case Query.QueryTeam.Ally:
                    if (other.CurrentTeam.HasFlag(CharacterState.Team.AgainstTheWorld))
                        return false;
                    return origin.CurrentTeam == other.CurrentTeam;
                case Query.QueryTeam.Enemy:
                    if (other.CurrentTeam.HasFlag(CharacterState.Team.AgainstTheWorld))
                        return true;
                    return origin.CurrentTeam != other.CurrentTeam;
                default:
                    throw new InvalidOperationException($"Invalid query team type: {query}");
            }
        }

        public static bool IsInRange(Target source, Target target, float minRange, float maxRange)
        {
            if (target.Type == TargetType.None)
                return true;

            var dir = target.Position - source.Position;
            dir.y = 0;
            var range = dir.magnitude;
            return range >= minRange && range <= maxRange;
        }
    }
}
