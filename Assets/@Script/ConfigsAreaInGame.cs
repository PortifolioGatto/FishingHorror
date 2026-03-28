using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ConfigsAreaInGame : MonoBehaviour
{
    [SerializeField] private OptionsController optionsController;

    [SerializeField] private InputActionReference openConfigsMenuAction;

    public static string PlayerPrefsSensitivityKey = "MouseSensitivity";

    private void Start()
    {
        openConfigsMenuAction.action.performed += OpenConfigsMenu;
    }

    private void OnDestroy()
    {
        openConfigsMenuAction.action.performed -= OpenConfigsMenu;
    }


    private void OpenConfigsMenu(InputAction.CallbackContext context)
    {
        ToggleConfigsMenu();
    }

    public void ToggleConfigsMenu()
    {
        optionsController.ToggleOptionsMenu();

        if (optionsController.optionsMenu.activeSelf)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        PlayerCamera.Instance.cameraEnabled = !optionsController.optionsMenu.activeSelf;

        Time.timeScale = optionsController.optionsMenu.activeSelf ? 0f : 1f;
    }
}

public interface IListenConfigChanged
{
    void OnConfigChanged();
}