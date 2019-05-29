using UnityEngine;
using Utils.FSM;

namespace AI
{
    class WaitingInRangeState : IStateBehaviour<AIState>
    {
        private readonly float _waitTime;
        private readonly float _keepRange;
        private readonly AIState _targetOutOfRange;
        private readonly AIState _finished;
        private readonly AIAgent _agent;
        private readonly float _tolerance;

        private float _timePassed;
        

        public WaitingInRangeState(AIAgent agent,
            float time,
            float keepRange,
            AIState finishedWaiting,
            AIState targetOutOfRange,
            float tolerance = 1f) 
        {
            _agent = agent;
            _finished = finishedWaiting;
            _targetOutOfRange = targetOutOfRange;
            _keepRange = keepRange;
            _waitTime = time;
            _tolerance = tolerance;
        }

        public void StateStarted()
        {
            _timePassed = 0;
        }

        public AIState? StateUpdate()
        {
            _timePassed += Time.deltaTime;
            if (_timePassed > _waitTime)
                return _finished;

            if (_agent.ActiveTarget != null && _agent.ActiveTarget.IsAlive)
            {
                _agent.Movement.ControlLookAt(_agent.ActiveTarget.transform.position);

                var distance = (_agent.ActiveTarget.transform.position - _agent.Transform.position).magnitude;
                if (Mathf.Abs(distance - _keepRange) > _tolerance)
                    return _targetOutOfRange;
            }

            return null;
        }

        public void StateEnded() {}
    }
}