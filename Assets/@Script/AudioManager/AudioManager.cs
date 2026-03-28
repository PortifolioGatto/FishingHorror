using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioSource sfxPlayerPrefab;


    [Header("Audio Mixer Groups")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private AudioMixerGroup masterMixerGroup;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Space]

    [SerializeField] private SFXDatabase sfxDatabase;

    private const string MasterVolumeParam = "MasterVolume";
    private const string MusicVolumeParam = "MusicVolume";
    private const string SFXVolumeParam = "SFXVolume";


    private void Awake()
    {
        if(Instance == null)
        {
            InitializeMixer();

            Instance = this;
            DontDestroyOnLoad(this);
            return;
        }

        Destroy(this.gameObject);

        

    }
    private void Start()
    {
        
    }


    private void InitializeMixer()
    {
        Debug.Log("Initializing Audio Mixer with saved settings... " + PlayerPrefs.GetFloat("MasterVolume", 1f));
        SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 1f));
        SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 1f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 1f));
    }

    public void SetMasterVolume(float volume)
    {
        mainMixer.SetFloat(MasterVolumeParam, Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
        Debug.Log($"Master volume set to {volume} (dB: {Mathf.Log10(volume) * 20})");
    }
    public void SetMusicVolume(float volume)
    {
        mainMixer.SetFloat(MusicVolumeParam, Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }
    public void SetSFXVolume(float volume)
    {
        mainMixer.SetFloat(SFXVolumeParam, Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();
    }

    public float GetMasterVolume()
    {
        mainMixer.GetFloat(MasterVolumeParam, out float volume);

        float linearVolume = Mathf.Pow(10, volume / 20);

        Debug.Log($"Master volume retrieved: {linearVolume} (dB: {volume})");

        return linearVolume;
    }
    public float GetMusicVolume()
    {
        mainMixer.GetFloat(MusicVolumeParam, out float volume);
        return Mathf.Pow(10, volume / 20);
    }
    public float GetSFXVolume()
    {
        mainMixer.GetFloat(SFXVolumeParam, out float volume);
        return Mathf.Pow(10, volume / 20);
    }

    public AudioSource PlaySFX(AudioClip clip, Vector3 position, float volume = 1f, float pitchDelta = .05f)
    {
        AudioSource _as = Instantiate(sfxPlayerPrefab, position, Quaternion.identity);
        _as.clip = clip;
        _as.volume = volume;
        _as.outputAudioMixerGroup = sfxMixerGroup;
        _as.pitch = Random.Range(1f - pitchDelta, 1f + pitchDelta);
        _as.Play();
        Destroy(_as.gameObject, clip.length + 1f);
        return _as;
    }
    public AudioSource PlaySFX(string sfxName, Vector3 position, float volume = 1f, float pitchDelta = .05f)
    {
        if (sfxDatabase.sfxDictionary.Count == 0)
        {
            Debug.LogWarning("SFX Database is empty. Please add sound effects to the database.");
            return null;
        }
        if (!sfxDatabase.sfxDictionary.ContainsKey(sfxName.ToLower()))
        {
            Debug.LogWarning($"SFX '{sfxName}' not found in the database.");
            return null;
        }
        AudioClip clip = sfxDatabase.sfxDictionary[sfxName.ToLower()].GetRandomClip();

        AudioSource _as = Instantiate(sfxPlayerPrefab, position, Quaternion.identity);
        _as.clip = clip;
        _as.volume = volume;
        _as.outputAudioMixerGroup = sfxMixerGroup;
        _as.pitch = Random.Range(1f - pitchDelta, 1f + pitchDelta);
        _as.Play();

        Destroy(_as.gameObject, clip.length + 1f);

        return _as;
    }

    public AudioSource PlaySFXLoop(string sfxName, Vector3 position, float volume = 1f)
    {
        if (sfxDatabase.sfxDictionary.Count == 0)
        {
            Debug.LogWarning("SFX Database is empty. Please add sound effects to the database.");
            return null;
        }
        if (!sfxDatabase.sfxDictionary.ContainsKey(sfxName.ToLower()))
        {
            Debug.LogWarning($"SFX '{sfxName}' not found in the database.");
            return null;
        }
        AudioClip clip = sfxDatabase.sfxDictionary[sfxName.ToLower()].GetRandomClip();
        AudioSource _as = Instantiate(sfxPlayerPrefab, position, Quaternion.identity);
        _as.clip = clip;
        _as.volume = volume;
        _as.outputAudioMixerGroup = sfxMixerGroup;
        _as.pitch = Random.Range(0.95f, 1.05f);
        _as.loop = true;
        _as.Play();

        return _as;
    }
}
