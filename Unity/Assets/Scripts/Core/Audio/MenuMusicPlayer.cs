using UnityEngine;

/// <summary>Loops the menu soundtrack while a menu scene is loaded.</summary>
public sealed class MenuMusicPlayer : MonoBehaviour
{
    private const string MenuResource = "Audio/Musica/menu";
    private const float MusicVolume = 0.4f;

    private AudioSource source;

    /// <summary>Adds a player to the given host when one is not already present.</summary>
    public static void EnsureOn(GameObject host)
    {
        if (host != null && host.GetComponent<MenuMusicPlayer>() == null)
        {
            host.AddComponent<MenuMusicPlayer>();
        }
    }

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.clip = Resources.Load<AudioClip>(MenuResource);
        source.volume = MusicVolume * MenuPreferences.MusicVolume;
        if (source.clip != null)
        {
            source.Play();
        }
    }

    private void Update()
    {
        if (source != null)
        {
            source.volume = MusicVolume * MenuPreferences.MusicVolume;
        }
    }
}
