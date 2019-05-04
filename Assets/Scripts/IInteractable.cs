using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    public enum Interaction : int
    {
        Use = 0,
        Pick = 0,
        Buy = 0,
        Dismantle = 1
    }

    public enum InteractableType
    {
        Shop,
        DroppedItem,
        DroppedSpell
    }

    public interface IInteractable
    {
        InteractableType Type { get; }
        void Interact(CharacterState character, Interaction interaction);
    }
}
