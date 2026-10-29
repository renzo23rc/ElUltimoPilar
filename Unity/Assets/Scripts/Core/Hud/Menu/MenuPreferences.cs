using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Stores and applies local menu preferences independently from match state.
/// </summary>
public static class MenuPreferences
{
    private const string PreferencesFileName = "menu-preferences.json";
    private const int CurrentSchemaVersion = 1;
    private const float MinimumVolume = 0f;
    private const float MaximumVolume = 1f;
    private const float MinimumLookSensitivity = 0.1f;
    private const float MaximumLookSensitivity = 5f;
    private const float MinimumShakeIntensity = 0f;
    private const float MaximumShakeIntensity = 2f;

    [Serializable]
    private sealed class PreferencesData
    {
        public int schemaVersion = CurrentSchemaVersion;
        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float effectsVolume = 1f;
        public float lookSensitivity = 1f;
        public float shakeIntensity = 1f;
        public bool invertY;
        public bool fullscreen;
        public bool vSync = true;
        public int qualityLevel;
        public int resolutionWidth;
        public int resolutionHeight;
    }

    private static PreferencesData data;
    private static bool initialized;

    /// <summary>Gets the master output volume in the range 0–1.</summary>
    public static float MasterVolume { get { EnsureInitialized(); return data.masterVolume; } }
    /// <summary>Gets the music volume in the range 0–1.</summary>
    public static float MusicVolume { get { EnsureInitialized(); return data.musicVolume; } }
    /// <summary>Gets the effects volume in the range 0–1.</summary>
    public static float EffectsVolume { get { EnsureInitialized(); return data.effectsVolume; } }
    /// <summary>Gets the look sensitivity multiplier.</summary>
    public static float LookSensitivity { get { EnsureInitialized(); return data.lookSensitivity; } }
    /// <summary>Gets the combat shake multiplier.</summary>
    public static float ShakeIntensity { get { EnsureInitialized(); return data.shakeIntensity; } }
    /// <summary>Gets whether vertical look input is inverted.</summary>
    public static bool InvertY { get { EnsureInitialized(); return data.invertY; } }
    /// <summary>Gets whether fullscreen mode is enabled.</summary>
    public static bool Fullscreen { get { EnsureInitialized(); return data.fullscreen; } }
    /// <summary>Gets whether vertical synchronization is enabled.</summary>
    public static bool VSync { get { EnsureInitialized(); return data.vSync; } }
    /// <summary>Gets the active quality level.</summary>
    public static int QualityLevel { get { EnsureInitialized(); return data.qualityLevel; } }
    /// <summary>Gets the configured resolution width.</summary>
    public static int ResolutionWidth { get { EnsureInitialized(); return data.resolutionWidth; } }
    /// <summary>Gets the configured resolution height.</summary>
    public static int ResolutionHeight { get { EnsureInitialized(); return data.resolutionHeight; } }

    /// <summary>Sets the master output volume.</summary>
    public static void SetMasterVolume(float value)
    {
        EnsureInitialized();
        data.masterVolume = ClampFinite(value, data.masterVolume, MinimumVolume, MaximumVolume);
        AudioListener.volume = data.masterVolume;
        Save();
    }

    /// <summary>Sets the music volume.</summary>
    public static void SetMusicVolume(float value)
    {
        EnsureInitialized();
        data.musicVolume = ClampFinite(value, data.musicVolume, MinimumVolume, MaximumVolume);
        Save();
    }

    /// <summary>Sets the effects volume.</summary>
    public static void SetEffectsVolume(float value)
    {
        EnsureInitialized();
        data.effectsVolume = ClampFinite(value, data.effectsVolume, MinimumVolume, MaximumVolume);
        Save();
    }

    /// <summary>Sets the look sensitivity multiplier.</summary>
    public static void SetLookSensitivity(float value)
    {
        EnsureInitialized();
        data.lookSensitivity = ClampFinite(value, data.lookSensitivity, MinimumLookSensitivity, MaximumLookSensitivity);
        Save();
    }

    /// <summary>Sets the combat shake multiplier.</summary>
    public static void SetShakeIntensity(float value)
    {
        EnsureInitialized();
        data.shakeIntensity = ClampFinite(value, data.shakeIntensity, MinimumShakeIntensity, MaximumShakeIntensity);
        Save();
    }

    /// <summary>Sets whether vertical look input is inverted.</summary>
    public static void SetInvertY(bool value)
    {
        EnsureInitialized();
        data.invertY = value;
        Save();
    }

    /// <summary>Sets fullscreen mode immediately.</summary>
    public static void SetFullscreen(bool value)
    {
        EnsureInitialized();
        data.fullscreen = value;
        Screen.fullScreen = value;
        Save();
    }

    /// <summary>Sets vertical synchronization immediately.</summary>
    public static void SetVSync(bool value)
    {
        EnsureInitialized();
        data.vSync = value;
        QualitySettings.vSyncCount = value ? 1 : 0;
        Save();
    }

    /// <summary>Sets a supported Unity quality level immediately.</summary>
    public static void SetQualityLevel(int value)
    {
        EnsureInitialized();
        if (!TryClampQualityLevel(value, out int qualityLevel))
        {
            return;
        }

        data.qualityLevel = qualityLevel;
        QualitySettings.SetQualityLevel(qualityLevel, true);
        QualitySettings.vSyncCount = data.vSync ? 1 : 0;
        Save();
    }

    /// <summary>Sets a resolution supported by the current display.</summary>
    public static void SetResolution(int width, int height)
    {
        EnsureInitialized();
        if (!IsSupportedResolution(width, height))
        {
            return;
        }

        data.resolutionWidth = width;
        data.resolutionHeight = height;
        Screen.SetResolution(width, height, data.fullscreen);
        Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        initialized = false;
        EnsureInitialized();
    }

    private static void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        data = CreateDefaults();
        Load();
        ApplyAll();
    }

    private static PreferencesData CreateDefaults()
    {
        return new PreferencesData
        {
            fullscreen = Screen.fullScreen,
            vSync = QualitySettings.vSyncCount > 0,
            qualityLevel = QualitySettings.GetQualityLevel(),
            resolutionWidth = Screen.width,
            resolutionHeight = Screen.height
        };
    }

    private static void Load()
    {
        string path = GetPreferencesPath();
        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            PreferencesData loaded = JsonUtility.FromJson<PreferencesData>(File.ReadAllText(path));
            if (loaded == null || loaded.schemaVersion != CurrentSchemaVersion)
            {
                return;
            }

            data.masterVolume = ClampFinite(loaded.masterVolume, data.masterVolume, MinimumVolume, MaximumVolume);
            data.musicVolume = ClampFinite(loaded.musicVolume, data.musicVolume, MinimumVolume, MaximumVolume);
            data.effectsVolume = ClampFinite(loaded.effectsVolume, data.effectsVolume, MinimumVolume, MaximumVolume);
            data.lookSensitivity = ClampFinite(loaded.lookSensitivity, data.lookSensitivity, MinimumLookSensitivity, MaximumLookSensitivity);
            data.shakeIntensity = ClampFinite(loaded.shakeIntensity, data.shakeIntensity, MinimumShakeIntensity, MaximumShakeIntensity);
            data.invertY = loaded.invertY;
            data.fullscreen = loaded.fullscreen;
            data.vSync = loaded.vSync;
            if (TryClampQualityLevel(loaded.qualityLevel, out int qualityLevel))
            {
                data.qualityLevel = qualityLevel;
            }

            if (IsSupportedResolution(loaded.resolutionWidth, loaded.resolutionHeight))
            {
                data.resolutionWidth = loaded.resolutionWidth;
                data.resolutionHeight = loaded.resolutionHeight;
            }
        }
        catch (IOException exception)
        {
            Debug.LogWarning($"[MenuPreferences] Could not read local preferences: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogWarning($"[MenuPreferences] Local preferences are not accessible: {exception.Message}");
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning($"[MenuPreferences] Local preferences are invalid: {exception.Message}");
        }
    }

    private static void ApplyAll()
    {
        AudioListener.volume = data.masterVolume;
        Screen.fullScreen = data.fullscreen;
        if (TryClampQualityLevel(data.qualityLevel, out int qualityLevel))
        {
            data.qualityLevel = qualityLevel;
            QualitySettings.SetQualityLevel(qualityLevel, true);
        }

        QualitySettings.vSyncCount = data.vSync ? 1 : 0;
        if (IsSupportedResolution(data.resolutionWidth, data.resolutionHeight))
        {
            Screen.SetResolution(data.resolutionWidth, data.resolutionHeight, data.fullscreen);
        }
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(GetPreferencesPath(), JsonUtility.ToJson(data, true));
        }
        catch (IOException exception)
        {
            Debug.LogWarning($"[MenuPreferences] Could not save local preferences: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogWarning($"[MenuPreferences] Local preferences are not writable: {exception.Message}");
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning($"[MenuPreferences] Local preferences could not be saved: {exception.Message}");
        }
    }

    private static string GetPreferencesPath()
    {
        return Path.Combine(Application.persistentDataPath, PreferencesFileName);
    }

    private static float ClampFinite(float value, float fallback, float minimum, float maximum)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return fallback;
        }

        return Mathf.Clamp(value, minimum, maximum);
    }

    private static bool TryClampQualityLevel(int value, out int qualityLevel)
    {
        int qualityCount = QualitySettings.names.Length;
        qualityLevel = qualityCount == 0 ? 0 : Mathf.Clamp(value, 0, qualityCount - 1);
        return qualityCount > 0;
    }

    private static bool IsSupportedResolution(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        foreach (Resolution resolution in Screen.resolutions)
        {
            if (resolution.width == width && resolution.height == height)
            {
                return true;
            }
        }

        return width == Screen.width && height == Screen.height;
    }
}
