using Assets.Scripts;
using Data;
using Spells;
using UI;
using UnityEngine;

namespace Actors
{
    public class PlayerController : MonoBehaviour, ITargetLocationProvider
    {
        private Vector3 _inputMoveDirection;
        private CharacterState _characterState;
        private SpellbookState _spellbook;
        private MovementController _movement;
        private Interactor _interactor;
        private Camera _mainCamera;

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
            if (slotState.State != SpellbookState.SpellState.Ready)
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

        private void TryAbortSpell(int slotIndex)
        {
            var slotState = _spellbook.GetSpellSlotState(slotIndex);
            if (slotState.State == SpellbookState.SpellState.Firing)
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
                return new Target(this);

            // Construct screen ray
            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (type == TargetType.Character)
            {
                // Raycast characters
                if (Physics.Raycast(ray, out var charHit, 100f, Common.LayerMasks.Actors))
                {
                    var character = charHit.transform.GetComponent<CharacterState>();
                    if (character && character.IsAlive)
                        return new Target(character);
                }
            }

            if (Physics.Raycast(ray, out var hit, 100f, Common.LayerMasks.Ground))
            {
                // Hits ground -> return location target
                //var targetY = _characterState.GetNodeTransform(CharacterState.NodeRole.SpellEmitter).position.y;
                //return new Target(ray.origin + ray.direction * (targetY - ray.origin.y) / ray.direction.y);
                var forward = (hit.point - transform.position).normalized;
                return new Target(hit.point, forward);
            }

            return Target.None;
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
                FireSpell((int)SpellSlot.LMB);

            if (Input.GetMouseButtonUp(0))
                TryAbortSpell((int)SpellSlot.LMB);

            if (Input.GetMouseButton(1))
                FireSpell((int)SpellSlot.RMB);

            if (Input.GetMouseButtonUp(1))
                TryAbortSpell((int)SpellSlot.RMB);

            if (Input.GetButton(Common.Input.UltButton))
                FireSpell((int)SpellSlot.ULT);

            if (Input.GetButtonUp(Common.Input.UltButton))
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

            // Rotate towards location under the cursor
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 100f, Common.LayerMasks.Ground))
            {
                _movement.ControlLookAt(hit.point);
            }
        }

        public Vector3 GetTargetLocation()
        {
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetButton(Common.Input.UltButton))
                return GetTarget(TargetType.Location).Position;
            return Vector3.zero;
        }
    }
}
