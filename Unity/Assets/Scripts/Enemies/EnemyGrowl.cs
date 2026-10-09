using UnityEngine;

/// <summary>
/// Plays a spatial growl at random intervals while its <see cref="Enemy"/> is alive.
/// The growl depends on the enemy type and the clips are loaded from Resources.
/// </summary>
public class EnemyGrowl : MonoBehaviour
{
    private const string GrowlResourcePrefix = "Audio/Enemigos/growl_enemigo";
    private const int RunnerGrowl = 1;
    private const int ArtilleryGrowl = 2;
    private const int ColossusGrowl = 3;
    private const int ExplosiveGrowl = 4;
    private const int DefaultGrowl = 5;
    private const float MinInitialDelaySeconds = 1f;
    private const float MaxInitialDelaySeconds = 4f;
    private const float MinIntervalSeconds = 6f;
    private const float MaxIntervalSeconds = 14f;
    private const float MinGapBetweenGrowlsSeconds = 0.35f;
    private const float GrowlVolume = 0.6f;
    private const float MaxHearingDistanceMeters = 35f;
    private const float FullSpatialBlend = 1f;

    private static float nextAllowedGrowlTime;

    private Enemy enemy;
    private AudioSource source;
    private float timerSeconds;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetThrottle()
    {
        nextAllowedGrowlTime = 0f;
    }

    private void Start()
    {
        enemy = GetComponent<Enemy>();
        AudioClip clip = Resources.Load<AudioClip>($"{GrowlResourcePrefix}{SelectGrowlIndex(enemy)}");
        if (enemy == null || clip == null)
        {
            enabled = false;
            return;
        }

        source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.playOnAwake = false;
        source.loop = false;
        source.volume = GrowlVolume;
        source.spatialBlend = FullSpatialBlend;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.maxDistance = MaxHearingDistanceMeters;
        timerSeconds = Random.Range(MinInitialDelaySeconds, MaxInitialDelaySeconds);
    }

    private void Update()
    {
        if (enemy.EstaMuerto)
        {
            return;
        }

        timerSeconds -= Time.deltaTime;
        if (timerSeconds > 0f)
        {
            return;
        }

        timerSeconds = Random.Range(MinIntervalSeconds, MaxIntervalSeconds);
        if (Time.time < nextAllowedGrowlTime)
        {
            return;
        }

        nextAllowedGrowlTime = Time.time + MinGapBetweenGrowlsSeconds;
        source.Play();
    }

    private static int SelectGrowlIndex(Enemy enemy)
    {
        switch (enemy)
        {
            case Runner _:
                return RunnerGrowl;
            case Artillery _:
                return ArtilleryGrowl;
            case Colossus _:
                return ColossusGrowl;
            case Explosive _:
                return ExplosiveGrowl;
            default:
                return DefaultGrowl;
        }
    }
}
