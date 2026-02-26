public interface IInteractable
{
    bool isHovering { get; set; }
    bool canInteract { get; set; }

    void OnHover();
    void OnHoverExit();

    void Interact();

    string GetInteractionText();
}
