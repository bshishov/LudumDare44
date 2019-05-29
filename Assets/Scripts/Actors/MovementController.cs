using Actors;
using Data;
using Spells;
using UnityEngine;
using UnityEngine.AI;
using Utils.FSM;


[RequireComponent(typeof(CharacterState))]
[RequireComponent(typeof(NavMeshAgent))]
public class MovementController : MonoBehaviour
{
    public enum MovementState
    {
        Control,
        ForceMovementNavigational,
        ForceMovement,
        Stunned,
        Dead
    }

    [Range(0, 1)]
    public float RotationSlerpFactor = 0.9f;

    private NavMeshAgent _navMeshAgent;
    private CharacterState _characterState;
    private Quaternion _targetRotation;

    // Force 
    private Target _forceTargetInfo;
    private float _forceSpeed;
    private float _forceDuration;
    private bool _forceBreakOnDestination;
    private float _forceMaxDistance;
    private Vector3 _forceSource;
    private StateMachine<MovementState> _stateMachine;

    void Start()
    {
        _characterState = GetComponent<CharacterState>();
        _characterState.Died += CharacterStateOnDeath;

        _navMeshAgent = GetComponent<NavMeshAgent>();
        _navMeshAgent.speed = _characterState.Speed + Random.value;
        _navMeshAgent.updateRotation = false;

        // State machine setup
        _stateMachine = new StateMachine<MovementState>();
        _stateMachine.AddState(MovementState.ForceMovementNavigational, 
            new LambdaStateBehaviour<MovementState>(ForceNavUpdate)
            {
                Started = () => { _navMeshAgent.isStopped = false; },
                Ended = () => { _navMeshAgent.ResetPath(); }
            });
        _stateMachine.AddState(MovementState.ForceMovement,
            new LambdaStateBehaviour<MovementState>(ForceUpdate)
            {
                Started = () => { _navMeshAgent.isStopped = true; }
            });
        _stateMachine.AddState(MovementState.Control,
            new LambdaStateBehaviour<MovementState>(ControlNavUpdate)
            {
                Started = () => { _navMeshAgent.isStopped = false; }
            });
        _stateMachine.AddState(MovementState.Stunned,
            new LambdaStateBehaviour<MovementState>(StunnedUpdate)
            {
                Started = () => { _navMeshAgent.enabled = false; },
                Ended = () => { _navMeshAgent.enabled = true; }
            });
        _stateMachine.AddState(MovementState.Dead,
            new LambdaStateBehaviour<MovementState>()
            {
                Started = () =>
                {
                    _navMeshAgent.isStopped = true;
                    _navMeshAgent.enabled = false;
                }
            });
        _stateMachine.SwitchToState(MovementState.Control);
    }

    MovementState? ForceNavUpdate()
    {
        _forceDuration -= Time.deltaTime;

        var targetPos = _forceTargetInfo.Position;
        _navMeshAgent.SetDestination(targetPos);
        _navMeshAgent.speed = _forceSpeed;
        LookAt(targetPos);

        var dir = targetPos - transform.position;
        if (_forceBreakOnDestination && dir.magnitude < 1f)
            return MovementState.Control;

        if (_forceDuration < 0 || Vector3.Distance(transform.position, _forceSource) > _forceMaxDistance)
            return MovementState.Control;

        return null;
    }

    MovementState? StunnedUpdate()
    {
        if(_characterState.IsStunned)
            return null;

        return MovementState.Control;
    }

    MovementState? ControlNavUpdate()
    {
        if (_characterState.IsStunned)
            return MovementState.Stunned;

        _navMeshAgent.speed = _characterState.Speed;
        return null;
    }

    MovementState? ForceUpdate()
    {
        _forceDuration -= Time.deltaTime;

        var targetPos = _forceTargetInfo.Position;

        var dir = targetPos - transform.position;
        if (_forceBreakOnDestination && dir.magnitude < 1f)
            return MovementState.Control;

        LookAt(targetPos);
        _navMeshAgent.Move(Vector3.ClampMagnitude(dir.normalized * _forceSpeed * Time.deltaTime, _forceMaxDistance));

        if (_forceDuration < 0 || Vector3.Distance(transform.position, _forceSource) > _forceMaxDistance)
            return MovementState.Control;

        return null;
    }

    void Update()
    {
        _stateMachine.Update();
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, RotationSlerpFactor);
    }

    private void CharacterStateOnDeath()
    {
        _stateMachine.SwitchToState(MovementState.Dead);
    }

    private void LookAt(Vector3 target)
    {
        var q = Quaternion.LookRotation(target - transform.position);
        q.eulerAngles = new Vector3(0, q.eulerAngles.y, 0);
        _targetRotation = q;
    }

    public void ControlLookAt(Vector3 target)
    {
        if (_stateMachine.CurrentStateKey != MovementState.Control)
            return;

        LookAt(target);
    }

    public void ForceMove(
        Affect.MoveInfo.MovementType type,
        Target target, 
        float speed, 
        float duration, 
        bool breakOnDestination,
        float maxDistance)
    {

        if (type == Affect.MoveInfo.MovementType.Warp)
        {
            _navMeshAgent.Warp(target.Position);
            return;
        }
        
        _forceSpeed = speed;
        _forceDuration = duration;
        _forceBreakOnDestination = breakOnDestination;
        _forceTargetInfo = target;
        _forceSource = transform.position;
        _forceMaxDistance = maxDistance;

        if (type == Affect.MoveInfo.MovementType.Navigational)
            _stateMachine.SwitchToState(MovementState.ForceMovementNavigational);

        if (type == Affect.MoveInfo.MovementType.IgnoreNavigation)
            _stateMachine.SwitchToState(MovementState.ForceMovement);
    }

    public void ControlMove(Vector3 motionVector)
    {
        if (_stateMachine.CurrentStateKey == MovementState.Control)
            _navMeshAgent.Move(motionVector);
    }

    public void ControlSetDestination(Vector3 position)
    {
        if (_stateMachine.CurrentStateKey == MovementState.Control)
        {
            // Sets destination for auto path
            _navMeshAgent.isStopped = false;
            _navMeshAgent.SetDestination(position);
        }
    }

    public void ControlStop()
    {
        if (_stateMachine.CurrentStateKey != MovementState.Control)
            return;

        // Used to stop auto path
        _navMeshAgent.isStopped = true;
    }
}
