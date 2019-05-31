using Assets.Scripts;
using Data;
using Spells;
using UI;
using UnityEngine;
using UnityEngine.AI;

namespace Actors
{
    public class PlayerController : MonoBehaviour
    {
        private Vector3 _inputMoveDirection;
        private CharacterState _characterState;
        private SpellbookState _spellbook;
        private MovementController _movement;
        private Interactor _interactor;
        private Camera _mainCamera;
        private readonly TargetLocationProvider _cursorTarget = new TargetLocationProvider();
        private Vector3? _cursorWorldPosition;

        void Start()
        {
            _mainCamera = Camera.main;
            _characterState = GetComponent<CharacterState>();
            _spellbook = GetComponent<SpellbookState>();
            _interactor = GetComponent<Interactor>();
            _movement = GetComponent<MovementController>();
        }

        private void FireSpell(int slotIndex)
        {
            // TODO: Separate/decouple logic into spellbook
            var slotState = _spellbook.GetSpellSlotState(slotIndex);
            if (slotState.State != SpellbookState.SlotState.Ready)
            {
                // TODO: B-O-O-B! DO SOMETHING!
                if(slotState.Spell != null && slotState.Spell.Cooldown.GetValue(slotState.NumStacks) > 5f)
                    UIInvalidAction.Instance?.Show(UIInvalidAction.InvalidAction.SpellIsNotReady);
                // Spell is not ready or missing - just exit
                return;
            }

            var bloodCost = slotState.Spell.BloodCost.GetValue(slotState.NumStacks);
            if (_characterState.Health <= bloodCost)
            {
                UIInvalidAction.Instance?.Show(UIInvalidAction.InvalidAction.NotEnoughBlood);
                // Not enough hp
                return;
            }
        
            var target = GetTarget(slotState.Spell.TargetType);
            if (!target.IsValid)
            {
                UIInvalidAction.Instance?.Show(UIInvalidAction.InvalidAction.InvalidTarget);
                return;
            }

            if (_spellbook.TryFireSpellToTarget(slotIndex, target))
            {
                // If cast was successful, reduce hp by cost amount
                _characterState.ApplyModifier(ModificationParameter.HpFlat, 
                    -bloodCost, 
                    _characterState, 
                    null);
            }
            else
            {
                UIInvalidAction.Instance?.Show(UIInvalidAction.InvalidAction.CantCastSpell);
            }
        }

        private void TryAbortSpell(int slotIndex)
        {
            var slotState = _spellbook.GetSpellSlotState(slotIndex);
            if (slotState.State == SpellbookState.SlotState.Firing)
            {
                if(slotState.Spell.ManualAbort)
                    slotState.SpellHandler?.Abort();
            }
        }

        private Target GetTarget(TargetType type)
        {
            if(type == TargetType.None)
                return Target.None;

            if (type == TargetType.LocationProvider)
                return new Target(_cursorTarget);

            // Construct screen ray
            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (type == TargetType.Character)
            {
                // Raycast characters
                // TODO: raycast all?
                if (Physics.Raycast(ray, out var charHit, 100f, Common.LayerMasks.Actors))
                {
                    var character = charHit.transform.GetComponent<CharacterState>();
                    if (character && character.IsAlive)
                        return new Target(character);
                }
            }

            if (_cursorTarget.IsValid)
                return new Target(_cursorTarget.Location);
            
            return Target.None;
        }

        private Vector3? GetCursorPointOnGround()
        {
            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 100f, Common.LayerMasks.Ground))
                return hit.point;
            return null;
        }

        void Update()
        {
            if (!_characterState.IsAlive)
            {
                // Character is dead no need to move and handle input controls
                return;
            }
            
            // Tray raycast cursor to ground 
            _cursorWorldPosition = GetCursorPointOnGround();

            // Get input movement direction
            _inputMoveDirection.x = Input.GetAxis(Common.Input.HorizontalAxis);
            _inputMoveDirection.z = Input.GetAxis(Common.Input.VerticalAxis);

            // Check buttons
            var lmb = Input.GetMouseButton(0);
            var lmbUp = Input.GetMouseButtonUp(0);
            var rmb = Input.GetMouseButton(1);
            var rmbUp = Input.GetMouseButtonUp(1);
            var ult = Input.GetButton(Common.Input.UltButton);
            var ultUp = Input.GetButtonUp(Common.Input.UltButton);

            // If anything is pressed and cursor has the location on ground (raycast succeeded)
            // then dynamic target is updated
            if ((lmb || rmb || ult) && _cursorWorldPosition.HasValue)
            {
                _cursorTarget.IsValid = true;
                _cursorTarget.Location = _cursorWorldPosition.Value;
            }
            else
            {
                _cursorTarget.IsValid = false;
            }
            
            if (lmb)
                FireSpell((int)SpellSlot.LMB);

            if (lmbUp)
                TryAbortSpell((int)SpellSlot.LMB);

            if (rmb)
                FireSpell((int)SpellSlot.RMB);

            if (rmbUp)
                TryAbortSpell((int)SpellSlot.RMB);

            if (ult)
                FireSpell((int)SpellSlot.ULT);

            if (ultUp)
                TryAbortSpell((int)SpellSlot.ULT);

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
            var motionVector = _characterState.Speed * Time.deltaTime * _inputMoveDirection.normalized;
            _movement.ControlMove(motionVector);

            // Rotate towards cursor
            if (_cursorWorldPosition.HasValue)
                _movement.ControlLookAt(_cursorWorldPosition.Value);
        }
    }
}
