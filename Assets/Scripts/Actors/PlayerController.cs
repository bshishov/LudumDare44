using UnityEngine;
using UnityEditor;
using System;
using Assets.Scripts.Data;
using Spells;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    public float maxSpeed = 7;
    private Vector3 moveDirection = new Vector3();

    private Plane _ground;
    private CharacterState _characterState;
    private SpellbookState _spellbook;

    private NavMeshAgent _agent;
    private AnimationController _animator;

    void Start()
    {
        _ground = new Plane(Vector3.up, Vector3.zero);

        _characterState = GetComponent<CharacterState>();
        _animator = GetComponent<AnimationController>();
        _spellbook = GetComponent<SpellbookState>();

        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;

        CharacterUtils.ApplySettings(_characterState, _agent, false);
    }

    void GetInput()
    {
        moveDirection.x = Input.GetAxis("Horizontal");
        moveDirection.z = Input.GetAxis("Vertical");

        if (Input.GetMouseButtonDown(0))
            FireSpell((int)Spell.Slot.LMB);

        if (Input.GetMouseButtonDown(1))
            FireSpell((int)Spell.Slot.RMB);

        if (Input.GetButtonDown("Jump"))
            FireSpell((int)Spell.Slot.ULT);
    }

    private void FireSpell(int index)
    {
        if (!_spellbook.IsSpellReady(index))
        {
            // Spell is not ready or missing - just exit
            return;
        }

        var groundPoint = Vector3.zero;
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

        var data = new SpellEmitterData
        {
            floorIntercection = groundPoint,
            hitInfo = hitInfo,
            sourceTransform = _characterState.GetNodeTransform(CharacterState.CharacterNode.NodeRole.SpellEmitter),
            owner = _characterState,
            ray = ray
        };

        _spellbook.FireSpell(index, data);
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
