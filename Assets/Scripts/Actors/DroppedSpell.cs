using System;
using Assets.Scripts;
using Assets.Scripts.Data;
using UnityEngine;

namespace Actors
{
    public class DroppedSpell : MonoBehaviour, IInteractable
    {
        public Spell Spell;
        public InteractableType Type => InteractableType.DroppedSpell;

        public void Interact(CharacterState character, Interaction interaction)
        {
            if (interaction == Interaction.Pick)
            {
                character.Pickup(Spell);
                Destroy(gameObject);
            }

            if (interaction == Interaction.Discard)
            {
                throw new NotImplementedException();
            }
        }

        public void Setup(Spell spell)
        {
            Spell = spell;
        }
    }
}
