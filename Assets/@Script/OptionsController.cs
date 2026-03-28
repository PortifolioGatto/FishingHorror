using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class OptionsController : MonoBehaviour
{
    public GameObject optionsMenu;

    [Header("Video")]
    [SerializeField] private TMPro.TMP_Dropdown targetFramerate;

    [Header("Controls")]
    [SerializeField] private Slider sensibilitySlider;

    [Header("Audio")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    private IEnumerator Start()
    {
        yield return null;

        InitializeUI();

        LoadSettings();
    }

    public void ToggleOptionsMenu()
    {
        optionsMenu.SetActive(!optionsMenu.activeSelf);
    }

    private void LoadSettings()
    {
        int savedFramerateIndex = PlayerPrefs.GetInt("TargetFramerate", 2);
        targetFramerate.value = savedFramerateIndex;
        SetTargetFramerate(savedFramerateIndex);
    }

    private void InitializeUI()
    {
        targetFramerate.ClearOptions();
        targetFramerate.AddOptions(new System.Collections.Generic.List<string> { "Unlimited", "30 FPS", "60 FPS", "120 FPS", "144 FPS", "240 FPS" });

        targetFramerate.onValueChanged.AddListener(SetTargetFramerate);

        sensibilitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
        sensibilitySlider.onValueChanged.AddListener(value =>
        {
            PlayerPrefs.SetFloat("MouseSensitivity", value);
            InvokeConfigChanged();
        });

        masterVolumeSlider.value = AudioManager.Instance.GetMasterVolume();
        musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
        sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();

        masterVolumeSlider.onValueChanged.AddListener((value) =>
        {
            AudioManager.Instance.SetMasterVolume(value);
            InvokeConfigChanged();
        });
        musicVolumeSlider.onValueChanged.AddListener( (value) =>
        {
            AudioManager.Instance.SetMusicVolume(value);
            InvokeConfigChanged();
        });
        sfxVolumeSlider.onValueChanged.AddListener( (value) =>
        {
            AudioManager.Instance.SetSFXVolume(value);
            InvokeConfigChanged();
        });
    }

    public void SetTargetFramerate(int index)
    {
        int framerate = 30; // Padrão
        switch (index)
        {
            case 0: framerate = -1; break;
            case 1: framerate = 30; break;
            case 2: framerate = 60; break;
            case 3: framerate = 120; break;
            case 4: framerate = 144; break;
            case 5: framerate = 240; break;
        }
        Application.targetFrameRate = framerate;
        PlayerPrefs.SetInt("TargetFramerate", index);
        PlayerPrefs.Save();

        InvokeConfigChanged();
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
