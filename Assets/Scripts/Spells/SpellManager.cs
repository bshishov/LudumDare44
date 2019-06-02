using System.Collections.Generic;
using System.Text;
using Actors;
using UnityEngine;
using UnityEngine.Assertions;
using Utils;
using Utils.Debugger;

namespace Spells
{
    public class SpellManager : LazySingleton<SpellManager>
    {
        private readonly List<SpellHandler> _activeSpellStates = new List<SpellHandler>(10);

#if DEBUG
        private readonly StringBuilder _stateStringBuilder = new StringBuilder();
#endif

        private void Start()
        {
#if DEBUG
            Debugger.Default.Display($"SpellManager/ActiveSpells", _activeSpellStates.Count);
            Debugger.Default.Display($"SpellManager/State",
                new Vector2(600, 600), rect =>
                {
                    GUI.Label(rect, _stateStringBuilder.ToString());
                });
#endif
        }
        
        private void Update()
        {
#if DEBUG
            _stateStringBuilder.Clear();
#endif
            
            // Process all active spell casts
            for (var i = _activeSpellStates.Count - 1; i >= 0; i--)
            {
                // Update active
                _activeSpellStates[i].Update();

#if DEBUG
                _activeSpellStates[i].GetState(_stateStringBuilder);
                _stateStringBuilder.AppendLine();
#endif

                // Remove inactive
                if (!_activeSpellStates[i].IsActive)
                    _activeSpellStates.RemoveAt(i);
            }
        }

        public ISpellHandler Cast(Spell spell, CharacterState caster, Target target, int stacks)
        {
            if (!target.IsValid)
                return null;
            
            if (caster == null || !caster.IsAlive)
                return null;
            
            Assert.IsNotNull(spell);

            if (target.Type != spell.TargetType)
            {
                Debug.LogWarning($"Can't cast {spell.name}. Invalid target, required type: {spell.TargetType}, got {target.Type}", this);
                return null;
            }

            var source = new Target(caster);
            var minRange = spell.MinRange.GetValue(stacks);
            var maxRange = spell.MaxRange.GetValue(stacks);

#if DEBUG
            Debugger.Default.DrawCircle(source.Position, Vector3.up, minRange, Color.yellow, 1f);
            Debugger.Default.DrawCircle(source.Position, Vector3.up, maxRange, Color.yellow, 1f);
#endif

            if (spell.CheckRangeOnCast && !TargetUtility.IsInRange(source, target, minRange, maxRange))
            {
                Debug.LogWarning($"Can't cast {spell.name}. Out of range", this);
                return null;
            }

            if (target.Type == TargetType.Character &&
                !TargetUtility.IsValidTeam(caster, target.Character, spell.AffectsTeam))
            {
                Debug.LogWarning($"Can't cast {spell.name}. Invalid team", this);
                return null;
            }

            var state = new SpellHandler(spell, source, target, stacks);
            _activeSpellStates.Add(state);
            return state;
        }

      
    }
}