/**
 * PozoKill.cs
 * Trampa mortal del pozo central (Fase 2).
 * Detecta caída de jugadores y del Coloso; al Coloso lo mata con recompensa
 * y al jugador lo derriba con animación de caída.
 *
 * Colocar en el GameObject "PozoCentral" creado por TestSceneSetup / ArenaTransform.
 * Requiere Collider isTrigger y opcional Rigidbody kinematic para detectar CharacterController.
 */
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PozoKill : MonoBehaviour
{
    private const float PlayerFallBelowWorldY = -5f;
    private const float TriggerFallBelowWorldY = -2f;
    private const float InstakillDamage = 9999f;
    private const float GizmoFillAlpha = 0.2f;
    private const float GizmoHeightOffset = 1f;
    private const float GizmoExtraHeightMeters = 2f;

    [Header("Configuración")]
    public float radioMortal = 4.5f; // Radio en XZ (pozo escala 3 => radio 1.5, ampliado para caída)
    public float alturaMortal = 2f; // Y relativo al centro del pozo, si entity y < centro.y + altura => muere
    public bool mataJugador = true;
    public bool mataEnemigos = true;
    public float dañoInstakill = InstakillDamage;

    private readonly List<Colossus> colosos = new List<Colossus>();

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        // Rigidbody kinematic necesario para que OnTriggerEnter funcione con CharacterController
        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        TryKill(other);
    }

    void OnTriggerStay(Collider other)
    {
        // Por si el objetivo entra rápido o aparece dentro.
        TryKill(other);
    }

    // Polling para CharacterController (no siempre genera trigger) y para Colosos empujados.
    void Update()
    {
        if (mataJugador)
            RevisarJugadores();
        if (mataEnemigos)
            RevisarColosos();
    }

    void RevisarJugadores()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null) return;

        foreach (PlayerController player in manager.Players)
        {
            if (player == null || player.estaDerribado) continue;
            bool caidaLibre = player.transform.position.y < PlayerFallBelowWorldY;
            if (EstaDentroDelPozo(player.transform.position) || caidaLibre)
                MatarJugador(player);
        }
    }

    void RevisarColosos()
    {
        // Se copia primero: matar a un Coloso lo quita del registro de enemigos activos.
        colosos.Clear();
        foreach (Enemy enemy in Enemy.Active)
        {
            if (enemy is Colossus colossus && !colossus.EstaMuerto && EstaDentroDelPozo(colossus.transform.position))
                colosos.Add(colossus);
        }

        foreach (Colossus colossus in colosos)
            MatarColoso(colossus);
    }

    bool EstaDentroDelPozo(Vector3 posicion)
    {
        Vector2 offsetXZ = new Vector2(posicion.x - transform.position.x, posicion.z - transform.position.z);
        return offsetXZ.magnitude <= radioMortal && EstaBajoAlturaMortal(posicion);
    }

    bool EstaBajoAlturaMortal(Vector3 posicion)
    {
        return posicion.y <= transform.position.y + alturaMortal;
    }

    void TryKill(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            // Verificar altura también para evitar kill si el jugador pasa por encima (puentes).
            bool caido = EstaBajoAlturaMortal(player.transform.position) || player.transform.position.y < TriggerFallBelowWorldY;
            if (mataJugador && caido)
                MatarJugador(player);
            return;
        }

        // El pozo solo mata al Coloso: corredores y artilleros sobreviven al pasar por el borde.
        Colossus colossus = other.GetComponentInParent<Colossus>();
        if (mataEnemigos && colossus != null && EstaBajoAlturaMortal(colossus.transform.position))
            MatarColoso(colossus);
    }

    void MatarJugador(PlayerController player)
    {
        if (player.estaDerribado) return;
        player.CaerEnPozo(transform.position);
    }

    void MatarColoso(Colossus colossus)
    {
        if (colossus == null || colossus.EstaMuerto) return;
        Debug.Log($"[PozoKill] ¡Coloso cayó al pozo! Muerte instantánea + recompensa {colossus.energiaDrop}");
        // Ignora la resistencia del Coloso y conserva la recompensa normal de muerte.
        colossus.KillInstantly();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioMortal);
        Vector3 alturaMortalPos = transform.position + Vector3.up * alturaMortal;
        Gizmos.DrawLine(alturaMortalPos, alturaMortalPos + Vector3.forward * radioMortal);
        Gizmos.color = new Color(1f, 0f, 0f, GizmoFillAlpha);
        Gizmos.DrawCube(
            transform.position + Vector3.up * (alturaMortal * 0.5f - GizmoHeightOffset),
            new Vector3(radioMortal * 2f, alturaMortal + GizmoExtraHeightMeters, radioMortal * 2f));
    }
}
