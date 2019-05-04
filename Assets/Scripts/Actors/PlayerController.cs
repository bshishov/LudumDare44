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
    
    private CharacterState _characterState;
    private SpellbookState _spellbook;

    private NavMeshAgent _agent;
    private AnimationController _animator;

    void Start()
    {
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
        var slotState = _spellbook.GetSpellSlotState(slotIndex);
        if (slotState.State != SpellbookState.SpellState.Ready)
        {
            // Spell is not ready or missing - just exit
            return;
        }

        if (_characterState.Health <= slotState.Spell.BloodCost)
        {
            // Not enough hp
            return;
        }
        
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, Common.LayerMasks.ActorsOrGround))
        {
            var target = new TargetInfo { Position = ray.GetPoint(hit.distance) };
            var tgt = hit.transform.GetComponent<CharacterState>();
            if (tgt != null && tgt.IsAlive)
            {
                target.Character = tgt;
                target.Transform = target.Character.GetNodeTransform(CharacterState.NodeRole.Chest);
                target.Position = target.Transform.position;
            }
            else
            {
                // TODO: FIX targets for ground-parallel projectiles
                var adoptedTarget = target.Position.Value;
                adoptedTarget.y = _characterState.GetNodeTransform(CharacterState.NodeRole.SpellEmitter).position.y;
                target.Position = adoptedTarget;
            }

            if (_spellbook.TryFireSpellToTarget(slotIndex, target))
            {
                // If cast was successful, reduce hp by cost amount
                _characterState.ApplyModifier(
                    ModificationParameter.HpFlat, 
                    -slotState.Spell.BloodCost, 
                    1, 
                    1, 
                    _characterState, 
                    null, 
                    out _);
            }
        }
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
        if (Physics.Raycast(ray, out var hit, Common.LayerMasks.Ground))
        {
            var hitPoint = ray.GetPoint(hit.distance);
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

        if (Physics.Raycast(ray, out var hit, Common.LayerMasks.Ground))
        {
            var hitPoint = ray.GetPoint(hit.distance);
            Gizmos.DrawSphere(hitPoint, 0.2f);
        }

        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, 100, Common.LayerMasks.Actors))
        {
            Gizmos.DrawWireCube(hitInfo.transform.position, Vector3.one);
        }
    }

    public static Vector3? GetGroundPositionUnderCursor()
    {
        if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, 100f, Common.LayerMasks.Ground))
        {
            return hit.point;
        }

        return null;
    }
}
