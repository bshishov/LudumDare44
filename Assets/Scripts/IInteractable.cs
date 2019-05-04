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
        bool IsActive { get; }
        InteractableType Type { get; }
        void Interact(CharacterState character, Interaction interaction);
        string GetInteractionText(Interaction interaction);
    }
}
