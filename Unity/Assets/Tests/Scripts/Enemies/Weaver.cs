/**
 * Weaver.cs
 * Tejedor: Enemigo a distancia que no ataca directamente.
 * Lanza un campo que ralentiza al jugador y reduce visibilidad cerca del Pilar.
 * Aplica daño por segundo dentro del área.
 */
using UnityEngine;

public class Weaver : Enemy
{
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
        velocidadMovimiento = 3f;
        vidaMaxima = 35f;
        vidaActual = vidaMaxima;
        energiaDrop = 4;
        rangoAtaque = rangoLanzamiento;
        
        // No tiene daño directo
        dañoAlPilar = 0f;
        dañoAlJugador = 0f;
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
        else if (distanciaPilar < rangoLanzamiento * 0.6f)
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
                Quaternion.LookRotation(lookDir), Time.deltaTime * 3f);
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
        campo.transform.position = posicion + Vector3.up * 0.1f;
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
 * Zona de efecto del Tejedor.
 */
public class WeaverZone : MonoBehaviour
{
    public float dañoPorSegundo = 5f;
    public float factorRalentizacion = 0.5f;
    public float duracion = 8f;
    
    private float timer = 0f;

    void Start()
    {
        timer = duracion;
        Destroy(gameObject, duracion + 0.5f);
    }

    void Update()
    {
        timer -= Time.deltaTime;
        
        // Pulso de daño cada segundo
        if (Mathf.FloorToInt(timer) != Mathf.FloorToInt(timer + Time.deltaTime))
        {
            AplicarDañoEnArea();
        }
    }

    void AplicarDañoEnArea()
    {
        Collider[] enZona = Physics.OverlapSphere(transform.position, transform.localScale.x * 0.5f);
        foreach (var col in enZona)
        {
            var player = col.GetComponent<PlayerController>();
            if (player != null)
            {
                player.RecibirDaño(dañoPorSegundo);
                // Aquí se aplicaría ralentización al sistema de movimiento
            }
            
            var pilar = col.GetComponent<Pilar>();
            if (pilar != null)
            {
                pilar.RecibirDaño(dañoPorSegundo * 0.5f);
            }
        }
    }
}
