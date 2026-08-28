/**
 * PozoKill.cs
 * Trampa mortal del pozo central (Fase 2).
 * Detecta caída de enemigos (incluye Coloso) y jugador, aplica instakill
 * con recompensa y actualiza contador de oleada vía EnemySpawner.
 *
 * Colocar en el GameObject "PozoCentral" creado por TestSceneSetup / ArenaTransform.
 * Requiere Collider isTrigger y opcional Rigidbody kinematic para detectar CharacterController.
 */
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PozoKill : MonoBehaviour
{
    [Header("Configuración")]
    public float radioMortal = 4.5f; // Radio en XZ (pozo escala 3 => radio 1.5, ampliado para caída)
    public float alturaMortal = 2f; // Y relativo al centro del pozo, si entity y < centro.y + altura => muere
    public bool mataJugador = true;
    public bool mataEnemigos = true;
    public float dañoInstakill = 9999f;

    private Collider col;

    void Awake()
    {
        col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        // Rigidbody kinematic necesario para que OnTriggerEnter funcione con CharacterController
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        TryKill(other.gameObject);
    }

    void OnTriggerStay(Collider other)
    {
        // Por si el enemigo entra rápido o spawnea dentro
        TryKill(other.gameObject);
    }

    // Polling para CharacterController (no siempre genera trigger) y para caída por gravedad
    void Update()
    {
        if (!mataJugador && !mataEnemigos) return;

        // Check jugadores por distancia (CharacterController alternative)
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p == null || p.estaDerribado) continue;
            Vector3 planoPozo = new Vector3(transform.position.x, p.transform.position.y, transform.position.z);
            float distXZ = Vector3.Distance(new Vector3(p.transform.position.x, 0, p.transform.position.z),
                                            new Vector3(transform.position.x, 0, transform.position.z));
            bool dentroRadio = distXZ <= radioMortal;
            bool bajoAltura = p.transform.position.y <= transform.position.y + alturaMortal;
            // También si cae por debajo del mundo
            bool caidaLibre = p.transform.position.y < -5f;
            if ((dentroRadio && bajoAltura) || caidaLibre)
            {
                if (mataJugador)
                    MatarJugador(p);
            }
        }

        // Check enemigos por OverlapSphere (robusto para Rigidbody enemies) - solo Coloso
        if (mataEnemigos)
        {
            Collider[] enPozo = Physics.OverlapSphere(transform.position, radioMortal);
            foreach (var c in enPozo)
            {
                if (c.gameObject == this.gameObject) continue;
                // Solo si está bajo la altura mortal
                if (c.transform.position.y > transform.position.y + alturaMortal) continue;
                var e = c.GetComponent<Enemy>() ?? c.GetComponentInParent<Enemy>();
                if (e is Colossus) TryKill(c.gameObject);
            }
        }
    }

    void TryKill(GameObject go)
    {
        // Prioridad: Player
        var player = go.GetComponent<PlayerController>();
        if (player == null) player = go.GetComponentInParent<PlayerController>();
        if (player != null && mataJugador)
        {
            // Verificar altura también para evitar kill si player pasa por encima (puentes)
            if (player.transform.position.y <= transform.position.y + alturaMortal || player.transform.position.y < -2f)
            {
                MatarJugador(player);
                return;
            }
        }

        var enemy = go.GetComponent<Enemy>();
        if (enemy == null) enemy = go.GetComponentInParent<Enemy>();
        if (enemy != null && mataEnemigos)
        {
            // Solo matar si realmente cayó (y bajo altura mortal) - evita matar corredores que caminan sobre el borde
            if (enemy.transform.position.y > transform.position.y + alturaMortal) return;
            // Pozo solo debe matar Coloso (mini-jefe) - corredores/artilleros deben sobrevivir al pasar por borde
            if (!(enemy is Colossus))
            {
                // Opcional: log para debug pero no matar
                // Debug.Log($"[PozoKill] {enemy.name} sobre pozo pero no es Coloso - ignorado");
                return;
            }
            MatarEnemigo(enemy);
        }
    }

    void MatarJugador(PlayerController player)
    {
        if (player.estaDerribado) return;
        Debug.Log($"[PozoKill] Jugador {player.name} cayó al pozo! Instakill + derribado");
        // Efecto caída: empujar hacia abajo rápido si tiene CharacterController
        player.CaerEnPozo(transform.position);
    }

    void MatarEnemigo(Enemy enemy)
    {
        if (enemy == null) return;
        // Evitar doble kill si ya muerto
        var campo = enemy.GetType().GetField("estaMuerto", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        // Si ya está muerto, ignorar
        try
        {
            if (enemy is Colossus col)
            {
                Debug.Log($"[PozoKill] ¡Coloso cayó al pozo! Muerte instantánea + recompensa {col.energiaDrop}");
            }
            else
            {
                Debug.Log($"[PozoKill] {enemy.name} cayó al pozo");
            }
        }
        catch { }

        // Daño instakill que ignora resistencias (para Coloso que reduce 80%)
        // Usamos 9999 directo via RecibirDaño, pero Coloso lo reduce; añadimos bypass llamando a método interno si existe
        if (enemy is Colossus)
        {
            // Bypass resistencia: directo a vida 0 y Morir
            enemy.vidaActual = 0;
            // Invocar Morir protegido via reflection o via daño masivo que supere resistencia
            // Damos 5000*5 para asegurar muerte aun con 0.2 factor
            enemy.RecibirDaño(5000f);
            if (enemy.vidaActual > 0)
            {
                // Fallback: destruir directo y notificar spawner
                EnemySpawner.Instance?.EnemigoEliminado(enemy);
                if (enemy.TryGetComponent<Collider>(out var c)) c.enabled = false;
                Destroy(enemy.gameObject);
            }
        }
        else
        {
            enemy.RecibirDaño(dañoInstakill);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        // Cilindro wire: radio y altura mortal
        Gizmos.DrawWireSphere(transform.position, radioMortal);
        Gizmos.DrawLine(transform.position + Vector3.up * alturaMortal, transform.position + Vector3.up * alturaMortal + Vector3.forward * radioMortal);
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawCube(transform.position + Vector3.up * (alturaMortal * 0.5f - 1f), new Vector3(radioMortal * 2, alturaMortal + 2f, radioMortal * 2));
    }
}
