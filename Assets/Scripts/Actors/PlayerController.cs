using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    public float maxSpeed = 7;
    private Vector3 moveDirection = new Vector3();

    private Plane _ground;
    private CharacterState _characterState;

    private NavMeshAgent _agent;
    private AnimationController _animator;


    void Start()
    {
        _ground = new Plane(Vector3.up, Vector3.zero);

        _characterState = GetComponent<CharacterState>();
        _animator = GetComponent<AnimationController>();

        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;

        CharacterUtils.ApplySettings(_characterState, _agent, false);
    }

    void GetInput()
    {
        moveDirection.x = Input.GetAxis("Horizontal");
        moveDirection.z = Input.GetAxis("Vertical");

        var spells = _characterState.SpellbookState;
        if(spells == null)
        {
            Debug.LogError("No spell state!");
            return;
        }

        if (Input.GetMouseButtonDown(0))
            spells.FireSpell(0);

        if (Input.GetMouseButtonDown(1))
            spells.FireSpell(1);

        if (Input.GetMouseButtonDown(1))
            spells.FireSpell(2);
    }

    void Update()
    {
        GetInput();
        Move();
        LookAt();
    }

    void Move()
    {
        var motionVector = moveDirection.normalized * maxSpeed * Time.deltaTime;

        _agent.Move(motionVector);
        _agent.SetDestination(transform.position + motionVector);

        _animator.SetSpeed(_agent.velocity.magnitude);
    }

    private void LookAt()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (_ground.Raycast(ray, out var enter))
        {
            var hitPoint = ray.GetPoint(enter);
            transform.LookAt(hitPoint);

            var q = transform.rotation;
            q.eulerAngles = new Vector3(0, q.eulerAngles.y, q.eulerAngles.z);
            transform.rotation = q;
        }
    }
}
