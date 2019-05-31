using System;
using System.Collections.Generic;
using Utils;

namespace Spells.DataOriented.States
{
    public class SpellManager : Singleton<SpellManager>
    {
        private readonly List<SpellInfo> _spells = new List<SpellInfo>(10);
        private readonly List<SubSpellInfo> _subSpells = new List<SubSpellInfo>(100);

        private ISpellStateBehaviour _state1;

        private void Start()
        {
            _state1 = new SpellCreatedState(this);
        }
        
        private void Update()
        {
            // Update spells
            for (var i = _spells.Count - 1; i >= 0; i--)
            {
                var entity = _spells[i];
                if (entity.State == SubSpellState.Ended)
                {
                    _spells.RemoveAt(i);
                    break;
                }
                var previousState = entity.State;
                var handler = GetSpellBehaviour(entity.State);
                
                // Update using desired state
                handler.StateUpdate(entity);
                
                // If after update entity is in the same state - do nothing
                if (previousState == entity.State)
                    continue;
                
                // If state has changed - call end in the current handler
                handler.StateEnded(entity);
                
                // And Also call the start of the new handler
                GetSpellBehaviour(entity.State).StateStarted(entity);
            }
        }

        private ISpellStateBehaviour GetSpellBehaviour(SubSpellState state)
        {
            return _state1;
        }
        
        private ISubSpellStateBehaviour GetSubSpellBehaviour(SubSpellState state)
        {
            throw new NotImplementedException();
        }

        public void Cast(Spell spell, Target source, Target target, int stacks)
        {
            _spells.Add(new SpellInfo
            {
                State = SubSpellState.Started,
                Spell = spell,
                CastSource = source,
                OriginalCastTarget = target,
                Stacks = stacks
            });
        }

        public void Cast(SpellInfo spell, SubSpell subSpell, Target source, Target target)
        {
            _subSpells.Add(new SubSpellInfo
            {
                State = SubSpellState.Started,
                SpellInfo = spell,
                SubSpell = subSpell,
                Source = source,
                Target = target
            });
        }
    }
}