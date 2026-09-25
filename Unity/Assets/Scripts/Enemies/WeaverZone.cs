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

    public float dañoPorSegundo = 5f;
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

        foreach (PlayerController player in playersDentro)
        {
            if (playersRalentizados.Add(player))
            {
                player.AplicarRalentizacion(this, factorRalentizacion, timer);
            }
        }

        foreach (Enemy enemy in enemigosDentro)
        {
            if (enemigosRalentizados.Add(enemy))
            {
                enemy.AplicarRalentizacion(this, factorRalentizacion, timer);
            }
        }

        playersRalentizados.RemoveWhere(player => ReleaseIfOutside(player, playersDentro));
        enemigosRalentizados.RemoveWhere(enemy => ReleaseIfOutside(enemy, enemigosDentro));
    }

    bool ReleaseIfOutside(PlayerController player, HashSet<PlayerController> inside)
    {
        if (player != null && inside.Contains(player))
        {
            return false;
        }

        if (player != null)
        {
            player.QuitarRalentizacion(this);
        }

        return true;
    }

    bool ReleaseIfOutside(Enemy enemy, HashSet<Enemy> inside)
    {
        if (enemy != null && inside.Contains(enemy))
        {
            return false;
        }

        if (enemy != null)
        {
            enemy.QuitarRalentizacion(this);
        }

        return true;
    }

    void RestaurarTodos()
    {
        foreach (PlayerController player in playersRalentizados)
        {
            if (player != null)
            {
                player.QuitarRalentizacion(this);
            }
        }

        foreach (Enemy enemy in enemigosRalentizados)
        {
            if (enemy != null)
            {
                enemy.QuitarRalentizacion(this);
            }
        }

        playersRalentizados.Clear();
        enemigosRalentizados.Clear();
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
