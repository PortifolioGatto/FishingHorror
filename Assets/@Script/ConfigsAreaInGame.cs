using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ConfigsAreaInGame : MonoBehaviour
{
    [SerializeField] private GameObject configsMenu;

    [SerializeField] private Slider sensitivitySlider;

    [SerializeField] private InputActionReference openConfigsMenuAction;

    public static string PlayerPrefsSensitivityKey = "MouseSensitivity";

    private void Start()
    {
        sensitivitySlider.onValueChanged.AddListener(value =>
        {
            PlayerPrefs.SetFloat(PlayerPrefsSensitivityKey, value);
            PlayerPrefs.Save();

            InvokeConfigChanged();
        });

        sensitivitySlider.value = PlayerPrefs.GetFloat(PlayerPrefsSensitivityKey, 0.5f);

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
        configsMenu.SetActive(!configsMenu.activeSelf);

        if (configsMenu.activeSelf)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        PlayerCamera.Instance.cameraEnabled = !configsMenu.activeSelf;

        Time.timeScale = configsMenu.activeSelf ? 0f : 1f;
    }

    private void InvokeConfigChanged()
    {
        IListenConfigChanged[] listeners = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.InstanceID).OfType<IListenConfigChanged>().ToArray();
        foreach (var listener in listeners)
        {
            listener.OnConfigChanged();
        }
    }
}

public interface IListenConfigChanged
{
    void OnConfigChanged();
}