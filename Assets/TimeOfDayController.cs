using UnityEngine;

public class TimeOfDayController : MonoBehaviour
{
    [System.Serializable]
    public class TimeOfDaySettings
    {
        [Header("Time Period")]
        public string timeName;

        [Header("Skybox")]
        public Material skyboxMaterial;

        [Header("Fog Settings")]
        public bool enableFog = true;
        public FogMode fogMode = FogMode.ExponentialSquared;
        public Color fogColor = Color.gray;
        [Range(0f, 0.1f)]
        public float fogDensity = 0.01f;

        [Header("Lighting")]
        public Color ambientColor = Color.gray;
        [Range(0f, 2f)]
        public float ambientIntensity = 1f;
        public Color sunColor = Color.white;
        [Range(0f, 3f)]
        public float sunIntensity = 1f;
    }

    [Header("Time of Day Presets")]
    public TimeOfDaySettings morningSettings = new TimeOfDaySettings { timeName = "Morning" };
    public TimeOfDaySettings noonSettings = new TimeOfDaySettings { timeName = "Noon" };
    public TimeOfDaySettings eveningSettings = new TimeOfDaySettings { timeName = "Evening" };
    public TimeOfDaySettings nightSettings = new TimeOfDaySettings { timeName = "Night" };

    [Header("Sun Light Reference")]
    public Light sunLight;

    [Header("Debug")]
    public bool randomizeOnStart = true;
    public bool showCurrentTime = true;

    private TimeOfDaySettings currentSettings;
    private TimeOfDaySettings[] allSettings;

    void Start()
    {
        InitializeSettings();

        if (randomizeOnStart)
        {
            SetRandomTimeOfDay();
        }
        else
        {
            SetTimeOfDay(0); // Default to morning
        }
    }

    void InitializeSettings()
    {
        // Initialize default values for each time of day
        SetupMorningDefaults();
        SetupNoonDefaults();
        SetupEveningDefaults();
        SetupNightDefaults();

        allSettings = new TimeOfDaySettings[] { morningSettings, noonSettings, eveningSettings, nightSettings };
    }

    void SetupMorningDefaults()
    {
        morningSettings.timeName = "Morning";
        morningSettings.fogColor = new Color(0.8f, 0.9f, 1f, 1f); // Light blue
        morningSettings.fogDensity = 0.005f;
        morningSettings.ambientColor = new Color(0.7f, 0.8f, 1f, 1f);
        morningSettings.ambientIntensity = 0.8f;
        morningSettings.sunColor = new Color(1f, 0.95f, 0.8f, 1f);
        morningSettings.sunIntensity = 1.2f;
    }

    void SetupNoonDefaults()
    {
        noonSettings.timeName = "Noon";
        noonSettings.fogColor = new Color(0.9f, 0.95f, 1f, 1f); // Very light blue
        noonSettings.fogDensity = 0.003f;
        noonSettings.ambientColor = new Color(0.5f, 0.7f, 1f, 1f);
        noonSettings.ambientIntensity = 1.2f;
        noonSettings.sunColor = Color.white;
        noonSettings.sunIntensity = 2f;
    }

    void SetupEveningDefaults()
    {
        eveningSettings.timeName = "Evening";
        eveningSettings.fogColor = new Color(1f, 0.7f, 0.5f, 1f); // Orange/pink
        eveningSettings.fogDensity = 0.008f;
        eveningSettings.ambientColor = new Color(1f, 0.6f, 0.4f, 1f);
        eveningSettings.ambientIntensity = 0.9f;
        eveningSettings.sunColor = new Color(1f, 0.6f, 0.3f, 1f);
        eveningSettings.sunIntensity = 1.5f;
    }

    void SetupNightDefaults()
    {
        nightSettings.timeName = "Night";
        nightSettings.fogColor = new Color(0.2f, 0.3f, 0.5f, 1f); // Dark blue
        nightSettings.fogDensity = 0.015f;
        nightSettings.ambientColor = new Color(0.2f, 0.3f, 0.6f, 1f);
        nightSettings.ambientIntensity = 0.3f;
        nightSettings.sunColor = new Color(0.8f, 0.9f, 1f, 1f);
        nightSettings.sunIntensity = 0.5f;
    }

    public void SetRandomTimeOfDay()
    {
        int randomIndex = Random.Range(0, allSettings.Length);
        SetTimeOfDay(randomIndex);
    }

    public void SetTimeOfDay(int timeIndex)
    {
        if (timeIndex < 0 || timeIndex >= allSettings.Length)
        {
            Debug.LogWarning("Invalid time index: " + timeIndex);
            return;
        }

        currentSettings = allSettings[timeIndex];
        ApplyTimeOfDaySettings();

        if (showCurrentTime)
        {
            Debug.Log($"Time of Day set to: {currentSettings.timeName}");
        }
    }

    void ApplyTimeOfDaySettings()
    {
        if (currentSettings == null) return;

        // Apply Skybox
        if (currentSettings.skyboxMaterial != null)
        {
            RenderSettings.skybox = currentSettings.skyboxMaterial;
        }

        // Apply Fog Settings
        RenderSettings.fog = currentSettings.enableFog;
        RenderSettings.fogMode = currentSettings.fogMode;
        RenderSettings.fogColor = currentSettings.fogColor;
        RenderSettings.fogDensity = currentSettings.fogDensity;

        // Apply Ambient Lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = currentSettings.ambientColor;
        RenderSettings.ambientIntensity = currentSettings.ambientIntensity;

        // Apply Sun Settings
        if (sunLight != null)
        {
            sunLight.color = currentSettings.sunColor;
            sunLight.intensity = currentSettings.sunIntensity;
            RenderSettings.sun = sunLight;
        }

        // Force skybox update
        DynamicGI.UpdateEnvironment();
    }

    // Public methods to manually set specific times
    public void SetMorning() => SetTimeOfDay(0);
    public void SetNoon() => SetTimeOfDay(1);
    public void SetEvening() => SetTimeOfDay(2);
    public void SetNight() => SetTimeOfDay(3);

    // Get current time name for UI or debugging
    public string GetCurrentTimeName()
    {
        return currentSettings?.timeName ?? "Unknown";
    }

    void OnValidate()
    {
        // Update settings in real-time when editing in inspector
        if (Application.isPlaying && currentSettings != null)
        {
            ApplyTimeOfDaySettings();
        }
    }
}
