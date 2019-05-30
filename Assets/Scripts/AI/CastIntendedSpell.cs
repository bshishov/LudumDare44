using Spells;
using UnityEngine;
using Utils.FSM;

namespace AI
{
    /// <summary>
    /// This state performs spell casting. Including channeling
    /// It uses "spell intention" from memory. It should be set from another state
    /// </summary>
    class CastIntendedSpell : IStateBehaviour<AIState>
    {
        private readonly AIAgent _agent;
        private readonly AIState _nextState;
        private readonly AIState _fallbackState;
        private bool _isCasting;
        private readonly TargetLocationProvider _targetProxy;

        public CastIntendedSpell(AIAgent agent, AIState next, AIState fallback)
        {
            _agent = agent;
            _nextState = next;
            _fallbackState = fallback;
            _targetProxy = new TargetLocationProvider
            {
                IsValid = false
            };
        }

        public void StateStarted()
        {
            _isCasting = false;
            _targetProxy.IsValid = false;
        }

        public AIState? StateUpdate()
        {
            if (!_agent.IsAlive())
            {
                AbortIfCasting();
                return _fallbackState;
            }

            if (!_agent.HasTarget())
            {
                AbortIfCasting();
                return _fallbackState;
            }

            if (!_agent.IsBetweenFearAndAggro())
            {
                AbortIfCasting();
                return _fallbackState;
            }

            if (_agent.IntendedSpell == null)
            {
                AbortIfCasting();
                return _fallbackState;
            }

            var slotState = _agent.SpellBook.GetSpellSlotState(_agent.IntendedSpell.Slot);
            if (slotState.State == SpellbookState.SpellState.Ready)
            {
                if (_isCasting)
                {
                    // If we are casting and ended up in ready state
                    // then it means that the cast is completed
                    _agent.LastSpellCastTime = Time.time;
                    return _nextState;
                }
                
                // If AI is not casting and the slot is ready
                // Cast the spell
                _agent.Movement.ControlLookAt(_agent.ActiveTarget.transform.position);
                _agent.Movement.ControlStop();
                
                if (_agent.SpellBook.TryFireSpellToTarget(_agent.IntendedSpell.Slot, 
                    GetTarget(slotState.Spell.TargetType)))
                {
                    _isCasting = true;
                    return null;
                }

                // Casting failed, fallback
                return _fallbackState;
            }
            else if(slotState.State == SpellbookState.SpellState.Firing || 
                    slotState.State == SpellbookState.SpellState.Preparing)
            {
                // Update target while casting
                _targetProxy.Location = _agent.ActiveTarget.transform.position;
                
                // We are casting, we cool
                return null;
            }

            // Failed to cast
            return _fallbackState;
        }

        public void StateEnded()
        {
            _targetProxy.IsValid = false;
        }

        private void AbortIfCasting()
        {
            _targetProxy.IsValid = false;
            
            if (_agent.IntendedSpell == null)
                return;
            
            var slotState = _agent.SpellBook.GetSpellSlotState(_agent.IntendedSpell.Slot);
            slotState.SpellHandler?.Abort();
        }

        private Target GetTarget(TargetType type)
        {
            switch (type)
            {
                case TargetType.None:
                    return Target.None;
                case TargetType.Location:
                    var t = _agent.ActiveTarget.transform;
                    return new Target(t.position, t.forward);
                case TargetType.Transform:
                    return new Target(_agent.ActiveTarget.transform);
                case TargetType.LocationProvider:
                    _targetProxy.IsValid = true;
                    _targetProxy.Location = _agent.ActiveTarget.transform.position;
                    return new Target(_targetProxy);
                case TargetType.Character:
                    return new Target(_agent.ActiveTarget);
                default:
                    return Target.None;
            }
        }
    }
}