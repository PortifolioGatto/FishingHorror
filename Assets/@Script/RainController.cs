using UnityEngine;
using VolumetricFogAndMist2;

public class RainController : MonoBehaviour
{
    [System.Serializable]
    public class RainSettings
    {
        [Header("Clouds Settings")]
        public float targetDensity = 0.5f;
        public Color color;

        [Header("Rain Settings")]
        public bool enableRain = false;
        public bool enableThunder = false;
    }

    [SerializeField] private RainSettings earlySettings;
    [SerializeField] private RainSettings highnoonSettings;
    [SerializeField] private RainSettings afternoonSettings;
    [SerializeField] private RainSettings nightSettings;

    [SerializeField] private VolumetricFog clouds;

    private float currentDensity;
    private Color currentColor;
    private bool currentEnableRain;
    private bool currentEnableThunder;

    public float CurrentDensity => currentDensity;
    public Color CurrentColor => currentColor;
    public bool CurrentEnableRain => currentEnableRain;
    public bool CurrentEnableThunder => currentEnableThunder;

    public static RainController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        currentColor = clouds.profile.specularColor;
        currentDensity = clouds.profile.density;
    }

    private void Update()
    {
        float timeOfDay = DayNightCycle.Instance.GetTimeOfDay();

        // timeOfDay: 0 = night, 0.25 = early morning, 0.5 = high noon, 0.75 = afternoon, 1 = night
        // We define 4 zones and lerp between adjacent settings:
        //   [0.00 - 0.25] night     -> early
        //   [0.25 - 0.50] early     -> highnoon
        //   [0.50 - 0.75] highnoon  -> afternoon
        //   [0.75 - 1.00] afternoon -> night

        RainSettings from;
        RainSettings to;
        float t;

        if (timeOfDay < 0.25f)
        {
            from = nightSettings;
            to = earlySettings;
            t = timeOfDay / 0.25f;
        }
        else if (timeOfDay < 0.5f)
        {
            from = earlySettings;
            to = highnoonSettings;
            t = (timeOfDay - 0.25f) / 0.25f;
        }
        else if (timeOfDay < 0.75f)
        {
            from = highnoonSettings;
            to = afternoonSettings;
            t = (timeOfDay - 0.5f) / 0.25f;
        }
        else
        {
            from = afternoonSettings;
            to = nightSettings;
            t = (timeOfDay - 0.75f) / 0.25f;
        }

        t = Mathf.SmoothStep(0f, 1f, t);

        currentDensity = Mathf.Lerp(from.targetDensity, to.targetDensity, t);
        currentColor = Color.Lerp(from.color, to.color, t);

        clouds.profile.noiseStrength = currentDensity;
        clouds.profile.albedo = currentColor;

        // For rain and thunder, we can just switch at the midpoint of each zone for simplicity
        WeatherController.instance.EnableRain(from.enableRain);
        WeatherController.instance.EnableThunder(from.enableThunder);
    }
}