using UnityEngine;

public class DriveInteractable : MonoBehaviour, IInteractable
{
    public bool isHovering { get; set; }
    [field: SerializeField] public bool canInteract { get; set; } = true;

    [SerializeField] private string interactionText = "Drive the boat";
    [SerializeField] private string stopInteractionText = "Stop driving the boat";

    public string GetInteractionText()
    {
        if(PlayerBoatManager.Instance.holdingWheel)
        {
            return stopInteractionText;
        }
        else
        {
            return interactionText;
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
