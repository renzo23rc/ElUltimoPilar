using UnityEngine;

/// <summary>Plays two-dimensional menu cursor clips from Resources.</summary>
public static class MenuSfx
{
    private const string SelectResource = "Audio/Fx/cursor_select";
    private const string BackResource = "Audio/Fx/cursor_back";
    private const string HostName = "MenuSfxHost";
    private const float Volume = 0.8f;

    private static AudioSource source;

    /// <summary>Plays the cursor-select clip.</summary>
    public static void PlaySelect()
    {
        Play(SelectResource);
    }

    /// <summary>Plays the cursor-back clip.</summary>
    public static void PlayBack()
    {
        Play(BackResource);
    }

    private static void Play(string resource)
    {
        AudioClip clip = Resources.Load<AudioClip>(resource);
        if (clip == null)
        {
            return;
        }

        EnsureSource().PlayOneShot(clip, Volume * MenuPreferences.EffectsVolume);
    }

    private static AudioSource EnsureSource()
    {
        if (source != null)
        {
            return source;
        }

        GameObject host = new GameObject(HostName);
        Object.DontDestroyOnLoad(host);
        source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }
}
