using System;
using System.Linq;
using Assets.Scripts;
using UnityEngine;

namespace Actors
{
    [RequireComponent(typeof(CharacterState))]
    public class Interactor : MonoBehaviour
    {
        public event Action<IInteractable, IInteractable> OnInteractableChanged;
        public IInteractable ClosestInRange => _inRange;
        public float InteractionRadius = 1f;
        public float PollCooldown = 0.2f;

        private IInteractable _inRange;
        private float _cd;
        private CharacterState _characterState;

        void Start()
        {
            _characterState = GetComponent<CharacterState>();
        }
    
        void Update()
        {
            if (_cd < PollCooldown)
            {
                _cd -= Time.deltaTime;
                var newInRange = GetClosestInteractable();
                if (newInRange != _inRange)
                {
                    OnInteractableChanged?.Invoke(_inRange, newInRange);
                }

                _inRange = newInRange;
            }
        }

        private IInteractable GetClosestInteractable()
        {
            var pos = _characterState.GetNodeTransform(CharacterState.NodeRole.Chest).position;
            return Physics.OverlapSphere(pos, InteractionRadius, Common.LayerMasks.Interactable)
                .OrderBy(go => Vector3.Distance(go.transform.position, pos))
                .Select(c => c.GetComponent<IInteractable>())
                .FirstOrDefault(interactable => interactable != null);
        }
    }
}
