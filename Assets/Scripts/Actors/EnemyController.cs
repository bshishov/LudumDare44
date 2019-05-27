using System.Linq;
using AI;
using Data;
using UnityEngine;
using Utils.FSM;

namespace Actors
{
    [RequireComponent(typeof(AnimationController))]
    [RequireComponent(typeof(CharacterState))]
    [RequireComponent(typeof(MovementController))]
    public class EnemyController : MonoBehaviour
    {
        private readonly StateMachine<AIState> _stateMachine = new StateMachine<AIState>();
        private AIAgent _agent;

        private void Start()
        {
            var player = GameObject.FindGameObjectsWithTag(Common.Tags.Player)
                .Select(o => o.GetComponent<CharacterState>())
                .FirstOrDefault();

            var agent = new AIAgent(this)
            {
                ActiveTarget = player
            };
            _agent = agent;

            if (agent.Config.AI == null)
            {
                Debug.LogError($"AI Config for the EnemyController of {gameObject.name} is not set", this);
                return;
            }

            if (agent.Config.AI.Class == CharacterClass.Melee)
            {
                // Here we specify all states and how agent behave in those states
                _stateMachine.AddState(AIState.Wandering,
                    new WanderState(agent, AIState.AggroMove));

                _stateMachine.AddState(AIState.AggroMove,
                    new MoveToTargetRangeState(agent,
                        agent.Config.AI.MeleeRange,
                        AIState.StartingMeleeAttack,
                        AIState.Wandering,
                        MoveToTargetRangeState.MoveMode.Inside));

                _stateMachine.AddState(AIState.StartingMeleeAttack,
                    new StartMeleeAttack(agent,
                        AIState.WaitingMeleeAttackAnimation,
                        AIState.Wandering));

                _stateMachine.AddState(AIState.WaitingMeleeAttackAnimation,
                    new WaitingInRangeState(agent,
                        agent.Config.AI.MeleeDamageDelay,
                        agent.Config.AI.MaxMeleeRange,
                        AIState.EndingMeleeAttack,
                        AIState.Wandering,
                        1000f));

                _stateMachine.AddState(AIState.EndingMeleeAttack,
                    new DealMeleeDamage(agent,
                        AIState.StartingMeleeAttack,
                        AIState.Wandering));
            }

            if (agent.Config.AI.Class == CharacterClass.Caster)
            {
                // Wandering and doing nothing, waiting for target in range
                _stateMachine.AddState(AIState.Wandering,
                    new WanderState(agent, AIState.AggroMove));

                // Moving toward target to reach fear range and also stay inside aggro range
                _stateMachine.AddState(AIState.AggroMove,
                    new MoveToTargetRangeState(agent,
                        agent.Config.AI.FearRange,
                        AIState.SpellIntention,
                        AIState.Wandering,
                        MoveToTargetRangeState.MoveMode.Outside));

                // State that decides which spell to cast
                _stateMachine.AddState(AIState.SpellIntention,
                    new SpellCastIntention(agent,
                        AIState.MovingToSpellRange,
                        AIState.AggroMove));

                // State to adjust position according to spell range
                _stateMachine.AddState(AIState.MovingToSpellRange,
                    new AdjustPositionToCastIntendedSpell(agent,
                        AIState.PerformingCast,
                        AIState.AggroMove));

                // Finally cast a spell
                _stateMachine.AddState(AIState.PerformingCast,
                    new CastIntendedSpell(agent,
                        AIState.WaitingAfterCast,
                        AIState.Wandering));

                // Wait AFK after a spell and go back to aggro movement
                _stateMachine.AddState(AIState.WaitingAfterCast,
                    new WaitAfterIntendedSpellCast(agent,
                        AIState.AggroMove, AIState.AggroMove));
            }

            // TODO: write buffer's state machine or make one big combined state machine

            _stateMachine.StateChanged += StateChanged;
            _stateMachine.SwitchToState(AIState.Wandering);
        }

        private void StateChanged(AIState state)
        {
            _agent.Logger.Log($"Switched to state <b>{state}</b>");
        }

        void Update()
        {
            _stateMachine.Update();
        }
    }
}