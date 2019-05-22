using UnityEngine;
using Actors;
using Assets.Scripts;
using Assets.Scripts.Data;
using Data;
using Spells;
using UI;

public class PlayerController : MonoBehaviour, IChannelingInfo
{
    private Vector3 _inputMoveDirection;
    private CharacterState _characterState;
    private SpellbookState _spellbook;
    private MovementController _movement;
    private Interactor _interactor;

    void Start()
    {
        _characterState = GetComponent<CharacterState>();
        _spellbook = GetComponent<SpellbookState>();
        _interactor = GetComponent<Interactor>();
        _movement = GetComponent<MovementController>();
    }

    private void FireSpell(int slotIndex)
    {
        // TODO: Separate/decouple logic into spellbook
        var slotState = _spellbook.GetSpellSlotState(slotIndex);
        if (slotState.State != SpellbookState.SpellState.Ready)
        {
            // TODO: B-O-O-B! DO SOMETHING!
            if(slotState.Spell != null && slotState.Spell.Cooldown > 5f)
                UIInvalidAction.Instance?.Show(UIInvalidAction.InvalidAction.SpellIsNotReady);
            // Spell is not ready or missing - just exit
            return;
        }

        if (_characterState.Health <= slotState.Spell.BloodCost)
        {
            UIInvalidAction.Instance?.Show(UIInvalidAction.InvalidAction.NotEnoughBlood);
            // Not enough hp
            return;
        }
        
        var target = GetTarget();
        if (target == null)
        {
            UIInvalidAction.Instance?.Show(UIInvalidAction.InvalidAction.InvalidTarget);
            return;
        }

        if (_spellbook.TryFireSpellToTarget(slotIndex, target, this))
        {
            // If cast was successful, reduce hp by cost amount
            _characterState.ApplyModifier(ModificationParameter.HpFlat, 
                                          -slotState.Spell.BloodCost, 
                                          1, 
                                          1, 
                                          _characterState, 
                                          null);
        }
        else
        {
            UIInvalidAction.Instance?.Show(UIInvalidAction.InvalidAction.CantCastSpell);
        }
    }

    private TargetInfo GetTarget()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 100f, Common.LayerMasks.ActorsOrGround))
            return null;

        var target = new TargetInfo {Position = ray.GetPoint(hit.distance)};
        var tgt = hit.transform.GetComponent<CharacterState>();
        if (tgt != null && tgt.IsAlive)
        {
            target.Character = tgt;
            target.Transform = target.Character.GetNodeTransform(CharacterState.NodeRole.Chest);
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


    void Update()
    {
        if (!_characterState.IsAlive)
        {
            // Character is dead no need to move and handle input controls
            return;
        }

        // Get input movement direction
        _inputMoveDirection.x = Input.GetAxis(Common.Input.HorizontalAxis);
        _inputMoveDirection.z = Input.GetAxis(Common.Input.VerticalAxis);

        // Check buttons
        if (Input.GetMouseButton(0))
            FireSpell((int)Spell.Slot.LMB);

        if (Input.GetMouseButton(1))
            FireSpell((int)Spell.Slot.RMB);

        if (Input.GetButton(Common.Input.UltButton))
            FireSpell((int)Spell.Slot.ULT);

        // If there is an interactable in range and corresponding button is pressed
        // then interact with it
        if (_interactor.ClosestInRange != null)
        {
            if (Input.GetButtonDown(Common.Input.UseButton1))
                _interactor.ClosestInRange.Interact(_characterState, (Interaction)0);

            if (Input.GetButtonDown(Common.Input.UseButton2))
                _interactor.ClosestInRange.Interact(_characterState, (Interaction)1);
        }

        // Finally move using movement controller
        var motionVector = _inputMoveDirection.normalized * _characterState.Speed * Time.deltaTime;
        _movement.ControlMove(motionVector);

        // Rotate towards location under the cursor
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 100f, Common.LayerMasks.Ground))
        {
            _movement.ControlLookAt(hit.point);
        }
    }

    public TargetInfo GetNewTarget()
    {
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetButton(Common.Input.UltButton))
            return GetTarget();
        return null;
    }
}
