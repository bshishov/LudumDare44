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
    public SpellEmitter[] _emitters;

    void Start()
    {
        _ground = new Plane(Vector3.up, Vector3.zero);

        _characterState = GetComponent<CharacterState>();
        _animator = GetComponent<AnimationController>();

        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;

        _emitters = GetComponentsInChildren<SpellEmitter>();

        CharacterUtils.ApplySettings(_characterState, _agent, false);
    }

    void GetInput()
    {
        moveDirection.x = Input.GetAxis("Horizontal");
        moveDirection.z = Input.GetAxis("Vertical");

        if (Input.GetMouseButtonDown(0))
            FireSpell(0);

        if (Input.GetMouseButtonDown(1))
            FireSpell(1);

        if (Input.GetMouseButtonDown(1))
            FireSpell(2);
    }

    private void FireSpell(int index)
    {
        Vector3 groundPoint = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (_ground.Raycast(ray, out var enter))
        {
            groundPoint = ray.GetPoint(enter);
        }

        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, 100, LayerMask.GetMask("Actors")))
        {
        }

        Debug.DrawRay(ray.origin, ray.direction, Color.green);

        var data = _emitters[0].GetData(_characterState, ray, groundPoint, hitInfo);
        _characterState.FireSpell(index, data);
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 1, 0, 0.75F);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Gizmos.DrawRay(ray);

        if (_ground.Raycast(ray, out var enter))
        {
            var hitPoint = ray.GetPoint(enter);
            Gizmos.DrawSphere(hitPoint, 0.2f);
        }

        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, 100, LayerMask.GetMask("Actors")))
        {
            Gizmos.DrawWireCube(hitInfo.transform.position, Vector3.one);
        }
    }
}
