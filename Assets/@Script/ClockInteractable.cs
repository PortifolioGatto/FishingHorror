using UnityEngine;

public class ClockInteractable : MonoBehaviour, IInteractable
{
    public GameObject endDayPanel;

    public bool canInteract { get; set; } = true;
    public bool isHovering { get; set; }

    public void Interact()
    {
        OpenEndDayPanel();
    }

    private void OpenEndDayPanel()
    {
        endDayPanel.SetActive(true);
        PlayerController.Instance.SetBlocker(true);
        PlayerCamera.Instance.cameraEnabled = false;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseEndDayPanel()
    {
        endDayPanel.SetActive(false);
        PlayerController.Instance.SetBlocker(false);

        PlayerCamera.Instance.cameraEnabled = true;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void ConfirmEndDay()
    {

        GameManager.Instance.EndDay();
    }


    public void OnHover()
    {
    }
    public void OnHoverExit()
    {
    }

    public string GetInteractionText()
    {
        throw new System.NotImplementedException();
    }
}
