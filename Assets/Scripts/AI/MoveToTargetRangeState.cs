using UnityEngine;
using Utils.FSM;

namespace AI
{
    class MoveToTargetRangeState : IStateBehaviour<AIState>
    {
        private readonly float _targetRange;
        private readonly float _tolerance;
        private readonly AIState _inRangeState;
        private readonly AIState _outOfRangeState;
        private readonly AIAgent _agent;

        public MoveToTargetRangeState(AIAgent agent,
            float targetRange,
            AIState inRangeState,
            AIState outOfRangeState, 
            float tolerance = 1f)
        {
            _tolerance = tolerance;
            _agent = agent;
            _targetRange = targetRange;
            _inRangeState = inRangeState;
            _outOfRangeState = outOfRangeState;
        }

        public void StateStarted() { }

        public AIState? StateUpdate()
        {
            if (!_agent.CharacterState.IsAlive)
                return null;

            if (_agent.ActiveTarget == null || !_agent.ActiveTarget.IsAlive)
                return _outOfRangeState;

            var direction = _agent.ActiveTarget.transform.position - _agent.transform.position;
            var distance = (direction).magnitude;

            if (distance > _agent.Config.IndifferenceDistance)
                return _outOfRangeState;

            direction = direction.normalized;

            Vector3 targetPosition;
            if (distance < _targetRange)
                targetPosition = _agent.ActiveTarget.transform.position - direction * _targetRange;
            else
                targetPosition = _agent.ActiveTarget.transform.position;

            _agent.Movement.SetDestination(targetPosition);
            _agent.Movement.LookAt(targetPosition);

            // If we've reached our target range - change state to in-range state
            if (Vector3.Distance(targetPosition, _agent.transform.position) < _tolerance)
                return _inRangeState;

            // Return same state
            return null;
        }

        public void StateEnded() {}
    }
}