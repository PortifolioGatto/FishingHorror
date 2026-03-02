using DG.Tweening;
using System.Collections;
using UnityEngine;
using VolumetricFogAndMist2;

public class WeatherController : MonoBehaviour
{
    [Header("Weather Settings")]
    [Header("Rainy Weather")]
    [SerializeField] private ParticleSystem rain_particles;
    [SerializeField] private AudioSource rain_audioLoop;
    [SerializeField] private GameObject thunder_effect; // Assign a thunder effect prefab (e.g., light flash, sound)

    [SerializeField] private float thunderIntervalMin = 5f; // Minimum time between thunder strikes
    [SerializeField] private float thunderIntervalMax = 15f; // Maximum time between thunder strikes
    [SerializeField] private float chanceToThunder = 0.1f; // Chance for thunder during rain

    private float nextThunderTime;

    private float rainVolume = 1f; // Volume for thunder sound, can be adjusted as needed    

    private Coroutine thunderCoroutine;

    public static WeatherController instance;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        rainVolume = rain_audioLoop.volume; // Store the original volume of the rain audio loop

        SetClearWeather();
    }

    [ContextMenu("Set Clear Weather")]
    public void SetClearWeather()
    {
        
        rain_particles.Stop(false);

        rain_audioLoop.DOFade(0f, 2f).OnComplete(() =>
        {
            rain_audioLoop.Stop();
            rain_audioLoop.volume = rainVolume; // Reset volume for next time
        });
    }

    [ContextMenu("Set Rainy Weather")]
    public void SetRainyWeather()
    {
        rain_particles.Play();

        if (thunderCoroutine == null)
        {
            thunderCoroutine = StartCoroutine(ThunderRoutine());
        }

        rain_audioLoop.volume = 0f;
        rain_audioLoop.Play();
        rain_audioLoop.DOFade(rainVolume, 2f);
    }

    public void EnableRain(bool enabled)
    {
        if(enabled)
        {
            if (!rain_particles.isPlaying)
            {
                rain_particles.Play();

                rain_audioLoop.volume = 0f;
                rain_audioLoop.Play();
                rain_audioLoop.DOFade(rainVolume, 2f);
            }
        }
        else
        {
            if (rain_particles.isPlaying)
            {
                rain_particles.Stop(false);

                rain_audioLoop.DOFade(0f, 2f).OnComplete(() =>
                {
                    rain_audioLoop.Stop();
                    rain_audioLoop.volume = rainVolume; // Reset volume for next time
                });
            }
        }
    }

    public void EnableThunder(bool enabled)
    {
        if (enabled) 
        {
            if (thunderCoroutine == null)
            {
                thunderCoroutine = StartCoroutine(ThunderRoutine());
            }
        }
        else
        {
            if (thunderCoroutine != null)
            {
                StopCoroutine(thunderCoroutine);
                thunderCoroutine = null;
            }
        }
    }


    [ContextMenu("Play Thunder")]
    public void PlayThunder()
    {
        StartCoroutine(EThunder());
    }

    private IEnumerator ThunderRoutine()
    {
        while (true)
        {
            if (Random.value < chanceToThunder)
            {
                yield return EThunder();
            }
            float waitTime = Random.Range(thunderIntervalMin, thunderIntervalMax);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private IEnumerator EThunder()
    {
        AudioSource src = AudioManager.Instance.PlaySFX("thunder", rain_particles.transform.position, Random.Range(0.5f, .75f));

        src.minDistance = 2500f; // Adjust as needed for thunder sound range
        src.maxDistance = 5000f; // Adjust as needed for thunder sound range

        int blinkCount = Random.Range(1, 4); // Randomize number of flashes
        for (int i = 0; i < blinkCount; i++)
        {
            thunder_effect.SetActive(true);
            yield return new WaitForSeconds(0.1f); // Duration of thunder effect
            thunder_effect.SetActive(false);
            yield return new WaitForSeconds(0.1f); // Time between flashes
        }
    }

}
