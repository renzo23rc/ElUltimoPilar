/**
 * WeaverZone.cs
 * Zona de efecto del Tejedor. Ralentiza mientras se está dentro y aplica daño por segundo.
 * Cada zona es una fuente de ralentización propia: salir de una no cancela otras.
 */
using System.Collections.Generic;
using UltimoPilar.Core.Shared;
using UnityEngine;

public class WeaverZone : MonoBehaviour
{
    private const float PresencePollIntervalSeconds = 0.15f;
    private const float DamagePulseIntervalSeconds = 1f;
    private const float DestroyGraceSeconds = 0.5f;
    private const float PilarDamageMultiplier = 0.5f;
    private const float RadiusFromScaleRatio = 0.5f;
    private static readonly Color GizmoColor = new Color(0.6f, 0.2f, 1f, 0.3f);

    public float dañoPorSegundo = 3f;
    public float factorRalentizacion = 0.5f;
    public float duracion = 8f;

    private float timer = 0f;
    private float pollTimer = 0f;
    private float damagePulseTimer = DamagePulseIntervalSeconds;
    private readonly HashSet<PlayerController> playersRalentizados = new HashSet<PlayerController>();
    private readonly HashSet<Enemy> enemigosRalentizados = new HashSet<Enemy>();

    private float Radio => transform.localScale.x * RadiusFromScaleRatio;

    void Start()
    {
        timer = duracion;
        Destroy(gameObject, duracion + DestroyGraceSeconds);
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            RestaurarTodos();
            return;
        }

        pollTimer -= Time.deltaTime;
        if (pollTimer <= 0f)
        {
            pollTimer = PresencePollIntervalSeconds;
            ActualizarRalentizacion();
        }

        damagePulseTimer -= Time.deltaTime;
        if (damagePulseTimer <= 0f)
        {
            damagePulseTimer += DamagePulseIntervalSeconds;
            AplicarDañoEnArea();
        }
    }

    void ActualizarRalentizacion()
    {
        var playersDentro = new HashSet<PlayerController>(OverlapQuery.FindUniqueInSphere<PlayerController>(transform.position, Radio));
        var enemigosDentro = new HashSet<Enemy>();
        foreach (Enemy enemy in OverlapQuery.FindUniqueInSphere<Enemy>(transform.position, Radio))
        {
            // El Nido es estacionario: la ralentización no le afecta.
            if (!(enemy is Nest))
            {
                enemigosDentro.Add(enemy);
            }
        }

        SincronizarRalentizados(playersRalentizados, playersDentro);
        SincronizarRalentizados(enemigosRalentizados, enemigosDentro);
    }

    // Ralentiza a quien entró y libera a quien salió; jugadores y enemigos comparten la regla.
    void SincronizarRalentizados<T>(HashSet<T> ralentizados, HashSet<T> dentro) where T : Component, ISlowable
    {
        foreach (T objetivo in dentro)
        {
            if (ralentizados.Add(objetivo))
            {
                objetivo.AplicarRalentizacion(this, factorRalentizacion, timer);
            }
        }

        ralentizados.RemoveWhere(objetivo => ReleaseIfOutside(objetivo, dentro));
    }

    bool ReleaseIfOutside<T>(T objetivo, HashSet<T> dentro) where T : Component, ISlowable
    {
        if (objetivo != null && dentro.Contains(objetivo))
        {
            return false;
        }

        if (objetivo != null)
        {
            objetivo.QuitarRalentizacion(this);
        }

        return true;
    }

    void RestaurarTodos()
    {
        Liberar(playersRalentizados);
        Liberar(enemigosRalentizados);
    }

    void Liberar<T>(HashSet<T> ralentizados) where T : Component, ISlowable
    {
        foreach (T objetivo in ralentizados)
        {
            if (objetivo != null)
            {
                objetivo.QuitarRalentizacion(this);
            }
        }

        ralentizados.Clear();
    }

    void OnDestroy()
    {
        RestaurarTodos();
    }

    void AplicarDañoEnArea()
    {
        foreach (PlayerController player in OverlapQuery.FindUniqueInSphere<PlayerController>(transform.position, Radio))
        {
            player.RecibirDaño(dañoPorSegundo);
        }

        foreach (Pilar pilar in OverlapQuery.FindUniqueInSphere<Pilar>(transform.position, Radio))
        {
            pilar.RecibirDaño(dañoPorSegundo * PilarDamageMultiplier);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = GizmoColor;
        Gizmos.DrawWireSphere(transform.position, Radio);
    }
}
