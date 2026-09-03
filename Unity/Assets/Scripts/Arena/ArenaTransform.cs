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
using System.Collections.Generic;

public class ArenaTransform : MonoBehaviour
{
    private const int FirstArenaPhase = 1;
    private const int LastArenaPhase = 4;
    private const int PhaseArrayLength = 5;
    private const float AudioVolume = 0.7f;
    private const float PozoWarningHeightMeters = 0.5f;
    private const float WarningMarkerHeight = 0.2f;
    private const float WarningRingHeight = 0.05f;
    private const float WarningMarkerScale = 1.2f;
    private const float WarningRingScale = 1.5f;
    private const float WarningPulseSpeed = 4f;
    private const float WarningPulseAmount = 0.4f;
    private const float WarningRingSpeed = 6f;
    private const float WarningRingAmount = 0.3f;
    private const float WarningAlphaBase = 0.4f;
    private const float WarningAlphaAmount = 0.4f;
    private const float GravityLightRangeMeters = 18f;
    private const float GravityLightIntensity = 4f;
    private const float DebrisSpawnRadiusMeters = 15f;
    private const float DebrisSpawnHeightMeters = 10f;
    private const int DebrisCount = 10;
    private const float DebrisMinimumScale = 0.3f;
    private const float DebrisMaximumScale = 1f;
    private const float DebrisMass = 0.1f;
    private const float DebrisLifetimeSeconds = 3f;
    private const float DebrisSpawnIntervalSeconds = 0.2f;
    private const float ArenaGizmoRadiusMeters = 20f;
    private static readonly Color GravityZoneColor = new Color(0.6f, 0.2f, 1f);
    private static readonly Color WarningMarkerColor = new Color(1f, 0f, 0f, 0.5f);

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
    public int faseActual = FirstArenaPhase;
    public bool transformacionEnProgreso = false;
    
    // Eventos
    public event Action<int> OnTransformacionIniciada;
    public event Action<int> OnTransformacionCompletada;
    
    private bool[] fasesActivadas = new bool[PhaseArrayLength]; // Índice 0 no usado, 1-4
    private readonly System.Collections.Generic.Queue<int> colaFases = new System.Collections.Generic.Queue<int>();
    private bool procesandoCola = false;

    private Vector3 initialPitScale;
    private Vector3 initialGravityZoneScale;
    private Vector3 initialEmergencyDebrisScale;
    private Color initialFloorColor;
    private bool initialPitScaleCaptured;
    private bool initialGravityZoneScaleCaptured;
    private bool initialEmergencyDebrisScaleCaptured;
    private bool initialFloorColorCaptured;
    private GameObject warningMarker;
    private GameObject warningRing;
    private readonly List<GameObject> activeDebris = new List<GameObject>();

    void Start()
    {
        if (pilar == null)
            pilar = FindFirstObjectByType<Pilar>();
        
        // Suscribirse a cambios de fase del Pilar
        if (pilar != null)
        {
            pilar.OnFaseCambiada += OnPilarFaseCambiada;
        }
        
            // GameManager owns the managed match reset. Preserve standalone
            // arena scenes without repeating the reset in a managed match.
            if (GameManager.Instance == null)
                ResetState();
    }

    void OnDestroy()
    {
        if (pilar != null)
            pilar.OnFaseCambiada -= OnPilarFaseCambiada;
    }

    public void ResetState()
    {
        CaptureInitialPresentationState();
        StopAllCoroutines();
        colaFases.Clear();
        procesandoCola = false;
        transformacionEnProgreso = false;
        faseActual = FirstArenaPhase;
        if (fasesActivadas == null || fasesActivadas.Length != PhaseArrayLength)
        {
fasesActivadas = new bool[PhaseArrayLength];
        }
        else
        {
Array.Clear(fasesActivadas, 0, fasesActivadas.Length);
        }
    
        ClearGeneratedArenaObjects();
        InicializarEstado();
        RestoreInitialPresentationState();
    }

    void CaptureInitialPresentationState()
    {
        if (!initialPitScaleCaptured && pozoCentral != null)
        {
            initialPitScale = pozoCentral.transform.localScale;
            initialPitScaleCaptured = true;
        }
        if (!initialGravityZoneScaleCaptured && zonaGravedad != null)
        {
            initialGravityZoneScale = zonaGravedad.transform.localScale;
            initialGravityZoneScaleCaptured = true;
        }
        if (!initialEmergencyDebrisScaleCaptured && escombrosFase4 != null)
        {
            initialEmergencyDebrisScale = escombrosFase4.transform.localScale;
            initialEmergencyDebrisScaleCaptured = true;
        }
        if (!initialFloorColorCaptured && sueloBase != null)
        {
            var floorRenderer = sueloBase.GetComponent<Renderer>();
            if (floorRenderer != null)
            {
                initialFloorColor = floorRenderer.material.color;
                initialFloorColorCaptured = true;
            }
        }
    }

    void RestoreInitialPresentationState()
    {
        if (initialPitScaleCaptured && pozoCentral != null)
            pozoCentral.transform.localScale = initialPitScale;
        if (initialGravityZoneScaleCaptured && zonaGravedad != null)
            zonaGravedad.transform.localScale = initialGravityZoneScale;
        if (initialEmergencyDebrisScaleCaptured && escombrosFase4 != null)
            escombrosFase4.transform.localScale = initialEmergencyDebrisScale;
        if (initialFloorColorCaptured && sueloBase != null)
        {
            var floorRenderer = sueloBase.GetComponent<Renderer>();
            if (floorRenderer != null) floorRenderer.material.color = initialFloorColor;
        }
    }

    void ClearGeneratedArenaObjects()
    {
        ClearWarningObjects();
        foreach (var debris in activeDebris)
        {
            if (debris == null) continue;
            debris.SetActive(false);
            Destroy(debris);
        }
        activeDebris.Clear();
    }

    void ClearWarningObjects()
    {
        if (warningMarker != null)
        {
            warningMarker.SetActive(false);
            Destroy(warningMarker);
            warningMarker = null;
        }
        if (warningRing != null)
        {
            warningRing.SetActive(false);
            Destroy(warningRing);
            warningRing = null;
        }
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
        for (int phase = faseActual + 1; phase <= nuevaFase; phase++)
        {
            if ((fasesActivadas != null) && (phase >= FirstArenaPhase) && (phase < fasesActivadas.Length)
                && !fasesActivadas[phase] && !colaFases.Contains(phase))
            {
                colaFases.Enqueue(phase);
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
        
            // Sonido de aviso (stinger) desde la camara explicita del jugador primario.
            if (sonidoTransformacion != null)
            {
                var gameManager = GameManager.Instance;
                var primaryCamera = gameManager != null && gameManager.player != null
                    ? gameManager.player.camaraJugador
                    : null;
                Vector3 posAudio = primaryCamera != null
                    ? primaryCamera.transform.position
                    : transform.position;
                AudioSource.PlayClipAtPoint(sonidoTransformacion, posAudio, AudioVolume);
            }

        // Advertencia específica del pozo (Fase 2): visual + HUD 3s previo
        if (fase == 2 && pozoCentral != null)
        {
            StartCoroutine(AdvertenciaPozoVisual(tiempoAvisoPrevio));
            var hud = FindFirstObjectByType<Hud>();
            if (hud != null) hud.MostrarAdvertencia("¡ADVERTENCIA: ¡POZO SE ABRE! ¡Aléjate del centro! (" + tiempoAvisoPrevio + "s)", Color.red, tiempoAvisoPrevio);
            else Debug.LogWarning("[ArenaTransform] Advertencia pozo: ¡Pozo en " + tiempoAvisoPrevio + "s!");
        }
        else if (fase == 3)
        {
            var hud = FindFirstObjectByType<Hud>();
            if (hud != null) hud.MostrarAdvertencia("¡ALERTA: Zona gravedad alterada!", GravityZoneColor, tiempoAvisoPrevio);
        }
        else if (fase == 4)
        {
            var hud = FindFirstObjectByType<Hud>();
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
        Vector3 pos = pozoCentral.transform.position + Vector3.up * PozoWarningHeightMeters;
        Vector3 escalaBase = pozoCentral.transform.localScale;
        // Cilindro rojo pulsante
        warningMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        GameObject adv = warningMarker;
        adv.name = "AdvertenciaPozo";
        Destroy(adv.GetComponent<Collider>());
        adv.transform.position = pos;
        adv.transform.localScale = new Vector3(escalaBase.x * WarningMarkerScale, WarningMarkerHeight, escalaBase.z * WarningMarkerScale);
        var rend = adv.GetComponent<Renderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        var mat = new Material(s);
        Color baseColor = WarningMarkerColor;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        else mat.color = baseColor;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
        mat.renderQueue = 3000;
        rend.material = mat;

        warningRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        GameObject anillo = warningRing;
        anillo.name = "AnilloAdvertenciaPozo";
        Destroy(anillo.GetComponent<Collider>());
        anillo.transform.position = pos + Vector3.up * 0.3f;
        anillo.transform.localScale = new Vector3(escalaBase.x * WarningRingScale, WarningRingHeight, escalaBase.z * WarningRingScale);
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
            float pulse = 1f + Mathf.PingPong(Time.time * WarningPulseSpeed, WarningPulseAmount);
            adv.transform.localScale = new Vector3(escalaBase.x * WarningMarkerScale * pulse, WarningMarkerHeight, escalaBase.z * WarningMarkerScale * pulse);
            float ringScale = WarningRingScale + Mathf.Sin(Time.time * WarningRingSpeed) * WarningRingAmount;
            anillo.transform.localScale = new Vector3(escalaBase.x * ringScale, WarningRingHeight, escalaBase.z * ringScale);
            float alpha = WarningAlphaBase + Mathf.PingPong(Time.time * 5f, WarningAlphaAmount);
            Color color = new Color(1f, 0f, 0f, alpha);
            if (rend != null && rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", color);
            else if (rend != null) rend.material.color = color;
            yield return null;
        }
        Destroy(adv);
        Destroy(anillo);
        if (warningMarker == adv) warningMarker = null;
        if (warningRing == anillo) warningRing = null;
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
                luz.range = GravityLightRangeMeters;
                luz.intensity = GravityLightIntensity;
                luz.color = GravityZoneColor;
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
        for (int index = 0; index < DebrisCount; index++)
        {
            Vector3 pos = UnityEngine.Random.insideUnitSphere * DebrisSpawnRadiusMeters;
            pos.y = DebrisSpawnHeightMeters;
            
            GameObject escombro = GameObject.CreatePrimitive(PrimitiveType.Cube);
            activeDebris.Add(escombro);
            escombro.transform.position = pos;
            escombro.transform.localScale = Vector3.one * UnityEngine.Random.Range(DebrisMinimumScale, DebrisMaximumScale);
            escombro.GetComponent<Renderer>().material.color = Color.gray;
            
            Destroy(escombro.GetComponent<Collider>());
            
            Rigidbody rb = escombro.AddComponent<Rigidbody>();
            rb.mass = DebrisMass;
            
            Destroy(escombro, DebrisLifetimeSeconds);
            
            yield return new WaitForSeconds(DebrisSpawnIntervalSeconds);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Dibujar radio de la arena
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, ArenaGizmoRadiusMeters);
        
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

    private const float EnemyDamping = 2.5f;
    private const float EnemyUpwardAcceleration = 9f;
    private const float EnemyHorizontalVelocityRetention = 0.92f;
    private const float ExitDamping = 0.1f;
    private const float VisualPulseBase = 1f;
    private const float VisualPulseSpeed = 1.8f;
    private const float VisualPulseAmount = 0.07f;
    private const float VisualAlphaBase = 0.35f;
    private const float VisualAlphaSpeed = 2.2f;
    private const float VisualAlphaAmount = 0.12f;
    private const float VisualWidthMeters = 10f;
    private const float VisualHeightMeters = 4f;
    private static readonly Color VisualColor = new Color(0.6f, 0.1f, 1f, VisualAlphaBase);
    
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
            rb.linearDamping = EnemyDamping; // flotacion
        }
    }
    void OnTriggerStay(Collider other)
    {
        var rb = other.GetComponent<Rigidbody>();
        if (rb != null && other.GetComponent<Enemy>() != null)
        {
            // Flotacion continua exagerada + tornado leve
            rb.AddForce(Vector3.up * EnemyUpwardAcceleration, ForceMode.Acceleration);
            rb.AddForce(new Vector3(Mathf.Sin(Time.time*2f)*fuerzaTornado, 0, Mathf.Cos(Time.time*2f)*fuerzaTornado), ForceMode.Acceleration);
            // Enemigos casi no avanzan en zona
            rb.linearVelocity = new Vector3(rb.linearVelocity.x * EnemyHorizontalVelocityRetention, rb.linearVelocity.y, rb.linearVelocity.z * EnemyHorizontalVelocityRetention);
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
        if (rb != null) rb.linearDamping = ExitDamping;
    }
    void Update()
    {
        // Pulso visual exagerado
        float scale = VisualPulseBase + Mathf.Sin(Time.time * VisualPulseSpeed) * VisualPulseAmount;
        transform.localScale = new Vector3(VisualWidthMeters * scale, VisualHeightMeters, VisualWidthMeters * scale);
        var rend = GetComponent<Renderer>();
        if (rend != null)
        {
            Color baseColor = VisualColor;
            float alpha = VisualAlphaBase + Mathf.Sin(Time.time * VisualAlphaSpeed) * VisualAlphaAmount;
            rend.material.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }
    }
}

public class ParticulaFlotante : MonoBehaviour
{
    private const float MinimumSpeed = 0.8f;
    private const float MaximumSpeed = 1.8f;
    private const float MinimumAmplitude = 0.15f;
    private const float MaximumAmplitude = 0.35f;
    private const float RandomOffsetMaximum = 6.28f;
    private const float YRotationDegreesPerSecond = 45f;
    private const float XRotationDegreesPerSecond = 30f;

    private Vector3 initialPosition;
    private float speed;
    private float amplitude;
    private float offset;

    private void Start()
    {
        initialPosition = transform.localPosition;
        speed = UnityEngine.Random.Range(MinimumSpeed, MaximumSpeed);
        amplitude = UnityEngine.Random.Range(MinimumAmplitude, MaximumAmplitude);
        offset = UnityEngine.Random.Range(0f, RandomOffsetMaximum);
    }

    private void Update()
    {
        transform.localPosition = initialPosition + Vector3.up * Mathf.Sin(Time.time * speed + offset) * amplitude;
        transform.Rotate(Vector3.up, YRotationDegreesPerSecond * Time.deltaTime);
        transform.Rotate(Vector3.right, XRotationDegreesPerSecond * Time.deltaTime);
    }
}
