using Actors;
using Spells;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(CharacterState))]
[RequireComponent(typeof(NavMeshAgent))]
public class MovementController : MonoBehaviour
{
    [Range(0, 1)]
    public float RotationSlerpFactor = 0.9f;

    private NavMeshAgent _navMeshAgent;
    private CharacterState _characterState;
    private bool _locked = false;
    private Quaternion _targetRotation;

    // Force 
    private bool _isForceMoving;
    private TargetInfo _forceTargetInfo;
    private float _forceSpeed;
    private float _forceDuration;
    private bool _forceBreakOnDestination;

    void Start()
    {
        _characterState = GetComponent<CharacterState>();
        _characterState.Died += CharacterStateOnDeath;

        _navMeshAgent = GetComponent<NavMeshAgent>();
        _navMeshAgent.speed = _characterState.Speed + Random.value;
        _navMeshAgent.updateRotation = false;
    }

    void Update()
    {
        if (_isForceMoving)
        {
            _forceDuration -= Time.deltaTime;
            
            var targetPos = _forceTargetInfo.Position.Value;
            var dir = targetPos - transform.position;
            if (_forceBreakOnDestination && dir.magnitude < 1f)
            {
                _isForceMoving = false;
                _navMeshAgent.isStopped = false;
            }
            else
            {
                LookAt(targetPos);
                if(!_navMeshAgent.isStopped)
                    _navMeshAgent.Move(dir.normalized * _forceSpeed * Time.deltaTime);
            }

            if (_forceDuration < 0)
            {
                _isForceMoving = false;
                _navMeshAgent.isStopped = false;
            }
        }
        else
        {
            // TODO: Implement and use on buff applied
            _navMeshAgent.speed = _characterState.Speed;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, RotationSlerpFactor);
    }

    private void CharacterStateOnDeath()
    {
        // If character dies - stop everything
        //_navMeshAgent.isStopped = true;
        _navMeshAgent.enabled = false;
    }

    public void Move(Vector3 motionVector)
    {
        if (_isForceMoving)
            return;

        // MOve directly
        _navMeshAgent.Move(motionVector);
    }

    public void SetDestination(Vector3 position)
    {
        if(_isForceMoving)
            return;

        // Sets destination for auto path
        _navMeshAgent.isStopped = false;
        _navMeshAgent.SetDestination(position);
    }

    public void LookAt(Vector3 target)
    {
        var q = Quaternion.LookRotation(target - transform.position);
        q.eulerAngles = new Vector3(0, q.eulerAngles.y, 0);
        _targetRotation = q;
    }

    public void ForceMove(TargetInfo target, float speed, float duration, bool breakOnDestination)
    {
        _navMeshAgent.isStopped = true;
        _isForceMoving = true;
        _forceSpeed = speed;
        _forceDuration = duration;
        _forceBreakOnDestination = breakOnDestination;
        _forceTargetInfo = target;
    }

    public void Stop()
    {
        // Used to stop auto path
        _navMeshAgent.isStopped = true;
    }

    public void Lock()
    {
        if (!_locked)
        {
            _locked = false;
            _navMeshAgent.isStopped = true;
        }
    }

    public void Unlock()
    {
        if (_locked)
        {
            _locked = true;
            _navMeshAgent.isStopped = false;
        }
    }
}
