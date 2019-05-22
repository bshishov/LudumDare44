using UnityEngine;
using Utils.FSM;

namespace AI
{
    class MoveToTargetRangeState : IStateBehaviour<AIState>
    {
        public enum MoveMode
        {
            Inside,
            Outside,
            Edge
        }

        private readonly float _targetRange;
        private readonly float _edgeTolerance;
        private readonly AIState _inRangeState;
        private readonly AIState _outOfAggroRangeState;
        private readonly AIAgent _agent;
        private readonly MoveMode _moveMode;

        public MoveToTargetRangeState(
            AIAgent agent,
            float targetRange,
            AIState inRangeState,
            AIState outOfAggroRangeState, 
            MoveMode moveMode = MoveMode.Inside,
            float edgeTolerance = 1f)
        {
            _edgeTolerance = edgeTolerance;
            _moveMode = moveMode;
            _agent = agent;
            _targetRange = targetRange;
            _inRangeState = inRangeState;
            _outOfAggroRangeState = outOfAggroRangeState;
        }

        public void StateStarted() { }

        public AIState? StateUpdate()
        {
            if (!_agent.IsAlive())
                return _outOfAggroRangeState;

            if (!_agent.HasTarget())
                return _outOfAggroRangeState;

            // Distance and direction
            var direction = _agent.ActiveTarget.transform.position - _agent.transform.position;
            var distance = (direction).magnitude;
            direction = direction.normalized;

            // We are too far from target
            if (distance > _agent.Config.AI.AggroRange)
                return _outOfAggroRangeState;

            Vector3 targetPosition;
            switch (_moveMode)
            {
                case MoveMode.Inside:
                    if (distance < _targetRange && distance < _agent.Config.AI.AggroRange)
                        return _inRangeState;
                    targetPosition = _agent.ActiveTarget.transform.position - direction * (_targetRange - 0.1f);
                    break;
                case MoveMode.Outside:
                    if (distance > _targetRange && distance < _agent.Config.AI.AggroRange)
                        return _inRangeState;
                    targetPosition = _agent.ActiveTarget.transform.position - direction * (_targetRange + 0.1f);
                    break;
                case MoveMode.Edge:
                    if(Mathf.Abs(distance - _targetRange) < _edgeTolerance && distance < _agent.Config.AI.AggroRange)
                        return _inRangeState;
                    targetPosition = _agent.ActiveTarget.transform.position - direction * _targetRange;
                    break;
                default:
                    targetPosition = _agent.ActiveTarget.transform.position - direction * _targetRange;
                    break;
            }
            
            // Do actual movement
            _agent.Movement.ControlSetDestination(targetPosition);
            _agent.Movement.ControlLookAt(targetPosition);

            // Return same state
            return null;
        }

        public void StateEnded() {}
    }
}