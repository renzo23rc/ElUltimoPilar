/**
 * Weaver.cs
 * Tejedor: Enemigo a distancia que no ataca directamente.
 * Lanza un campo que ralentiza al jugador y reduce visibilidad cerca del Pilar.
 * Aplica daño por segundo dentro del área.
 */
using UnityEngine;
using System.Collections.Generic;

public class Weaver : Enemy
{
private const float MovementSpeedMetersPerSecond = 1.2f;
private const float MaximumHealth = 35f;
private const int EnergyDropAmount = 4;
private const float NoDamage = 0f;
private const float RetreatRangeMultiplier = 0.6f;
private const float RotationSharpness = 3f;
private const float FieldHeightMeters = 0.1f;
    [Header("Tejedor Específico")]
    public GameObject prefabCampo;
    public float rangoLanzamiento = 18f;
    public float cooldownCampo = 5f;
    public float duracionCampo = 8f;
    public float radioCampo = 6f;
    public float dañoPorSegundo = 5f;
    public float factorRalentizacion = 0.5f;
    
    private float timerCampo = 0f;

    protected override void Start()
    {
        base.Start();
        atacaJugador = true;
        velocidadMovimiento = MovementSpeedMetersPerSecond;
        vidaMaxima = MaximumHealth;
        vidaActual = vidaMaxima;
        energiaDrop = EnergyDropAmount;
        rangoAtaque = rangoLanzamiento;
        
        // No tiene daño directo
        dañoAlPilar = NoDamage;
        dañoAlJugador = NoDamage;
    }

    protected override void Comportamiento()
    {
        if (pilarObjetivo == null) return;
        
        timerCampo -= Time.deltaTime;
        
        float distanciaPilar = Vector3.Distance(transform.position, pilarObjetivo.transform.position);
        
        // Mantenerse a distancia media del Pilar
        if (distanciaPilar > rangoLanzamiento)
        {
            Vector3 dir = pilarObjetivo.transform.position - transform.position;
            dir.y = 0;
            MoverHacia(dir.normalized);
        }
        else if (distanciaPilar < rangoLanzamiento * RetreatRangeMultiplier)
        {
            // Alejarse si está muy cerca
            Vector3 dir = transform.position - pilarObjetivo.transform.position;
            dir.y = 0;
            MoverHacia(dir.normalized);
        }
        else
        {
            // Posición ideal: lanzar campo
            if (timerCampo <= 0)
            {
                LanzarCampo();
                timerCampo = cooldownCampo;
            }
        }
        
        // Mirar al Pilar
        Vector3 lookDir = pilarObjetivo.transform.position - transform.position;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                Quaternion.LookRotation(lookDir), Time.deltaTime * RotationSharpness);
    }

    void LanzarCampo()
    {
        if (prefabCampo == null)
        {
            // Fallback: crear campo en posición del jugador o Pilar
            Vector3 pos = jugadorObjetivo != null ? jugadorObjetivo.transform.position : pilarObjetivo.transform.position;
            CrearCampo(pos);
            return;
        }
        
        // Lanzar hacia el Pilar
        Vector3 posicionCampo = pilarObjetivo.transform.position;
        Instantiate(prefabCampo, posicionCampo, Quaternion.identity);
    }

    void CrearCampo(Vector3 posicion)
    {
        // Crear un campo visual simple
        GameObject campo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        campo.name = "CampoTejedor";
        Destroy(campo.GetComponent<Collider>()); // No necesitamos collider físico
        campo.transform.position = posicion + Vector3.up * FieldHeightMeters;
        campo.transform.localScale = new Vector3(radioCampo * 2f, 0.1f, radioCampo * 2f);
        
        Renderer rend = campo.GetComponent<Renderer>();
        rend.material.color = new Color(0.5f, 0f, 0.5f, 0.3f);
        rend.material.SetFloat("_Mode", 3); // Transparente
        
        // Agregar script de zona
        var zona = campo.AddComponent<WeaverZone>();
        zona.dañoPorSegundo = dañoPorSegundo;
        zona.factorRalentizacion = factorRalentizacion;
        zona.duracion = duracionCampo;
    }
}

/**
 * WeaverZone.cs
 * Zona de efecto del Tejedor. Ralentiza temporalmente, daño por segundo.
 * Stack prohibido: no acumula multiplicadores. Restaura al salir o expirar.
 */
public class WeaverZone : MonoBehaviour
{
    public float dañoPorSegundo = 5f;
    public float factorRalentizacion = 0.5f;
    public float duracion = 8f;
    
    private float timer = 0f;
    private readonly HashSet<PlayerController> playersRalentizados = new HashSet<PlayerController>();
    private readonly HashSet<Enemy> enemigosRalentizados = new HashSet<Enemy>();
    private float pollTimer = 0f;

    void Start()
    {
        timer = duracion;
        Destroy(gameObject, duracion + 0.5f);
    }

    void Update()
    {
        timer -= Time.deltaTime;

        // Poll ralentización cada 0.15s para detectar enter/exit sin sobrecarga
        pollTimer -= Time.deltaTime;
        if (pollTimer <= 0f)
        {
            pollTimer = 0.15f;
            ActualizarRalentizacion();
        }
        
        // Pulso de daño cada segundo (usando FloorToInt trick)
        if (Mathf.FloorToInt(timer) != Mathf.FloorToInt(timer + Time.deltaTime))
        {
            AplicarDañoEnArea();
        }

        // Si zona expiró, restaurar todos antes de Destroy
        if (timer <= 0f)
        {
            RestaurarTodos();
        }
    }

    void ActualizarRalentizacion()
    {
        float radio = transform.localScale.x * 0.5f;
        Collider[] enZona = Physics.OverlapSphere(transform.position, radio);
        var playersDentro = new HashSet<PlayerController>();
        var enemigosDentro = new HashSet<Enemy>();

        foreach (var col in enZona)
        {
            var player = col.GetComponent<PlayerController>();
            if (player != null) playersDentro.Add(player);
            // También buscar en parent (CharacterController está en root, pero collider puede estar en hijo)
            if (player == null) player = col.GetComponentInParent<PlayerController>();
            if (player != null) playersDentro.Add(player);

            var enemy = col.GetComponent<Enemy>();
            if (enemy != null) enemigosDentro.Add(enemy);
            if (enemy == null) enemy = col.GetComponentInParent<Enemy>();
            if (enemy != null) enemigosDentro.Add(enemy);
        }

        // Aplicar a nuevos entrantes (stack prohibido: solo si no estaba ya)
        foreach (var p in playersDentro)
        {
            if (!playersRalentizados.Contains(p))
            {
                p.AplicarRalentizacion(factorRalentizacion, duracion);
                playersRalentizados.Add(p);
            }
        }
        foreach (var e in enemigosDentro)
        {
            if (e is Nest) continue; // Nido estacionario, no afecta movimiento
            if (!enemigosRalentizados.Contains(e))
            {
                e.AplicarRalentizacion(factorRalentizacion, duracion);
                enemigosRalentizados.Add(e);
            }
        }

        // Restaurar a los que salieron
        var salirPlayers = new List<PlayerController>();
        foreach (var p in playersRalentizados) if (!playersDentro.Contains(p)) salirPlayers.Add(p);
        foreach (var p in salirPlayers)
        {
            p.QuitarRalentizacion();
            playersRalentizados.Remove(p);
        }

        var salirEnemigos = new List<Enemy>();
        foreach (var e in enemigosRalentizados) if (!enemigosDentro.Contains(e) || e == null) salirEnemigos.Add(e);
        foreach (var e in salirEnemigos)
        {
            if (e != null) e.QuitarRalentizacion();
            enemigosRalentizados.Remove(e);
        }
        // Limpiar nulos
        enemigosRalentizados.RemoveWhere(e => e == null);
        playersRalentizados.RemoveWhere(p => p == null);
    }

    void RestaurarTodos()
    {
        foreach (var p in playersRalentizados) if (p != null) p.QuitarRalentizacion();
        foreach (var e in enemigosRalentizados) if (e != null) e.QuitarRalentizacion();
        playersRalentizados.Clear();
        enemigosRalentizados.Clear();
    }

    void OnDestroy()
    {
        RestaurarTodos();
    }

    void AplicarDañoEnArea()
    {
        Collider[] enZona = Physics.OverlapSphere(transform.position, transform.localScale.x * 0.5f);
        foreach (var col in enZona)
        {
            var player = col.GetComponent<PlayerController>();
            if (player == null) player = col.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.RecibirDaño(dañoPorSegundo);
            }
            
            var pilar = col.GetComponent<Pilar>();
            if (pilar == null) pilar = col.GetComponentInParent<Pilar>();
            if (pilar != null)
            {
                pilar.RecibirDaño(dañoPorSegundo * 0.5f);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.2f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, transform.localScale.x * 0.5f);
    }
}
