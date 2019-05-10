using System.Linq;
using AI;
using Assets.Scripts.Data;
using UnityEngine;
using Utils.FSM;

[RequireComponent(typeof(AnimationController))]
[RequireComponent(typeof(CharacterState))]
[RequireComponent(typeof(MovementController))]
public class EnemyController : MonoBehaviour
{
    private readonly StateMachine<AIState> _stateMachine = new StateMachine<AIState>();
    private AIAgent _agent;

    private void Start()
    {
        var players = GameObject.FindGameObjectsWithTag(Common.Tags.Player).
            Select(o => o.GetComponent<CharacterState>())
            .ToArray();

        var agent = new AIAgent(this)
        {
            ActiveTarget = players[0]
        };
        _agent = agent;

        if (agent.Config.Class == CharacterClass.Melee)
        {
            // Here we specify all states and how agent behave in those states
            _stateMachine.AddState(AIState.Wandering,
                new WanderState(agent, AIState.MovingToRange));

            _stateMachine.AddState(AIState.MovingToRange,
                new MoveToTargetRangeState(agent,
                    agent.Config.MeleeRange,
                    AIState.StartingMeleeAttack,
                    AIState.Wandering));

            _stateMachine.AddState(AIState.StartingMeleeAttack,
                new StartMeleeAttack(agent,
                    AIState.WaitingMeleeAttackAnimation,
                    AIState.Wandering));

            _stateMachine.AddState(AIState.WaitingMeleeAttackAnimation,
                new WaitingInRangeState(agent,
                    agent.Config.AnimationDelay,
                    agent.Config.MeleeRange,
                    AIState.EndingMeleeAttack,
                    AIState.Wandering,
                    1000f));

            _stateMachine.AddState(AIState.EndingMeleeAttack,
                new DealMeleeDamage(agent,
                    AIState.StartingMeleeAttack,
                    AIState.Wandering));
        }

        if (agent.Config.Class == CharacterClass.Caster)
        {
            _stateMachine.AddState(AIState.Wandering,
                new WanderState(agent, AIState.MovingToRange));

            _stateMachine.AddState(AIState.MovingToRange,
                new MoveToTargetRangeState(agent,
                    agent.Config.FearRange,
                    AIState.PerformingCast,
                    AIState.Wandering));

            _stateMachine.AddState(AIState.PerformingCast,
                new CastRandomSpell(agent,
                    AIState.WaitingAfterCast,
                    AIState.Wandering));

            _stateMachine.AddState(AIState.WaitingAfterCast,
                new WaitingInRangeState(agent,
                    agent.Config.AnimationDelay,
                    agent.Config.FearRange,
                    AIState.PerformingCast,
                    AIState.Wandering,
                    2f));
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