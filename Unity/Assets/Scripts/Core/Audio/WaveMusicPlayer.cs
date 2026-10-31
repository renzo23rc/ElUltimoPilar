using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Plays the current map's wave sting once when a wave starts. The phase soundtrack covers the rest of the match.
/// </summary>
public sealed class WaveMusicPlayer : MonoBehaviour
{
    private const string EstatuaSceneName = "EstatuaDeLaLibertad";
    private const string ObeliscoSceneName = "Obelisco";
    private const string CristoSceneName = "CristoRedentor";
    private const string EstatuaResource = "Audio/Musica/wave_eeuu";
    private const string ObeliscoResource = "Audio/Musica/wave_bsas";
    private const string CristoResource = "Audio/Musica/wave_brasil";
    private const float MusicVolume = 0.4f;

    private AudioSource source;
    private GameManager manager;
    private string loadedResource;

    /// <summary>Gets the Resources path of the wave track for a match scene, or null when the scene has none.</summary>
    public static string ResourcePathForScene(string sceneName)
    {
        switch (sceneName)
        {
            case EstatuaSceneName:
                return EstatuaResource;
            case ObeliscoSceneName:
                return ObeliscoResource;
            case CristoSceneName:
                return CristoResource;
            default:
                return null;
        }
    }

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = MusicVolume * MenuPreferences.MusicVolume;
    }

    private void Update()
    {
        SyncManager();
        SyncTrack();
        source.volume = MusicVolume * MenuPreferences.MusicVolume;
    }

    private void OnDestroy()
    {
        UnsubscribeManager();
    }

    private void SyncManager()
    {
        GameManager current = GameManager.Instance;
        if (current == null)
        {
            current = FindFirstObjectByType<GameManager>();
        }

        if (ReferenceEquals(current, manager))
        {
            return;
        }

        UnsubscribeManager();
        manager = current;
        SubscribeManager();
    }

    private void SubscribeManager()
    {
        if (manager == null)
        {
            return;
        }

        manager.OnOleadaIniciada += HandleWaveStarted;
        manager.OnVictoria += HandleMatchEnded;
        manager.OnDerrota += HandleMatchEnded;
    }

    private void UnsubscribeManager()
    {
        if (manager == null)
        {
            return;
        }

        manager.OnOleadaIniciada -= HandleWaveStarted;
        manager.OnVictoria -= HandleMatchEnded;
        manager.OnDerrota -= HandleMatchEnded;
        manager = null;
    }

    private void HandleWaveStarted(int wave)
    {
        SyncTrack();
        if (source.clip == null)
        {
            return;
        }

        source.Stop();
        source.time = 0f;
        source.Play();
    }

    private void HandleMatchEnded()
    {
        source.Stop();
    }

    private void SyncTrack()
    {
        string resource = ResourcePathForScene(SceneManager.GetActiveScene().name);
        if (resource == loadedResource)
        {
            return;
        }

        loadedResource = resource;
        source.Stop();
        source.clip = string.IsNullOrEmpty(resource) ? null : Resources.Load<AudioClip>(resource);
    }
}
