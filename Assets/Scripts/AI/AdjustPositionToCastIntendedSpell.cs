using Utils.FSM;

namespace AI
{
    class AdjustPositionToCastIntendedSpell : IStateBehaviour<AIState>
    {
        private readonly AIAgent _agent;
        private readonly AIState _nextState;
        private readonly AIState _fallbackState;

        private float _desiredRange;

        public AdjustPositionToCastIntendedSpell(AIAgent agent, AIState next, AIState fallback)
        {
            _agent = agent;
            _nextState = next;
            _fallbackState = fallback;
        }

        public void StateStarted()
        {
            var slotState = _agent.SpellBook.GetSpellSlotState(_agent.IntendedSpell.Slot);
            _desiredRange = slotState.Spell.CastRange;
        }

        public AIState? StateUpdate()
        {
            if (!_agent.IsAlive())
                return _fallbackState;

            if (!_agent.HasTarget())
                return _fallbackState;

            if (!_agent.IsBetweenFearAndAggro())
                return _fallbackState;

            var distance = _agent.GetLineDistanceToTarget();
            if (distance < _desiredRange)
                return _nextState;

            var direction = (_agent.ActiveTarget.transform.position - _agent.transform.position).normalized;

            var moveTo = _agent.ActiveTarget.transform.position - direction * (_desiredRange - 0.1f);
            _agent.Movement.SetDestination(moveTo);
            _agent.Movement.LookAt(moveTo);

            return null;
        }

        public void StateEnded() { }
    }
}