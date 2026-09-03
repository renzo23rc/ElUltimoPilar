using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Synthesizes procedural sound effects from domain and application events.
/// </summary>
public class AudioAdapter : MonoBehaviour
{
    /// <summary>
    /// Identifies the procedural sound effects supported by the adapter.
    /// </summary>
    public enum Sfx
    {
        Fire,
        Hit,
        Death,
        Explosion,
        Wave,
        Victory,
        Defeat,
        Heal,
        Ability,
        Variant
    }

    private const float DefaultVolume = 0.5f;
    private const int SampleRateHz = 44100;
    private const float WaveformPhaseRadians = 2f;
    private const float NoiseRange = 2.0f;
    private const float ArpeggioAmplitude = 0.6f;
    private const string GeneratedToneName = "tono";
    private const string GeneratedNoiseName = "ruido";
    private const string GeneratedArpeggioName = "arpegio";
    private const int NoiseSeed = 1234;
    private const float FireStartFrequencyHz = 880f;
    private const float FireEndFrequencyHz = 660f;
    private const float FireDurationSeconds = 0.09f;
    private const float FireAmplitude = 0.6f;
    private const float HitDurationSeconds = 0.07f;
    private const float HitAmplitude = 0.7f;
    private const float DeathStartFrequencyHz = 320f;
    private const float DeathEndFrequencyHz = 70f;
    private const float DeathDurationSeconds = 0.32f;
    private const float DeathAmplitude = 0.8f;
    private const float ExplosionDurationSeconds = 0.35f;
    private const float ExplosionAmplitude = 0.9f;
    private const float WaveStartFrequencyHz = 520f;
    private const float WaveEndFrequencyHz = 780f;
    private const float WaveDurationSeconds = 0.18f;
    private const float WaveAmplitude = 0.6f;
    private const float VictoryNoteDurationSeconds = 0.14f;
    private const float DefeatStartFrequencyHz = 220f;
    private const float DefeatEndFrequencyHz = 110f;
    private const float DefeatDurationSeconds = 0.6f;
    private const float DefeatAmplitude = 0.7f;
    private const float HealStartFrequencyHz = 440f;
    private const float HealEndFrequencyHz = 880f;
    private const float HealDurationSeconds = 0.2f;
    private const float HealAmplitude = 0.5f;
    private const float AbilityStartFrequencyHz = 990f;
    private const float AbilityEndFrequencyHz = 440f;
    private const float AbilityDurationSeconds = 0.16f;
    private const float AbilityAmplitude = 0.6f;
    private const float VariantNoteDurationSeconds = 0.12f;
    private static readonly float[] VictoryFrequenciesHz = { 523f, 659f, 784f };
    private static readonly float[] VariantFrequenciesHz = { 660f, 880f };

    [Header("Configuración")]
    [Range(0f, 1f)] public float volumen = DefaultVolume;

    private AudioSource fuente;
    private GameManager managerSuscrito;
    private readonly HashSet<WeaponSystem> armasSuscritas = new HashSet<WeaponSystem>();
    private readonly HashSet<EnergySystem> energiasSuscritas = new HashSet<EnergySystem>();
    private readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
    private static AudioAdapter instancia;

    /// <summary>
    /// Plays a sound effect through the shared audio adapter.
    /// </summary>
    /// <param name="efecto">The sound effect to play.</param>
    public static void Play(Sfx efecto)
    {
        if (instancia == null)
        {
            GameObject gameObject = new GameObject(nameof(AudioAdapter));
            instancia = gameObject.AddComponent<AudioAdapter>();
            DontDestroyOnLoad(gameObject);
        }

        instancia.Reproducir(efecto);
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        GenerarClips();

        CombatFeedback.OnCombatHit += ManejarImpacto;
        GameManager manager = FindFirstObjectByType<GameManager>();
        SuscribirGameManager(manager);
    }

    private void OnDestroy()
    {
        CombatFeedback.OnCombatHit -= ManejarImpacto;
        DesuscribirGameManager();
        if (instancia == this)
        {
            instancia = null;
        }
    }

    private void SuscribirGameManager(GameManager manager)
    {
        if (manager == null || managerSuscrito == manager)
        {
            return;
        }

        DesuscribirGameManager();
        managerSuscrito = manager;
        managerSuscrito.OnOleadaIniciada += ManejarOleada;
        managerSuscrito.OnVictoria += ManejarVictoria;
        managerSuscrito.OnDerrota += ManejarDerrota;
        managerSuscrito.OnPlayerRegistered += ManejarJugadorRegistrado;
        managerSuscrito.OnPlayerUnregistered += ManejarJugadorDesregistrado;
        foreach (PlayerController jugador in managerSuscrito.Players)
        {
            SuscribirJugador(jugador);
        }
    }

    private void DesuscribirGameManager()
    {
        if (managerSuscrito == null)
        {
            return;
        }

        managerSuscrito.OnOleadaIniciada -= ManejarOleada;
        managerSuscrito.OnVictoria -= ManejarVictoria;
        managerSuscrito.OnDerrota -= ManejarDerrota;
        managerSuscrito.OnPlayerRegistered -= ManejarJugadorRegistrado;
        managerSuscrito.OnPlayerUnregistered -= ManejarJugadorDesregistrado;
        foreach (WeaponSystem arma in armasSuscritas)
        {
            if (arma != null)
            {
                arma.OnDisparo -= ManejarDisparo;
            }
        }

        foreach (EnergySystem energia in energiasSuscritas)
        {
            if (energia == null)
            {
                continue;
            }

            energia.OnHabilidadActivada -= ManejarHabilidad;
            energia.OnCuracionUsada -= ManejarCuracion;
        }

        armasSuscritas.Clear();
        energiasSuscritas.Clear();
        managerSuscrito = null;
    }

    private void ManejarJugadorRegistrado(PlayerController jugador)
    {
        SuscribirJugador(jugador);
    }

    private void ManejarJugadorDesregistrado(PlayerController jugador)
    {
        if (jugador == null)
        {
            return;
        }

        WeaponSystem arma = jugador.GetComponent<WeaponSystem>();
        if (arma != null && armasSuscritas.Remove(arma))
        {
            arma.OnDisparo -= ManejarDisparo;
        }

        EnergySystem energia = jugador.GetComponent<EnergySystem>();
        if (energia != null && energiasSuscritas.Remove(energia))
        {
            energia.OnHabilidadActivada -= ManejarHabilidad;
            energia.OnCuracionUsada -= ManejarCuracion;
        }
    }

    private void SuscribirJugador(PlayerController jugador)
    {
        if (jugador == null)
        {
            return;
        }

        WeaponSystem arma = jugador.GetComponent<WeaponSystem>();
        if (arma != null && armasSuscritas.Add(arma))
        {
            arma.OnDisparo += ManejarDisparo;
        }

        EnergySystem energia = jugador.GetComponent<EnergySystem>();
        if (energia != null && energiasSuscritas.Add(energia))
        {
            energia.OnHabilidadActivada += ManejarHabilidad;
            energia.OnCuracionUsada += ManejarCuracion;
        }
    }

    private void ManejarImpacto(bool mato)
    {
        Reproducir(mato ? Sfx.Death : Sfx.Hit);
    }

    private void ManejarOleada(int oleada)
    {
        Reproducir(Sfx.Wave);
    }

    private void ManejarVictoria()
    {
        Reproducir(Sfx.Victory);
    }

    private void ManejarDerrota()
    {
        Reproducir(Sfx.Defeat);
    }

    private void ManejarDisparo(WeaponSystem.Arma arma)
    {
        Reproducir(arma != null && arma.tipo == WeaponSystem.TipoArma.Area ? Sfx.Explosion : Sfx.Fire);
    }

    private void ManejarHabilidad()
    {
        Reproducir(Sfx.Ability);
    }

    private void ManejarCuracion()
    {
        Reproducir(Sfx.Heal);
    }

    private void Reproducir(Sfx efecto)
    {
        if (fuente == null || !clips.TryGetValue(efecto, out AudioClip clip) || clip == null)
        {
            return;
        }

        fuente.PlayOneShot(clip, volumen);
    }

    private void GenerarClips()
    {
        clips[Sfx.Fire] = CrearTono(
            FireStartFrequencyHz,
            FireEndFrequencyHz,
            FireDurationSeconds,
            FireAmplitude,
            false);
        clips[Sfx.Hit] = CrearRuido(HitDurationSeconds, HitAmplitude);
        clips[Sfx.Death] = CrearTono(
            DeathStartFrequencyHz,
            DeathEndFrequencyHz,
            DeathDurationSeconds,
            DeathAmplitude,
            false);
        clips[Sfx.Explosion] = CrearRuido(ExplosionDurationSeconds, ExplosionAmplitude);
        clips[Sfx.Wave] = CrearTono(
            WaveStartFrequencyHz,
            WaveEndFrequencyHz,
            WaveDurationSeconds,
            WaveAmplitude,
            false);
        clips[Sfx.Victory] = CrearArpegio(VictoryFrequenciesHz, VictoryNoteDurationSeconds);
        clips[Sfx.Defeat] = CrearTono(
            DefeatStartFrequencyHz,
            DefeatEndFrequencyHz,
            DefeatDurationSeconds,
            DefeatAmplitude,
            false);
        clips[Sfx.Heal] = CrearTono(
            HealStartFrequencyHz,
            HealEndFrequencyHz,
            HealDurationSeconds,
            HealAmplitude,
            false);
        clips[Sfx.Ability] = CrearTono(
            AbilityStartFrequencyHz,
            AbilityEndFrequencyHz,
            AbilityDurationSeconds,
            AbilityAmplitude,
            true);
        clips[Sfx.Variant] = CrearArpegio(VariantFrequenciesHz, VariantNoteDurationSeconds);
    }

    private AudioClip CrearTono(
        float frecuenciaInicial,
        float frecuenciaFinal,
        float duracion,
        float amplitud,
        bool cuadrada)
    {
        int muestras = Mathf.Max(1, Mathf.RoundToInt(SampleRateHz * duracion));
        float[] datos = new float[muestras];
        float fase = 0f;
        for (int i = 0; i < muestras; i++)
        {
            float p = (float)i / muestras;
            float frecuencia = Mathf.Lerp(frecuenciaInicial, frecuenciaFinal, p);
            fase += WaveformPhaseRadians * Mathf.PI * frecuencia / SampleRateHz;
            float onda = cuadrada ? Mathf.Sign(Mathf.Sin(fase)) : Mathf.Sin(fase);
            float envolvente = Mathf.Sin(Mathf.PI * p);
            datos[i] = onda * envolvente * amplitud;
        }

        AudioClip clip = AudioClip.Create(GeneratedToneName, muestras, 1, SampleRateHz, false);
        clip.SetData(datos, 0);
        return clip;
    }

    private AudioClip CrearRuido(float duracion, float amplitud)
    {
        int muestras = Mathf.Max(1, Mathf.RoundToInt(SampleRateHz * duracion));
        float[] datos = new float[muestras];
        System.Random azar = new System.Random(NoiseSeed);
        for (int i = 0; i < muestras; i++)
        {
            float p = (float)i / muestras;
            float envolvente = 1f - p;
            datos[i] = (float)(azar.NextDouble() * NoiseRange - 1.0f) * envolvente * amplitud;
        }

        AudioClip clip = AudioClip.Create(GeneratedNoiseName, muestras, 1, SampleRateHz, false);
        clip.SetData(datos, 0);
        return clip;
    }

    private AudioClip CrearArpegio(float[] frecuencias, float duracionNota)
    {
        int porNota = Mathf.Max(1, Mathf.RoundToInt(SampleRateHz * duracionNota));
        float[] datos = new float[porNota * frecuencias.Length];
        for (int n = 0; n < frecuencias.Length; n++)
        {
            float fase = 0f;
            for (int i = 0; i < porNota; i++)
            {
                float p = (float)i / porNota;
                fase += WaveformPhaseRadians * Mathf.PI * frecuencias[n] / SampleRateHz;
                datos[n * porNota + i] = Mathf.Sin(fase) * Mathf.Sin(Mathf.PI * p) * ArpeggioAmplitude;
            }
        }

        AudioClip clip = AudioClip.Create(GeneratedArpeggioName, datos.Length, 1, SampleRateHz, false);
        clip.SetData(datos, 0);
        return clip;
    }
}
