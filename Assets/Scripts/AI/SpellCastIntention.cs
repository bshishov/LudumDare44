using System.Linq;
using Spells;
using Utils;
using Utils.FSM;

namespace AI
{
    /// <summary>
    /// State behaviour that chooses the spell to cast in the next state
    /// </summary>
    class SpellCastIntention : IStateBehaviour<AIState>
    {
        private readonly AIAgent _agent;
        private readonly AIState _nextState;
        private readonly AIState _fallbackState;

        public SpellCastIntention(AIAgent agent, AIState next, AIState fallback)
        {
            _agent = agent;
            _nextState = next;
            _fallbackState = fallback;
        }

        public void StateStarted() {}

        public AIState? StateUpdate()
        {
            if (!_agent.IsAlive())
                return _fallbackState;

            if (!_agent.HasTarget())
                return _fallbackState;

            if (!_agent.IsBetweenFearAndAggro())
                return _fallbackState;

            if (!_agent.CanCast())
                return _fallbackState;

            // If no intentions to choose from -> fallback
            if(_agent.Config.AI.SlotConfig == null || _agent.Config.AI.SlotConfig.Length == 0)
                return _fallbackState;
            
            // Select a slot to cast that is ready;
            var readySlots = _agent.Config.AI.SlotConfig.Where(s =>
            {
                var slotState = _agent.SpellBook.GetSpellSlotState(s.Slot);
                return slotState.State == SpellbookState.SlotState.Ready;
            }).ToList();

            // Find and randomly choose an active slot
            if (readySlots.Count >= 0)
            {
                var intention = RandomUtils.Choice(readySlots, s => s.Weight);
                _agent.IntendedSpell = intention;
                return _nextState;
            }

            // No slots to cast
            _agent.Logger.Log("No active slots to cast");
            return _fallbackState;
        }

        public void StateEnded() { }
    }
}
