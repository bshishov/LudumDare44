using UnityEngine;
using Utils.FSM;

namespace AI
{
    class WaitAfterIntendedSpellCast : IStateBehaviour<AIState>
    {
        private readonly AIAgent _agent;
        private readonly AIState _nextState;
        private readonly AIState _fallbackState;

        private float _waitingTime;

        public WaitAfterIntendedSpellCast(AIAgent agent, AIState next, AIState fallback)
        {
            _agent = agent;
            _nextState = next;
            _fallbackState = fallback;
        }

        public void StateStarted()
        {
            _waitingTime = 0f;
        }

        public AIState? StateUpdate()
        {
            if (_agent.IntendedSpell == null)
                return _fallbackState;

            _waitingTime += Time.deltaTime;
            if (_waitingTime > _agent.IntendedSpell.WaitAfterCast)
            {
                _waitingTime = 0;
                return _nextState;
            }

            return null;
        }

        public void StateEnded() {}
    }
}