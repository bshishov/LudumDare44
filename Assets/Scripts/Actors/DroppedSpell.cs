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
                character.Pickup(Spell, Stacks);

            if (interaction == Interaction.Dismantle)
            {
                if(Spell.ApplyBuffOnDismantle != null)
                    character.ApplyBuff(Spell.ApplyBuffOnDismantle, character, spell:null, Stacks);
            }

            Destroy(gameObject);
            CameraController.Instance.Shake(0.5f);
        }

        public void Setup(Spell spell, int stacks)
        {
            Spell = spell;
            Stacks = stacks;
        }
        
        public static GameObject InstantiateDroppedSpell(Spell spell, Vector3 position)
        {
            var go = GameObject.Instantiate(spell.DropItem, position, Quaternion.identity);
            if (go != null)
            {
                var dSpell = go.GetComponent<DroppedSpell>();
                if (dSpell != null)
                {
                    dSpell.Setup(spell, 1);
                }
            }

            return go;
        }
    }
}
