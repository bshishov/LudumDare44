using System;
using Assets.Scripts;
using Assets.Scripts.Data;
using UnityEngine;

namespace Actors
{
    public class DroppedSpell : MonoBehaviour, IInteractable
    {
        public int Stacks = 1;
        public Spell Spell;
        public InteractableType Type => InteractableType.DroppedSpell;

        public void Interact(CharacterState character, Interaction interaction)
        {
            if (interaction == Interaction.Pick)
            {
                character.Pickup(Spell, Stacks);
                Destroy(gameObject);
                CameraController.Instance.Shake(0.5f);
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
        
        public static GameObject InstantiateDroppedSpell(Spell spell, Vector3 position)
        {
            var go = GameObject.Instantiate(spell.DropItem, position, Quaternion.identity);
            if (go != null)
            {
                var dSpell = go.GetComponent<DroppedSpell>();
                if (dSpell != null)
                {
                    dSpell.Setup(spell);
                }
            }

            return go;
        }
    }
}
