using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    public float maxSpeed = 7;
    private Vector3 moveDirection = new Vector3();

    private Plane _ground;
    private CharacterParams _characterParams;
    private NavMeshAgent _agent;


    void Start()
    {
        _ground = new Plane(Vector3.up, Vector3.zero);

        _characterParams = GetComponent<CharacterParams>();
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;

        CharacterUtils.ApplaySettings(_characterParams, _agent, false);
    }

    void GetInput()
    {
        moveDirection.x = Input.GetAxis("Horizontal");
        moveDirection.z = Input.GetAxis("Vertical");
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
}
