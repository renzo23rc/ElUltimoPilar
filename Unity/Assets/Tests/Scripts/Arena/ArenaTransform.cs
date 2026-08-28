/**
 * ArenaTransform.cs
 * Gestiona las transformaciones acumulativas e irreversibles de la arena
 * según los umbrales de vida del Pilar.
 * 
 * Según el GDD:
 * - Fase 1 (100-75%): Arena base
 * - Fase 2 (75-50%): Pozo central se abre
 * - Fase 3 (50-25%): Zona de gravedad alterada
 * - Fase 4 (25-0%): Protocolo de emergencia + caos
 * 
 * Colocar en un GameObject vacío "ArenaManager" o en el suelo/arena.
 */
using UnityEngine;
using System;
using System.Collections;

public class ArenaTransform : MonoBehaviour
{
    [Header("Referencias")]
    public Pilar pilar;
    public GameObject sueloBase;
    
    [Header("Elementos de Transformación")]
    public GameObject pozoCentral; // Se activa en fase 2
    public GameObject zonaGravedad; // Se activa en fase 3
    public GameObject escombrosFase4; // Se activan en fase 4
    public GameObject[] obstaculosAdicionales;
    
    [Header("Configuración")]
    public float tiempoAvisoPrevio = 3f;
    public Color colorAviso = Color.yellow;
    public AudioClip sonidoTransformacion;
    
    [Header("Estado")]
    public int faseActual = 1;
    public bool transformacionEnProgreso = false;
    
    // Eventos
    public event Action<int> OnTransformacionIniciada;
    public event Action<int> OnTransformacionCompletada;
    
    private bool[] fasesActivadas = new bool[5]; // Índice 0 no usado, 1-4
    private readonly System.Collections.Generic.Queue<int> colaFases = new System.Collections.Generic.Queue<int>();
    private bool procesandoCola = false;

    void Start()
    {
        if (pilar == null)
            pilar = FindFirstObjectByType<Pilar>();
        
        // Suscribirse a cambios de fase del Pilar
        if (pilar != null)
        {
            pilar.OnFaseCambiada += OnPilarFaseCambiada;
        }
        
        // Estado inicial: todo desactivado excepto fase 1
        InicializarEstado();
    }

    void OnDestroy()
    {
        if (pilar != null)
            pilar.OnFaseCambiada -= OnPilarFaseCambiada;
    }

    void InicializarEstado()
    {
        if (pozoCentral != null) pozoCentral.SetActive(false);
        if (zonaGravedad != null) zonaGravedad.SetActive(false);
        if (escombrosFase4 != null) escombrosFase4.SetActive(false);
        
        if (obstaculosAdicionales != null)
        {
            foreach (var obs in obstaculosAdicionales)
            {
                if (obs != null) obs.SetActive(false);
            }
        }
    }

    void OnPilarFaseCambiada(int nuevaFase)
    {
        if (nuevaFase <= faseActual) return; // Solo avanza, nunca retrocede
        // Encolar todas las fases intermedias no activadas, con aviso visual/sonoro por fase
        for (int f = faseActual + 1; f <= nuevaFase; f++)
        {
            if (f < fasesActivadas.Length && !fasesActivadas[f] && !colaFases.Contains(f))
            {
                colaFases.Enqueue(f);
            }
        }
        faseActual = nuevaFase;
        if (!procesandoCola)
            StartCoroutine(ProcesarColaFases());
    }

    IEnumerator ProcesarColaFases()
    {
        procesandoCola = true;
        while (colaFases.Count > 0)
        {
            int fase = colaFases.Dequeue();
            yield return StartCoroutine(EjecutarTransformacion(fase));
        }
        procesandoCola = false;
    }

    IEnumerator EjecutarTransformacion(int fase)
    {
        transformacionEnProgreso = true;
        
        // 1. Aviso previo
        Debug.Log($"[ArenaTransform] ¡AVISO! Transformación a Fase {fase} en {tiempoAvisoPrevio} segundos...");
        OnTransformacionIniciada?.Invoke(fase);
        
        // Sonido de aviso (stinger) con guard para Camera.main null
        if (sonidoTransformacion != null)
        {
            Vector3 posAudio = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(sonidoTransformacion, posAudio, 0.7f);
        }

        // Advertencia específica del pozo (Fase 2): visual + HUD 3s previo
        if (fase == 2 && pozoCentral != null)
        {
            StartCoroutine(AdvertenciaPozoVisual(tiempoAvisoPrevio));
            var hud = FindFirstObjectByType<TestHUD>();
            if (hud != null) hud.MostrarAdvertencia("¡ADVERTENCIA: ¡POZO SE ABRE! ¡Aléjate del centro! (" + tiempoAvisoPrevio + "s)", Color.red, tiempoAvisoPrevio);
            else Debug.LogWarning("[ArenaTransform] Advertencia pozo: ¡Pozo en " + tiempoAvisoPrevio + "s!");
        }
        else if (fase == 3)
        {
            var hud = FindFirstObjectByType<TestHUD>();
            if (hud != null) hud.MostrarAdvertencia("¡ALERTA: Zona gravedad alterada!", new Color(0.6f,0.2f,1f), tiempoAvisoPrevio);
        }
        else if (fase == 4)
        {
            var hud = FindFirstObjectByType<TestHUD>();
            if (hud != null) hud.MostrarAdvertencia("¡PROTOCOLO EMERGENCIA! Torretas activadas", Color.cyan, tiempoAvisoPrevio);
        }
        
        // Efecto visual de aviso (parpadear el suelo)
        yield return StartCoroutine(AvisoVisual(tiempoAvisoPrevio));
        
        // 2. Ejecutar transformación
        switch (fase)
        {
            case 2:
                ActivarPozoCentral();
                break;
            case 3:
                ActivarZonaGravedad();
                break;
            case 4:
                ActivarProtocoloEmergencia();
                break;
        }
        
        fasesActivadas[fase] = true;
        transformacionEnProgreso = false;
        OnTransformacionCompletada?.Invoke(fase);
        
        Debug.Log($"[ArenaTransform] Transformación a Fase {fase} completada.");
    }

    IEnumerator AvisoVisual(float duracion)
    {
        float timer = 0f;
        Renderer rend = sueloBase?.GetComponent<Renderer>();
        Color colorOriginal = rend != null ? rend.material.color : Color.gray;
        
        while (timer < duracion)
        {
            timer += Time.deltaTime;
            float t = timer / duracion;
            
            // Parpadeo que acelera
            float parpadeo = Mathf.PingPong(Time.time * (1f + t * 5f), 1f);
            
            if (rend != null)
            {
                rend.material.color = Color.Lerp(colorOriginal, colorAviso, parpadeo * 0.5f);
            }
            
            yield return null;
        }
        
        if (rend != null)
            rend.material.color = colorOriginal;
    }

    IEnumerator AdvertenciaPozoVisual(float duracion)
    {
        if (pozoCentral == null) yield break;
        Vector3 pos = pozoCentral.transform.position + Vector3.up * 0.5f;
        Vector3 escalaBase = pozoCentral.transform.localScale;
        // Cilindro rojo pulsante
        GameObject adv = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        adv.name = "AdvertenciaPozo";
        Destroy(adv.GetComponent<Collider>());
        adv.transform.position = pos;
        adv.transform.localScale = new Vector3(escalaBase.x * 1.2f, 0.2f, escalaBase.z * 1.2f);
        var rend = adv.GetComponent<Renderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        var mat = new Material(s);
        Color baseCol = new Color(1, 0, 0, 0.5f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseCol);
        else mat.color = baseCol;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseCol);
        mat.renderQueue = 3000;
        rend.material = mat;

        GameObject anillo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        anillo.name = "AnilloAdvertenciaPozo";
        Destroy(anillo.GetComponent<Collider>());
        anillo.transform.position = pos + Vector3.up * 0.3f;
        anillo.transform.localScale = new Vector3(escalaBase.x * 1.5f, 0.05f, escalaBase.z * 1.5f);
        var rend2 = anillo.GetComponent<Renderer>();
        var mat2 = new Material(s);
        if (mat2.HasProperty("_BaseColor")) mat2.SetColor("_BaseColor", Color.yellow);
        else mat2.color = Color.yellow;
        if (mat2.HasProperty("_Color")) mat2.SetColor("_Color", Color.yellow);
        rend2.material = mat2;

        float timer = 0f;
        while (timer < duracion)
        {
            timer += Time.deltaTime;
            float pulse = 1f + Mathf.PingPong(Time.time * 4f, 0.4f);
            adv.transform.localScale = new Vector3(escalaBase.x * 1.2f * pulse, 0.2f, escalaBase.z * 1.2f * pulse);
            anillo.transform.localScale = new Vector3(escalaBase.x * (1.5f + Mathf.Sin(Time.time * 6f) * 0.3f), 0.05f, escalaBase.z * (1.5f + Mathf.Sin(Time.time * 6f) * 0.3f));
            float a = 0.4f + Mathf.PingPong(Time.time * 5f, 0.4f);
            Color c = new Color(1, 0, 0, a);
            if (rend != null && rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", c);
            else if (rend != null) rend.material.color = c;
            yield return null;
        }
        Destroy(adv);
        Destroy(anillo);
    }

    void ActivarPozoCentral()
    {
        if (pozoCentral != null)
        {
            pozoCentral.SetActive(true);
            
            // Animación de apertura
            StartCoroutine(AnimarApertura(pozoCentral));
        }
        
        Debug.Log("[ArenaTransform] Pozo central abierto. ¡Cuidado con los bordes!");
    }

    void ActivarZonaGravedad()
    {
        if (zonaGravedad != null)
        {
            zonaGravedad.SetActive(true);
            // Animacion exagerada de aparicion
            StartCoroutine(AnimarApertura(zonaGravedad));
            // Pulsar luz y escalar
            var luz = zonaGravedad.GetComponentInChildren<Light>();
            if (luz == null)
            {
                var goLuz = new GameObject("LuzZona");
                goLuz.transform.SetParent(zonaGravedad.transform);
                goLuz.transform.localPosition = Vector3.zero;
                luz = goLuz.AddComponent<Light>();
                luz.type = LightType.Point;
                luz.range = 18f;
                luz.intensity = 4f;
                luz.color = new Color(0.7f,0.2f,1f);
            }
            // Efecto visual de distorsión
            var particulas = zonaGravedad.GetComponentInChildren<ParticleSystem>();
            if (particulas != null) particulas.Play();
        }
        
        Debug.Log("[ArenaTransform] ¡ZONA DE GRAVEDAD ALTERADA EXAGERADA! Gravedad 80% menos, salto 2.2x, flotacion");
    }

    void ActivarProtocoloEmergencia()
    {
        if (escombrosFase4 != null)
        {
            escombrosFase4.SetActive(true);
        }
        
        // Activar obstáculos adicionales
        if (obstaculosAdicionales != null)
        {
            foreach (var obs in obstaculosAdicionales)
            {
                if (obs != null) obs.SetActive(true);
            }
        }
        
        // Caída de escombros visual
        StartCoroutine(CaidaEscombros());
        
        Debug.Log("[ArenaTransform] PROTOCOLO DE EMERGENCIA. La arena está severamente dañada.");
    }

    IEnumerator AnimarApertura(GameObject objeto)
    {
        Vector3 escalaFinal = objeto.transform.localScale;
        objeto.transform.localScale = Vector3.zero;
        
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            objeto.transform.localScale = Vector3.Lerp(Vector3.zero, escalaFinal, timer);
            yield return null;
        }
        
        objeto.transform.localScale = escalaFinal;
    }

    IEnumerator CaidaEscombros()
    {
        // Simular caída de escombros pequeños
        for (int i = 0; i < 10; i++)
        {
            Vector3 pos = UnityEngine.Random.insideUnitSphere * 15f;
            pos.y = 10f;
            
            GameObject escombro = GameObject.CreatePrimitive(PrimitiveType.Cube);
            escombro.transform.position = pos;
            escombro.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.3f, 1f);
            escombro.GetComponent<Renderer>().material.color = Color.gray;
            
            Destroy(escombro.GetComponent<Collider>());
            
            Rigidbody rb = escombro.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            
            Destroy(escombro, 3f);
            
            yield return new WaitForSeconds(0.2f);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Dibujar radio de la arena
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 20f);
        
        // Dibujar zonas de transformación
        if (pozoCentral != null)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(pozoCentral.transform.position, pozoCentral.transform.localScale);
        }
    }
}

// Script de zona de gravedad exagerado - va en el objeto ZonaGravedad
public class ZonaGravedadEffect : MonoBehaviour
{
    [Header("Efecto Exagerado")]
    public float fuerzaAscenso = 18f;
    public float radioEfecto = 5f;
    public float fuerzaTornado = 4f;
    
    void OnTriggerEnter(Collider other)
    {
        var pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.EntrarZonaGravedad();
            Debug.Log("[ZonaGravedad] Player ENTER - impulso exagerado");
        }
        var rb = other.GetComponent<Rigidbody>();
        if (rb != null && other.GetComponent<Enemy>() != null)
        {
            rb.AddForce(Vector3.up * fuerzaAscenso, ForceMode.VelocityChange);
            rb.linearDamping = 2.5f; // flotacion
        }
    }
    void OnTriggerStay(Collider other)
    {
        var rb = other.GetComponent<Rigidbody>();
        if (rb != null && other.GetComponent<Enemy>() != null)
        {
            // Flotacion continua exagerada + tornado leve
            rb.AddForce(Vector3.up * 9f, ForceMode.Acceleration);
            rb.AddForce(new Vector3(Mathf.Sin(Time.time*2f)*fuerzaTornado, 0, Mathf.Cos(Time.time*2f)*fuerzaTornado), ForceMode.Acceleration);
            // Enemigos casi no avanzan en zona
            rb.linearVelocity = new Vector3(rb.linearVelocity.x*0.92f, rb.linearVelocity.y, rb.linearVelocity.z*0.92f);
        }
    }
    void OnTriggerExit(Collider other)
    {
        var pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.SalirZonaGravedad();
            Debug.Log("[ZonaGravedad] Player EXIT");
        }
        var rb = other.GetComponent<Rigidbody>();
        if (rb != null) rb.linearDamping = 0.1f;
    }
    void Update()
    {
        // Pulso visual exagerado
        float s = 1f + Mathf.Sin(Time.time*1.8f)*0.07f;
        transform.localScale = new Vector3(10f*s, 4f, 10f*s);
        var rend = GetComponent<Renderer>();
        if (rend != null)
        {
            Color baseC = new Color(0.6f,0.1f,1f,0.35f);
            float a = 0.35f + Mathf.Sin(Time.time*2.2f)*0.12f;
            rend.material.color = new Color(baseC.r, baseC.g, baseC.b, a);
        }
    }
}

public class ParticulaFlotante : MonoBehaviour
{
    Vector3 ini; float spd, amp; float off;
    void Start(){ ini=transform.localPosition; spd=UnityEngine.Random.Range(0.8f,1.8f); amp=UnityEngine.Random.Range(0.15f,0.35f); off=UnityEngine.Random.Range(0,6.28f); }
    void Update(){
        transform.localPosition = ini + Vector3.up * Mathf.Sin(Time.time*spd+off)*amp;
        transform.Rotate(Vector3.up, 45*Time.deltaTime);
        transform.Rotate(Vector3.right, 30*Time.deltaTime);
    }
}
