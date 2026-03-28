using UnityEngine;
using UnityEngine.Localization;

public class DriveInteractable : MonoBehaviour, IInteractable
{
    public bool isHovering { get; set; }
    [field: SerializeField] public bool canInteract { get; set; } = true;

    public LocalizedString interactionText;
    public LocalizedString stopInteractionText;

    public string GetInteractionText()
    {
        if(!PlayerBoatManager.Instance.canDrive) return "";

        if (PlayerBoatManager.Instance.holdingWheel)
        {
            return stopInteractionText.GetLocalizedString();
        }
        else
        {
            return interactionText.GetLocalizedString();
        }
    }

    public void Interact()
    {
        PlayerBoatManager.Instance.ToggleDrivingMode();
    }

    public void OnHover()
    {
        
    }

    public void OnHoverExit()
    {
        
    }
}
