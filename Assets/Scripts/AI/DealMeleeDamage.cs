using UnityEngine;
using Utils.FSM;

namespace AI
{
    class DealMeleeDamage: IStateBehaviour<AIState>
    {
        private readonly AIAgent _agent;
        private readonly AIState _nextState;
        private readonly AIState _fallbackState;

        public DealMeleeDamage(AIAgent agent, AIState nextState, AIState fallback)
        {
            _agent = agent;
            _nextState = nextState;
            _fallbackState = fallback;
        }

        public void StateStarted() {}

        public AIState? StateUpdate()
        {
            if (_agent.ActiveTarget == null || !_agent.ActiveTarget.IsAlive)
                return _fallbackState;

            if (Vector3.Distance(_agent.ActiveTarget.transform.position, _agent.transform.position) >
                _agent.Config.AI.MaxMeleeRange)
                return _fallbackState;

            _agent.LastAttackTime = Time.time;
            _agent.ActiveTarget.ReceiveMeleeDamage(_agent.CharacterState.Damage);
            if (_agent.Config.AI.MeleeAttackBuff != null)
                _agent.ActiveTarget.ApplyBuff(_agent.Config.AI.MeleeAttackBuff, _agent.CharacterState, null, 1 + _agent.CharacterState.AdditionSpellStacks);

            return _nextState;
        }

        public void StateEnded() {}
    }
}