using UnityEngine;

public class FishingRodInteractable : MonoBehaviour, IInteractable
{
    public bool isHovering { get; set; }
    public bool canInteract { get; set; }

    public string GetInteractionText()
    {
        return "Pegar vara de pesca";
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
