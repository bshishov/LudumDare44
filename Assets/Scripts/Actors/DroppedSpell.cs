using Assets.Scripts;
using UnityEngine;

namespace Actors
{
    public class DroppedSpell : MonoBehaviour, IInteractable
    {
        public InteractableType Type => InteractableType.DroppedSpell;

        public void Interact(CharacterState character, Interaction interaction)
        {
            if (interaction == Interaction.Pick)
            {

            }

            if (interaction == Interaction.Discard)
            {

            }
        }
    }
}
