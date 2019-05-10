using System;
using UnityEngine;
using Utils.FSM;
using Random = UnityEngine.Random;

namespace AI
{
    class WanderState : IStateBehaviour<AIState>
    {
        private readonly AIAgent _agent;
        private readonly AIState _aggroState;

        public WanderState(AIAgent agent, AIState aggroState)
        {
            _agent = agent;
            _aggroState = aggroState;
        }

        public void StateStarted()
        {
        }

        public AIState? StateUpdate()
        {
            if (_agent.ActiveTarget == null || !_agent.ActiveTarget.IsAlive)
            {
                _agent.ActiveTarget = null;
                // TODO: add target searching
                return null;
            }

            var distance = (_agent.ActiveTarget.transform.position - _agent.transform.position).magnitude;
            if (distance < _agent.Config.IndifferenceDistance)
                return _aggroState;

            // TODO: Add random movement maybe ?
            // Do nothing, we are wandering and not in aggro
            return null;
        }

        public void StateEnded() { }
    }

    class CastRandomSpell : IStateBehaviour<AIState>
    {
        private readonly AIAgent _agent;
        private readonly AIState _nextState;
        private readonly AIState _fallbackState;

        public CastRandomSpell(AIAgent agent, AIState next, AIState fallback)
        {
            _agent = agent;
            _nextState = next;
            _fallbackState = fallback;
        }

        public void StateStarted() { }

        public AIState? StateUpdate()
        {
            if (_agent.ActiveTarget == null || !_agent.ActiveTarget.IsAlive)
                return _fallbackState;

            if (Time.time > _agent.Config.SpellCooldown + _agent.LastSpellCastTime)
            {
                _agent.Movement.LookAt(_agent.ActiveTarget.transform.position);
                _agent.Movement.Stop();

                if (_agent.SpellBook.TryFireSpellToTarget(
                    Mathf.FloorToInt(Random.value * _agent.Config.UseSpells.Count),
                    _agent.ActiveTarget, null))
                {
                    _agent.LastSpellCastTime = Time.time;
                }
            }

            return _nextState;
        }

        public void StateEnded() { }
    }

    class BufferTryBuff : IStateBehaviour<AIState>
    {
        private AIAgent _agent;

        public BufferTryBuff(AIAgent agent)
        {
            _agent = agent;
        }

        public void StateStarted() { }

        public AIState? StateUpdate()
        {
            /*
             *
             * yield return null;
        while (_characterState.IsAlive && _player.IsAlive)
        {
            var len = _player.transform.position - transform.position;
            var distance = len.magnitude;
            _bufferCheck += Time.deltaTime;

            if ((_buffTarget == null || !_buffTarget.IsAlive) && _bufferCheck>1f)
            {
                _logger.Log("Found target");
                _bufferCheck = 0;
                var allies = GameObject.FindGameObjectsWithTag(Common.Tags.Enemy).Select(o => o.GetComponent<CharacterState>()).ToArray();
                if (allies.Length > 0)
                    _buffTarget = RandomUtils.Choice(allies);                
            }

            if (_buffTarget == null || distance < _fearRange || !_buffTarget.IsAlive)
            {
                StartCoroutine(FearOrSpellState(BufferWanderState(), len, distance));
                break;
            }
            else
            {
                _movement.Stop();
                var buffTarget = _buffTarget.GetComponent<CharacterState>();

                buffTarget.ApplyBuff(_useBuff, buffTarget, null, 1);
                _animationController.PlayCastAnimation();
            }
            yield return null;
        }
             */

            return null;
        }

        public void StateEnded() { }
    }
}