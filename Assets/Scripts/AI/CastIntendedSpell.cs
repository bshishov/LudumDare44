using Spells;
using UnityEngine;
using Utils.FSM;

namespace AI
{
    /// <summary>
    /// This state performs spell casting. Including channeling
    /// It uses "spell intention" from memory. It should be set from another state
    /// </summary>
    class CastIntendedSpell : IStateBehaviour<AIState>, IChannelingInfo
    {
        private readonly AIAgent _agent;
        private readonly AIState _nextState;
        private readonly AIState _fallbackState;

        public CastIntendedSpell(AIAgent agent, AIState next, AIState fallback)
        {
            _agent = agent;
            _nextState = next;
            _fallbackState = fallback;
        }

        public void StateStarted() { }

        public AIState? StateUpdate()
        {
            if (!_agent.IsAlive())
                return _fallbackState;

            if (!_agent.HasTarget())
                return _fallbackState;

            if (!_agent.IsBetweenFearAndAggro())
                return _fallbackState;

            if (_agent.IntendedSpell == null)
                return _fallbackState;

            // TODO: Figure out channeling
            if (Time.time > _agent.Config.AI.SpellCastingCooldown + _agent.LastSpellCastTime)
            {
                _agent.Movement.LookAt(_agent.ActiveTarget.transform.position);
                _agent.Movement.Stop();
                
                if (_agent.SpellBook.TryFireSpellToTarget(
                    _agent.IntendedSpell.Slot, 
                    _agent.ActiveTarget, 
                    this))
                {
                    _agent.LastSpellCastTime = Time.time;
                    return _nextState;
                }
            }

            // Failed to cast
            return _fallbackState;
        }

        public void StateEnded() { }

        public TargetInfo GetNewTarget()
        {
            return TargetInfo.Create(_agent.ActiveTarget);
        }
    }
}