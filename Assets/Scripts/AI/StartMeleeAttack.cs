using UnityEngine;
using Utils.FSM;

namespace AI
{
    class StartMeleeAttack: IStateBehaviour<AIState>
    {
        private readonly AIAgent _agent;
        private readonly AIState _nextState;
        private readonly AIState _fallbackState;

        public StartMeleeAttack(AIAgent agent, AIState nextState, AIState fallbackState)
        {
            _agent = agent;
            _nextState = nextState;
            _fallbackState = fallbackState;
        }

        public void StateStarted() {}

        public AIState? StateUpdate()
        {
            if (_agent.ActiveTarget == null || !_agent.ActiveTarget.IsAlive)
                return _fallbackState;

            if (Time.time > _agent.Config.MeleeCooldown + _agent.LastAttackTime)
            {
                _agent.Movement.Stop();
                _agent.Movement.LookAt(_agent.ActiveTarget.transform.position);
                _agent.AnimationController.PlayAttackAnimation();
                return _nextState;
            }

            return _fallbackState;
        }

        public void StateEnded() {}
    }
}