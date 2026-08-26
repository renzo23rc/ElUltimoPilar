/**
 * WeaponSystem.cs
 * Gestiona las 3 armas base del jugador: Directa, Área, Cuerpo a cuerpo.
 * Incluye variantes temporales como drops.
 * 
 * Colocar en el mismo GameObject que PlayerController.
 */
using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class WeaponSystem : MonoBehaviour
{
    public enum TipoArma { Directa, Area, CuerpoACuerpo }
    
    [System.Serializable]
    public class Arma
    {
        public string nombre;
        public TipoArma tipo;
        public float daño;
        public float cadencia;
        public int municionMaxima;
        [HideInInspector] public int municionActual;
        public float alcance;
        public float radioArea; // Solo para arma de área
        public GameObject prefabProyectil;
        public GameObject prefabImpacto;
        
        [Header("Feedback")]
        public AudioClip sonidoDisparo;
        public float intensidadScreenShake = 0.1f;
    }
    
    [Header("Armas Base")]
    public Arma armaDirecta = new Arma
    {
        nombre = "Rifle Directo",
        tipo = TipoArma.Directa,
        daño = 15f,
        cadencia = 0.15f,
        municionMaxima = 60,
        municionActual = 60,
        alcance = 100f
    };
    
    public Arma armaArea = new Arma
    {
        nombre = "Lanzador de Área",
        tipo = TipoArma.Area,
        daño = 40f,
        cadencia = 1.2f,
        municionMaxima = 12,
        municionActual = 12,
        alcance = 30f,
        radioArea = 5f
    };
    
    public Arma armaMelee = new Arma
    {
        nombre = "Martillo de Choque",
        tipo = TipoArma.CuerpoACuerpo,
        daño = 50f,
        cadencia = 0.8f,
        municionMaxima = -1, // Sin munición
        municionActual = -1,
        alcance = 3f,
        radioArea = 2.5f
    };
    
    [Header("Estado")]
    public TipoArma armaEquipada = TipoArma.Directa;
    public float cooldownDisparo = 0f;
    
    [Header("Referencias")]
    public Transform puntoDisparo;
    public Camera camara;
    public LayerMask capasImpacto;
    
    // Eventos
    public event Action<Arma> OnDisparo;
    public event Action OnSinMunicion;
    public event Action<TipoArma> OnCambioArma;
    
    private PlayerController player;

    void Start()
    {
        player = GetComponent<PlayerController>();
        if (camara == null)
            camara = Camera.main;
        if (puntoDisparo == null && camara != null)
            puntoDisparo = camara.transform;
    }

    void Update()
    {
        cooldownDisparo -= Time.deltaTime;
        
        if (Keyboard.current == null || Mouse.current == null) return;
        
        // Cambio de arma con teclas 1, 2, 3
        if (Keyboard.current.digit1Key.wasPressedThisFrame) CambiarArma(TipoArma.Directa);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) CambiarArma(TipoArma.Area);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) CambiarArma(TipoArma.CuerpoACuerpo);
        
        // Scroll para cambiar arma
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll > 0) CambiarArmaSiguiente();
        if (scroll < 0) CambiarArmaAnterior();
    }

    public void DispararActual()
    {
        if (cooldownDisparo > 0) return;
        
        Arma actual = ObtenerArmaActual();
        if (actual == null) return;
        
        // Verificar munición
        if (actual.municionActual == 0)
        {
            OnSinMunicion?.Invoke();
            Debug.Log("[WeaponSystem] ¡Sin munición!");
            return;
        }
        
        // Gastar munición (excepto melee)
        if (actual.tipo != TipoArma.CuerpoACuerpo)
        {
            actual.municionActual--;
        }
        
        cooldownDisparo = actual.cadencia;
        
        switch (actual.tipo)
        {
            case TipoArma.Directa:
                DispararDirecto(actual);
                break;
            case TipoArma.Area:
                DispararArea(actual);
                break;
            case TipoArma.CuerpoACuerpo:
                AtacarMelee(actual);
                break;
        }
        
        OnDisparo?.Invoke(actual);
        
        // Screen shake simple
        // ScreenShake.Instance?.Shake(actual.intensidadScreenShake);
    }

    void DispararDirecto(Arma arma)
    {
        Ray ray = new Ray(puntoDisparo.position, puntoDisparo.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, arma.alcance, capasImpacto))
        {
            var enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.RecibirDaño(arma.daño);
                Debug.Log($"[WeaponSystem] Impacto directo: {arma.daño} daño a {enemy.name}");
            }
            
            // Efecto de impacto
            if (arma.prefabImpacto != null)
                Instantiate(arma.prefabImpacto, hit.point, Quaternion.identity);
            
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red, 0.3f);
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * arma.alcance, Color.white, 0.3f);
        }
        
        // Proyectil visual
        if (arma.prefabProyectil != null)
        {
            GameObject proj = Instantiate(arma.prefabProyectil, puntoDisparo.position, puntoDisparo.rotation);
            var rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = puntoDisparo.forward * 50f;
        }
    }

    void DispararArea(Arma arma)
    {
        Ray ray = new Ray(puntoDisparo.position, puntoDisparo.forward);
        Vector3 puntoImpacto;
        
        if (Physics.Raycast(ray, out RaycastHit hit, arma.alcance, capasImpacto))
        {
            puntoImpacto = hit.point;
        }
        else
        {
            puntoImpacto = puntoDisparo.position + puntoDisparo.forward * arma.alcance;
        }
        
        // Daño en área
        Collider[] afectados = Physics.OverlapSphere(puntoImpacto, arma.radioArea);
        int contador = 0;
        foreach (var col in afectados)
        {
            var enemy = col.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.RecibirDaño(arma.daño);
                contador++;
            }
        }
        
        Debug.Log($"[WeaponSystem] Explosión de área: {arma.daño} daño a {contador} enemigos");
        
        // Efecto visual
        if (arma.prefabImpacto != null)
            Instantiate(arma.prefabImpacto, puntoImpacto, Quaternion.identity);
        
        // Debug
        Debug.DrawRay(ray.origin, ray.direction * (puntoImpacto - puntoDisparo.position).magnitude, Color.yellow, 0.5f);
    }

    void AtacarMelee(Arma arma)
    {
        // Ataque en arco frontal
        Collider[] afectados = Physics.OverlapSphere(transform.position + transform.forward * 1.5f, arma.radioArea);
        int contador = 0;
        foreach (var col in afectados)
        {
            var enemy = col.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.RecibirDaño(arma.daño);
                
                // Empujar enemigo (física)
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 dirEmpuje = (col.transform.position - transform.position).normalized;
                    dirEmpuje.y = 0.5f;
                    rb.AddForce(dirEmpuje * 500f);
                }
                
                contador++;
            }
        }
        
        Debug.Log($"[WeaponSystem] Ataque melee: {arma.daño} daño a {contador} enemigos");
    }

    public void CambiarArma(TipoArma tipo)
    {
        if (armaEquipada == tipo) return;
        armaEquipada = tipo;
        OnCambioArma?.Invoke(tipo);
        Debug.Log($"[WeaponSystem] Arma cambiada a: {ObtenerArmaActual()?.nombre}");
    }

    void CambiarArmaSiguiente()
    {
        int actual = (int)armaEquipada;
        int siguiente = (actual + 1) % 3;
        CambiarArma((TipoArma)siguiente);
    }

    void CambiarArmaAnterior()
    {
        int actual = (int)armaEquipada;
        int anterior = (actual - 1 + 3) % 3;
        CambiarArma((TipoArma)anterior);
    }

    public Arma ObtenerArmaActual()
    {
        return armaEquipada switch
        {
            TipoArma.Directa => armaDirecta,
            TipoArma.Area => armaArea,
            TipoArma.CuerpoACuerpo => armaMelee,
            _ => null
        };
    }

    public void ReponerMunicion()
    {
        armaDirecta.municionActual = armaDirecta.municionMaxima;
        armaArea.municionActual = armaArea.municionMaxima;
        Debug.Log("[WeaponSystem] Munición repuesta");
    }

    void OnDrawGizmosSelected()
    {
        if (armaEquipada == TipoArma.CuerpoACuerpo)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + transform.forward * 1.5f, armaMelee.radioArea);
        }
    }
}
