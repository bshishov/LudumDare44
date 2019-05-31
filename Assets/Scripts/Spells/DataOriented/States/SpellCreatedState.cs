namespace Spells.DataOriented.States
{
    public class SpellCreatedState : ISpellStateBehaviour
    {
        private readonly SpellManager _manager;
        
        public SpellCreatedState(SpellManager manager)
        {
            _manager = manager;
        }
        
        public void StateStarted(SpellInfo entity) { }

        public void StateUpdate(SpellInfo entity)
        {
            if (!entity.TargetsAreValid())
            {
                // Targets are not valid. Exit
                entity.State = SubSpellState.Ended;
                return;
            }
            
            // TODO: Check range
            
            if (entity.Spell.MainSubSpells.Length == 0)
            {
                // No main SubSpells. End
                entity.State = SubSpellState.Ended;    
                return;
            }

            for (var i = 0; i < entity.Spell.MainSubSpells.Length; i++)
            {
                var subSpell = entity.Spell.MainSubSpells[i];
                if (subSpell.SpellShouldWaitUntilEnd)
                    entity.WaitingSubspells += 1;
                _manager.Cast(entity, subSpell, entity.CastSource, entity.CastTarget);
            }
            
            // Todo FIRE STARTED EVENT
            
            entity.State = SubSpellState.Firing;
        }

        public void StateEnded(SpellInfo entity) { }
    }

    public class SpellFiringState : ISpellStateBehaviour
    {
        private readonly SpellManager _manager;
        
        public SpellFiringState(SpellManager manager)
        {
            _manager = manager;
        }
        
        public void StateStarted(SpellInfo entity)
        {
        }

        public void StateUpdate(SpellInfo entity)
        {
            // TODO: Update targets
            
            if (!entity.TargetsAreValid())
            {
                // Targets are not valid. Exit
                entity.State = SubSpellState.Ended;
                return;
            }

            if (entity.WaitingSubspells <= 0)
            {
                entity.State = SubSpellState.Ended;
                return;
            }
        }

        public void StateEnded(SpellInfo entity)
        {
        }
    }
}