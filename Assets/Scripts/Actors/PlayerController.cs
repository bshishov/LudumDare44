using UnityEngine;
using Actors;
using Assets.Scripts;
using Assets.Scripts.Data;
using Spells;

public class PlayerController : MonoBehaviour, IChannelingInfo
{
    public float maxSpeed = 7;
    private Vector3 moveDirection = new Vector3();
    
    private CharacterState _characterState;
    private SpellbookState _spellbook;
    private MovementController _movement;

    private AnimationController _animator;
    private const float InteractRadius = 1f;
    private Interactor _interactor;

    void Start()
    {
        _characterState = GetComponent<CharacterState>();
        _animator = GetComponent<AnimationController>();
        _spellbook = GetComponent<SpellbookState>();
        _interactor = GetComponent<Interactor>();
        _movement = GetComponent<MovementController>();
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
        
        var target = GetTarget();
        if(target == null)
            return;

        if (_spellbook.TryFireSpellToTarget(slotIndex, target, this))
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

    private TargetInfo GetTarget()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, Common.LayerMasks.ActorsOrGround))
            return null;

        var target = new TargetInfo {Position = ray.GetPoint(hit.distance)};
        var tgt = hit.transform.GetComponent<CharacterState>();
        if (tgt != null && tgt.IsAlive)
        {
            target.Character = tgt;
            target.Transform = target.Character.GetNodeTransform(CharacterState.NodeRole.Chest);
            target.Position  = target.Transform.position;
        }
        else
        {
            // TODO: FIX targets for ground-parallel projectiles
            var adoptedTarget = target.Position.Value;
            adoptedTarget.y = _characterState.GetNodeTransform(CharacterState.NodeRole.SpellEmitter).position.y;
            target.Position = adoptedTarget;
        }

        return target;
    }

    private void TryInteract(Interaction interaction)
    {
        _interactor.ClosestInRange?.Interact(_characterState, interaction);
    }

    void Update()
    {
        if (_characterState.IsAlive)
        {
            HandleInput();

            var motionVector = moveDirection.normalized * _characterState.Speed * Time.deltaTime;
            _movement.Move(motionVector);

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, Common.LayerMasks.Ground))
            {
                _movement.LookAt(hit.point);
            }
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

    public TargetInfo GetNewTarget()
    {
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetButton("Ult"))
            return GetTarget();
        return null;
    }
}
