/**
 * AudioAdapter.cs
 * Audio reactivo sin assets: sintetiza efectos procedurales y los dispara
 * desde eventos de dominio (oleadas, resultado, impactos, disparos, energía).
 * No decide transiciones; solo consume eventos públicos.
 */
using UnityEngine;
using System;
using System.Collections.Generic;

public class AudioAdapter : MonoBehaviour
{
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

    [Header("Configuración")]
    [Range(0f, 1f)] public float volumen = 0.5f;

    private AudioSource fuente;
    private GameManager managerSuscrito;
    private readonly HashSet<WeaponSystem> armasSuscritas = new HashSet<WeaponSystem>();
    private readonly HashSet<EnergySystem> energiasSuscritas = new HashSet<EnergySystem>();
    private readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();

    private static AudioAdapter instancia;

    public static void Play(Sfx efecto)
    {
        if (instancia == null)
        {
            var go = new GameObject("AudioAdapter");
            instancia = go.AddComponent<AudioAdapter>();
            DontDestroyOnLoad(go);
        }
        instancia.Reproducir(efecto);
    }

    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        GenerarClips();

        CombatFeedback.OnCombatHit += ManejarImpacto;
        var manager = FindFirstObjectByType<GameManager>();
        SuscribirGameManager(manager);
    }

    void OnDestroy()
    {
        CombatFeedback.OnCombatHit -= ManejarImpacto;
        DesuscribirGameManager();
        if (instancia == this)
            instancia = null;
    }

    private void SuscribirGameManager(GameManager manager)
    {
        if (manager == null || managerSuscrito == manager) return;
        DesuscribirGameManager();
        managerSuscrito = manager;
        managerSuscrito.OnOleadaIniciada += ManejarOleada;
        managerSuscrito.OnVictoria += ManejarVictoria;
        managerSuscrito.OnDerrota += ManejarDerrota;
        managerSuscrito.OnPlayerRegistered += ManejarJugadorRegistrado;
        managerSuscrito.OnPlayerUnregistered += ManejarJugadorDesregistrado;
        foreach (var jugador in managerSuscrito.Players)
            SuscribirJugador(jugador);
    }

    private void DesuscribirGameManager()
    {
        if (managerSuscrito == null) return;
        managerSuscrito.OnOleadaIniciada -= ManejarOleada;
        managerSuscrito.OnVictoria -= ManejarVictoria;
        managerSuscrito.OnDerrota -= ManejarDerrota;
        managerSuscrito.OnPlayerRegistered -= ManejarJugadorRegistrado;
        managerSuscrito.OnPlayerUnregistered -= ManejarJugadorDesregistrado;
        foreach (var arma in armasSuscritas)
            if (arma != null) arma.OnDisparo -= ManejarDisparo;
        foreach (var energia in energiasSuscritas)
        {
            if (energia == null) continue;
            energia.OnHabilidadActivada -= ManejarHabilidad;
            energia.OnCuracionUsada -= ManejarCuracion;
        }
        armasSuscritas.Clear();
        energiasSuscritas.Clear();
        managerSuscrito = null;
    }

    private void ManejarJugadorRegistrado(PlayerController jugador) => SuscribirJugador(jugador);

    private void ManejarJugadorDesregistrado(PlayerController jugador)
    {
        if (jugador == null) return;
        var arma = jugador.GetComponent<WeaponSystem>();
        if (arma != null && armasSuscritas.Remove(arma))
            arma.OnDisparo -= ManejarDisparo;
        var energia = jugador.GetComponent<EnergySystem>();
        if (energia != null && energiasSuscritas.Remove(energia))
        {
            energia.OnHabilidadActivada -= ManejarHabilidad;
            energia.OnCuracionUsada -= ManejarCuracion;
        }
    }

    private void SuscribirJugador(PlayerController jugador)
    {
        if (jugador == null) return;
        var arma = jugador.GetComponent<WeaponSystem>();
        if (arma != null && armasSuscritas.Add(arma))
            arma.OnDisparo += ManejarDisparo;
        var energia = jugador.GetComponent<EnergySystem>();
        if (energia != null && energiasSuscritas.Add(energia))
        {
            energia.OnHabilidadActivada += ManejarHabilidad;
            energia.OnCuracionUsada += ManejarCuracion;
        }
    }

    private void ManejarImpacto(bool mato) => Reproducir(mato ? Sfx.Death : Sfx.Hit);
    private void ManejarOleada(int oleada) => Reproducir(Sfx.Wave);
    private void ManejarVictoria() => Reproducir(Sfx.Victory);
    private void ManejarDerrota() => Reproducir(Sfx.Defeat);
    private void ManejarDisparo(WeaponSystem.Arma arma) => Reproducir(arma != null && arma.tipo == WeaponSystem.TipoArma.Area ? Sfx.Explosion : Sfx.Fire);
    private void ManejarHabilidad() => Reproducir(Sfx.Ability);
    private void ManejarCuracion() => Reproducir(Sfx.Heal);

    private void Reproducir(Sfx efecto)
    {
        if (fuente == null || !clips.TryGetValue(efecto, out var clip) || clip == null) return;
        fuente.PlayOneShot(clip, volumen);
    }

    private void GenerarClips()
    {
        clips[Sfx.Fire] = CrearTono(880f, 660f, 0.09f, 0.6f, false);
        clips[Sfx.Hit] = CrearRuido(0.07f, 0.7f);
        clips[Sfx.Death] = CrearTono(320f, 70f, 0.32f, 0.8f, false);
        clips[Sfx.Explosion] = CrearRuido(0.35f, 0.9f);
        clips[Sfx.Wave] = CrearTono(520f, 780f, 0.18f, 0.6f, false);
        clips[Sfx.Victory] = CrearArpegio(new[] { 523f, 659f, 784f }, 0.14f);
        clips[Sfx.Defeat] = CrearTono(220f, 110f, 0.6f, 0.7f, false);
        clips[Sfx.Heal] = CrearTono(440f, 880f, 0.2f, 0.5f, false);
        clips[Sfx.Ability] = CrearTono(990f, 440f, 0.16f, 0.6f, true);
        clips[Sfx.Variant] = CrearArpegio(new[] { 660f, 880f }, 0.12f);
    }

    private AudioClip CrearTono(float frecuenciaInicial, float frecuenciaFinal, float duracion, float amplitud, bool cuadrada)
    {
        int muestras = Mathf.Max(1, Mathf.RoundToInt(44100f * duracion));
        float[] datos = new float[muestras];
        float fase = 0f;
        for (int i = 0; i < muestras; i++)
        {
            float p = (float)i / muestras;
            float frecuencia = Mathf.Lerp(frecuenciaInicial, frecuenciaFinal, p);
            fase += 2f * Mathf.PI * frecuencia / 44100f;
            float onda = cuadrada ? Mathf.Sign(Mathf.Sin(fase)) : Mathf.Sin(fase);
            float envolvente = Mathf.Sin(Mathf.PI * p);
            datos[i] = onda * envolvente * amplitud;
        }
        var clip = AudioClip.Create("tono", muestras, 1, 44100, false);
        clip.SetData(datos, 0);
        return clip;
    }

    private AudioClip CrearRuido(float duracion, float amplitud)
    {
        int muestras = Mathf.Max(1, Mathf.RoundToInt(44100f * duracion));
        float[] datos = new float[muestras];
        var azar = new System.Random(1234);
        for (int i = 0; i < muestras; i++)
        {
            float p = (float)i / muestras;
            float envolvente = 1f - p;
            datos[i] = (float)(azar.NextDouble() * 2.0 - 1.0) * envolvente * amplitud;
        }
        var clip = AudioClip.Create("ruido", muestras, 1, 44100, false);
        clip.SetData(datos, 0);
        return clip;
    }

    private AudioClip CrearArpegio(float[] frecuencias, float duracionNota)
    {
        int porNota = Mathf.Max(1, Mathf.RoundToInt(44100f * duracionNota));
        float[] datos = new float[porNota * frecuencias.Length];
        for (int n = 0; n < frecuencias.Length; n++)
        {
            float fase = 0f;
            for (int i = 0; i < porNota; i++)
            {
                float p = (float)i / porNota;
                fase += 2f * Mathf.PI * frecuencias[n] / 44100f;
                datos[n * porNota + i] = Mathf.Sin(fase) * Mathf.Sin(Mathf.PI * p) * 0.6f;
            }
        }
        var clip = AudioClip.Create("arpegio", datos.Length, 1, 44100, false);
        clip.SetData(datos, 0);
        return clip;
    }
}
