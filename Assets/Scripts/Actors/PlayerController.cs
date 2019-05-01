using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
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

    void HandleInput()
    {
        moveDirection.x = Input.GetAxis("Horizontal");
        moveDirection.z = Input.GetAxis("Vertical");

        if (Input.GetMouseButton(0))
            FireSpell((int)Spell.Slot.LMB);

        if (Input.GetMouseButton(1))
            FireSpell((int)Spell.Slot.RMB);

        if (Input.GetButton("Ult"))
            FireSpell((int)Spell.Slot.ULT);

        if (Input.GetButtonDown("Use"))
            TryInteract((Interaction)0);

        if (Input.GetButtonDown("Use2"))
            TryInteract((Interaction)1);
    }

    private void FireSpell(int slotIndex)
    {
        if (!_spellbook.IsSpellReady(slotIndex))
        {
            // Spell is not ready or missing - just exit
            return;
        }

        var target = new TargetInfo();
        var groundPoint = Vector3.zero;
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (_ground.Raycast(ray, out var enter))
        {
            target.Position = ray.GetPoint(enter);
        }

        if (Physics.Raycast(ray, out var hit, LayerMask.GetMask("Actors")))
        {
            target.Character = hit.transform.GetComponent<CharacterState>();
        }
        else if(target.Position.HasValue)
        {
            var targetPosition = target.Position.Value;

            // Try locate target character located in target position
            var results = Physics.OverlapSphere(targetPosition, 1f, LayerMask.GetMask("Actors"));
            foreach (var result in results)
            {
                var character = result.GetComponent<CharacterState>();
                if (character == null)
                    continue;
                
                target.Character = character;
                break;
            }
        }
        else
        {
            Debug.LogWarning("Cant find any spell target");
            return;
        }

        if (target.Character)
        {
            target.Transform = target.Character.GetNodeTransform(CharacterState.NodeRole.Chest);
            target.Position = target.Transform.position;
        }
        else if(target.Position.HasValue)
        {
            var adoptedTarget = target.Position.Value;
            adoptedTarget.y = _characterState.GetNodeTransform(CharacterState.NodeRole.SpellEmitter).position.y;
            target.Position = adoptedTarget;
        }

        _spellbook.TryFireSpellToTarget(slotIndex, target);
    }

    private void TryInteract(Interaction interaction)
    {
        foreach (var interactable in GetUsableItemsInRange())
        {
            interactable.Interact(_characterState, interaction);
        }
    }

    private IEnumerable<IInteractable> GetUsableItemsInRange()
    {
        var pos = _characterState.GetNodeTransform(CharacterState.NodeRole.Chest).position;
        return Physics.OverlapSphere(pos, 1f)
            .Select(c => c.GetComponent<IInteractable>())
            .Where(i => i != null);
    }

    void Update()
    {
        if (_characterState.IsAlive)
        {
            HandleInput();
            Move();
            LookAt();
        }
    }

    void Move()
    {
        _agent.speed = _characterState.Speed;
        var motionVector = moveDirection.normalized * _characterState.Speed * Time.deltaTime;
        
        _agent.Move(motionVector);
        //_agent.SetDestination(transform.position + motionVector);
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
