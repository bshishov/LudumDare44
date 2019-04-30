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

        if (Input.GetMouseButtonDown(0))
            FireSpell((int)Spell.Slot.LMB);

        if (Input.GetMouseButtonDown(1))
            FireSpell((int)Spell.Slot.RMB);

        if (Input.GetButtonDown("Ult"))
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

        var groundPoint = Vector3.zero;
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (_ground.Raycast(ray, out var enter))
        {
            groundPoint = ray.GetPoint(enter);
        }

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, LayerMask.GetMask("Actors")))
        {
            var tgtCharacter = hit.transform.GetComponent<CharacterState>();
            if(tgtCharacter != null)
                _spellbook.TryFireSpellToTarget(slotIndex, tgtCharacter);
            return;
        }
        
        _spellbook.TryFireSpellToPoint(slotIndex, groundPoint);
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
        return Physics.OverlapSphere(transform.position, 1f)
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
