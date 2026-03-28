using UnityEngine;
using UnityEngine.Localization;

public class FishingRodInteractable : MonoBehaviour, IInteractable
{
    public bool isHovering { get; set; }
    public bool canInteract { get; set; }

    public LocalizedString interactionText;

    public string GetInteractionText()
    {
        return interactionText.GetLocalizedString();
    }

    public void Interact()
    {
        gameObject.SetActive(false);
        PlayerFishingSystem.Instance.EquipFishingRod();
    }

    public void OnHover()
    {
        
    }

    public void OnHoverExit()
    {
        
    }
}
