using UnityEngine;

/// <summary>
/// Loops the soundtrack of the Pilar's current phase and fades between tracks when the phase changes.
/// The tracks are loaded from Resources.
/// </summary>
public class PhaseMusicPlayer : MonoBehaviour
{
    private const string MusicResourcePrefix = "Audio/Musica/phase";
    private const int FirstPhase = 1;
    private const int LastPhase = 4;
    private const float MusicVolume = 0.4f;
    private const float FadeSeconds = 1.2f;

    private AudioSource source;
    private Pilar pilar;
    private int playingPhase;
    private int requestedPhase;

    private void Start()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        pilar = FindFirstObjectByType<Pilar>();
    }

    private void Update()
    {
        if (pilar == null)
        {
            pilar = FindFirstObjectByType<Pilar>();
            return;
        }

        requestedPhase = Mathf.Clamp(pilar.faseActual, FirstPhase, LastPhase);
        float step = MusicVolume / FadeSeconds * Time.unscaledDeltaTime;
        if (requestedPhase != playingPhase)
        {
            source.volume = Mathf.MoveTowards(source.volume, 0f, step);
            if (source.volume <= 0f)
            {
                StartTrack(requestedPhase);
            }

            return;
        }

        source.volume = Mathf.MoveTowards(source.volume, MusicVolume, step);
    }

    private void StartTrack(int phase)
    {
        playingPhase = phase;
        source.clip = Resources.Load<AudioClip>($"{MusicResourcePrefix}{phase}");
        if (source.clip != null)
        {
            source.Play();
        }
    }
}
