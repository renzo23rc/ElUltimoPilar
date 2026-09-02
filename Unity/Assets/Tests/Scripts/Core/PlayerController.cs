/**
 * PlayerController.cs
 * Movimiento, cámara, disparo básico y recolección de energía.
 * Usa el NUEVO Input System de Unity (configurado en Player Settings).
 * Lee input directamente en Update - NO requiere PlayerInput component.
 * 
 * Colocar en el GameObject del jugador.
 * Requiere: CharacterController y una cámara hija.
 */
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IPlayerRosterMember
{
    [Header("Movimiento")]
    public float velocidadMovimiento = 8f;
    public float gravedad = -20f;
    public float sensibilidadMouse = 0.5f;
    [Header("Salto")]
    public float alturaSalto = 1.8f;
    public float coyoteTime = 0.12f;
    private float tiempoEnAire = 0f;
    [Header("Zona Gravedad (auto)")]
    public bool enZonaGravedad = false;
    public float gravedadZona = -4f; // 80% menos
    public float impulsoZona = 6f; // empuje extra al entrar
    public float velocidadEnZona = 5f; // mas lento/flotante
    
    [Header("Vida")]
    public float vidaMaxima = 100f;
    public float vidaActual = 100f;
    
    [Header("Referencias")]
    public Camera camaraJugador;
    public Transform puntoDisparo;
    public LayerMask capaEnemigos;
    public LayerMask capaPilar;
    
    [Header("Munición por Arma")]
    public int municionDirecta = 80; // Sincronizado con WeaponSystem balanceo B1
    public int municionArea = 16;
    
    private CharacterController controller;
    private Vector3 velocidadVertical;
    private float rotacionX = 0f;
    
    // Componentes
    private EnergySystem energia;
    private WeaponSystem armas;

    [Header("Ralentización (Weaver)")]
    public bool estaRalentizado = false;
    private float velocidadOriginal = -1f;
    private float velocidadZonaOriginal = -1f;
    private Coroutine coRalentizacion;
    private float factorRalentActual = 1f;

    void OnEnable()
    {
        GameManager.Instance?.RegisterPlayer(this);
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        energia = GetComponent<EnergySystem>();
        armas = GetComponent<WeaponSystem>();
        
        if (camaraJugador == null)
            camaraJugador = GetComponentInChildren<Camera>();
        if (puntoDisparo == null)
            puntoDisparo = camaraJugador?.transform;
            
        Cursor.lockState = CursorLockMode.Locked;
        GameManager.Instance?.RegisterPlayer(this);
    }

    void OnDisable()
    {
        GameManager.Instance?.UnregisterPlayer(this);
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.juegoActivo) return;

        if (estaDerribado)
        {
            ManejarEstadoDerribado();
            // Aún aplicar gravedad suave para que no flote derribado
            if (controller != null && controller.enabled)
                ManejarGravedad();
            return;
        }
        
        LeerInput();
        ManejarGravedad();
        ManejarDisparo();
        ManejarReanimacionCoop();
    }

    void LeerInput()
    {
        if (estaDerribado) return; // Bloqueo de controles cuando derribado
        if (Keyboard.current == null || Mouse.current == null) return;
        
        // ========== MOVIMIENTO WASD ==========
        Vector2 inputMovimiento = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) inputMovimiento.y += 1;
        if (Keyboard.current.sKey.isPressed) inputMovimiento.y -= 1;
        if (Keyboard.current.aKey.isPressed) inputMovimiento.x -= 1;
        if (Keyboard.current.dKey.isPressed) inputMovimiento.x += 1;
        
        // ========== LOOK (MOUSE) ==========
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * sensibilidadMouse;
        rotacionX -= mouseDelta.y;
        rotacionX = Mathf.Clamp(rotacionX, -80f, 80f);
        
        if (camaraJugador != null)
        {
            camaraJugador.transform.localRotation = Quaternion.Euler(rotacionX, 0, 0);
            transform.Rotate(Vector3.up * mouseDelta.x);
        }
        
        // ========== MOVIMIENTO CON CÁMARA ==========
        Vector3 forward = camaraJugador.transform.forward;
        Vector3 right = camaraJugador.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        float vel = enZonaGravedad ? velocidadEnZona : velocidadMovimiento;
        Vector3 movimiento = (forward * inputMovimiento.y + right * inputMovimiento.x) * vel;
        // Flotacion extra en zona: movimiento mas esponjoso
        if (enZonaGravedad) movimiento.y += Mathf.Sin(Time.time * 2.5f) * 0.8f * Time.deltaTime * 60f;
        controller.Move(movimiento * Time.deltaTime);
        
        // ========== SALTO (Espacio) ==========
        bool quiereSaltar = Keyboard.current.spaceKey.wasPressedThisFrame 
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        if (quiereSaltar)
        {
            IntentarSaltar();
        }

        // ========== ACCIONES ==========
        if (Keyboard.current.hKey.wasPressedThisFrame && energia != null)
        {
            energia.GastarEnCuracion();
        }
        if (Keyboard.current.jKey.wasPressedThisFrame && energia != null)
        {
            energia.ActivarHabilidad();
        }
    }

    void ManejarGravedad()
    {
        bool enSuelo = controller.isGrounded;
        float gravActual = enZonaGravedad ? gravedadZona : gravedad;
        if (enSuelo)
        {
            tiempoEnAire = 0f;
            if (velocidadVertical.y < 0)
                velocidadVertical.y = enZonaGravedad ? -0.5f : -2f; // flotar en zona
        }
        else
        {
            tiempoEnAire += Time.deltaTime;
            velocidadVertical.y += gravActual * Time.deltaTime;
            // En zona, limitar caida y añadir flotacion exagerada
            if (enZonaGravedad)
            {
                velocidadVertical.y = Mathf.Max(velocidadVertical.y, -3f);
                velocidadVertical.y += Mathf.Sin(Time.time * 3f) * 0.15f;
            }
        }
        controller.Move(velocidadVertical * Time.deltaTime);
    }

    // Llamado por ZonaGravedadEffect
    public void EntrarZonaGravedad()
    {
        if (enZonaGravedad) return;
        enZonaGravedad = true;
        velocidadVertical.y = impulsoZona; // impulso hacia arriba exagerado
        Debug.Log("[Player] ¡Entraste en zona de gravedad alterada! Gravedad reducida + impulso");
        // FOV exagerado
        if (camaraJugador != null) camaraJugador.fieldOfView = 75f;
    }
    public void SalirZonaGravedad()
    {
        if (!enZonaGravedad) return;
        enZonaGravedad = false;
        Debug.Log("[Player] Saliste de zona de gravedad");
        if (camaraJugador != null) camaraJugador.fieldOfView = 60f;
    }

    void IntentarSaltar()
    {
        bool enSuelo = controller.isGrounded;
        bool puedeSaltar = enSuelo || tiempoEnAire < coyoteTime || transform.position.y <= 1.2f;
        Debug.Log($"[Player] Intento salto: enSuelo={enSuelo} tiempoEnAire={tiempoEnAire:F2} puede={puedeSaltar} y={transform.position.y:F2} vY={velocidadVertical.y:F2} enZona={enZonaGravedad}");
        if (!puedeSaltar)
        {
            Debug.LogWarning("[Player] Salto bloqueado - no en suelo");
            return;
        }

        float gravParaSalto = enZonaGravedad ? gravedadZona : gravedad;
        float altura = enZonaGravedad ? alturaSalto * 2.2f : alturaSalto; // salto 2.2x en zona (exagerado)
        velocidadVertical.y = Mathf.Sqrt(altura * -2f * gravParaSalto);
        tiempoEnAire = 999f; // evitar doble salto
        Debug.Log($"[Player] ¡Salto! vY={velocidadVertical.y:F1} (zona={enZonaGravedad})");
    }

    void ManejarDisparo()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Disparar();
        }
    }

    void Disparar()
    {
        if (armas != null)
        {
            armas.DispararActual();
            return;
        }
        
        // Fallback básico si no hay WeaponSystem
        if (puntoDisparo != null && Physics.Raycast(puntoDisparo.position, puntoDisparo.forward, out RaycastHit hit, 100f, capaEnemigos))
        {
            var enemy = hit.collider.GetComponent<Enemy>();
            enemy?.RecibirDaño(10f);
            
            Debug.DrawRay(puntoDisparo.position, puntoDisparo.forward * hit.distance, Color.red, 0.5f);
        }
    }

    public void RecibirDaño(float cantidad)
    {
        if (estaDerribado) return;
        vidaActual = Mathf.Max(0, vidaActual - cantidad);
        Debug.Log($"[Player] Daño recibido: {cantidad}. Vida: {vidaActual}/{vidaMaxima}");
        
        if (vidaActual <= 0)
        {
            EntrarDerribado();
        }
    }

    public void Curar(float cantidad)
    {
        if (estaDerribado) return;
        vidaActual = Mathf.Min(vidaMaxima, vidaActual + cantidad);
    }

    // ===== SISTEMA DERRIBADO / REANIMACIÓN CO-OP =====
    [Header("Estado Derribado (co-op)")]
    public bool estaDerribado = false;
    public float vidaAlRevivir = 50f;
    public float rangoReanimacion = 3f;
    public Key reanimarKey = Key.E;

    public event System.Action<PlayerController> OnDerribado;
    public event System.Action<PlayerController> OnReanimado;
    public bool IsDowned => estaDerribado;

    public void EntrarDerribado()
    {
        if (estaDerribado) return;
        estaDerribado = true;
        vidaActual = 0;
        // Bloqueo de disparo y movimiento ya via early return
        // Quitar ralentización pendiente
        if (estaRalentizado) QuitarRalentizacion();
        Debug.Log($"[Player] {name} DERRIBADO - Esperando reanimación (E a {rangoReanimacion}m)");
        OnDerribado?.Invoke(this);
        // Notificar GameManager para chequear derrota co-op
        GameManager.Instance?.NotificarJugadorDerribado(this);
        // Feedback HUD via evento, TestHUD lo escuchará
    }

    public void CaerEnPozo(Vector3 pozoPos)
    {
        if (estaDerribado) return;
        Debug.Log($"[Player] {name} cayendo al pozo en {pozoPos} - instakill con caída");
        // Animación de caída: deshabilitar controller momentáneamente y mover hacia abajo
        StartCoroutine(RutinaCaidaPozo(pozoPos));
    }

    System.Collections.IEnumerator RutinaCaidaPozo(Vector3 pozoPos)
    {
        // Desactivar movimiento por 0.6s y animar caída vertical
        float t = 0f;
        float dur = 0.6f;
        Vector3 ini = transform.position;
        // Bloquear input durante caída
        estaDerribado = true; // temporal para bloquear LeerInput
        // Pequeño impulso hacia centro del pozo
        Vector3 dirCentro = (pozoPos - transform.position);
        dirCentro.y = 0;
        dirCentro = dirCentro.normalized * 2f;
        // Si CharacterController está activo, mover con Move
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            Vector3 caida = Vector3.Lerp(ini, ini + dirCentro + Vector3.down * 6f, p);
            if (controller != null && controller.enabled)
            {
                Vector3 delta = caida - transform.position;
                controller.Move(delta);
            }
            else
            {
                transform.position = caida;
            }
            yield return null;
        }
        // Ahora estado derribado definitivo
        vidaActual = 0;
        // Asegurar notificación si no estaba ya
        if (!OnDerribado.GetInvocationList().Length.Equals(0)) {}
        OnDerribado?.Invoke(this);
        GameManager.Instance?.NotificarJugadorDerribado(this);
        Debug.Log($"[Player] {name} derribado por pozo");
    }

    public void Reanimar()
    {
        if (!estaDerribado) return;
        estaDerribado = false;
        vidaActual = vidaAlRevivir;
        velocidadVertical.y = 0;
        // Pequeña invulnerabilidad visual (parpadeo no implementado, solo log)
        Debug.Log($"[Player] {name} REANIMADO con {vidaActual} vida!");
        OnReanimado?.Invoke(this);
        GameManager.Instance?.NotificarJugadorReanimado(this);
    }

    void ManejarEstadoDerribado()
    {
        // Solo mostrar mensaje pulsante en HUD, sin movimiento
        // El jugador derribado no puede moverse ni disparar
    }

    void ManejarReanimacionCoop()
    {
        if (Keyboard.current == null) return;
        // Si estoy vivo, buscar aliados derribados cerca y pulsar E para reanimar
        if (estaDerribado) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        var gameManager = GameManager.Instance;
        if (gameManager == null) return;

        PlayerController objetivo = null;
        float minDist = float.MaxValue;
        foreach (var p in gameManager.Players)
        {
            if (p == this || !p.estaDerribado) continue;
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d <= rangoReanimacion && d < minDist)
            {
                minDist = d;
                objetivo = p;
            }
        }
        if (objetivo != null)
        {
            Debug.Log($"[Player] Reanimando a {objetivo.name} a {minDist:F1}m");
            objetivo.Reanimar();
        }
    }

    public void ReplenishWaveAmmo()
    {
        ReponerMunicion();
    }

    public void ReponerMunicion()
    {
        municionDirecta = 80;
        municionArea = 16;
        if (armas == null) armas = GetComponent<WeaponSystem>();
        armas?.ReponerMunicion();
        Debug.Log("[Player] Munición repuesta al final de oleada");
    }

    // ===== SISTEMA DE RALENTIZACIÓN (stack prohibido, 0.5/8s, afecta todos) =====
    public void AplicarRalentizacion(float factor, float duracion)
    {
        if (estaRalentizado)
        {
            // Stack prohibido: refrescar timer sin multiplicar
            if (coRalentizacion != null) StopCoroutine(coRalentizacion);
            coRalentizacion = StartCoroutine(RutinaRalentizacion(factor, duracion));
            return;
        }
        velocidadOriginal = velocidadMovimiento;
        velocidadZonaOriginal = velocidadEnZona;
        factorRalentActual = factor;
        velocidadMovimiento = velocidadOriginal * factor;
        velocidadEnZona = velocidadZonaOriginal * factor;
        estaRalentizado = true;
        Debug.Log($"[Player] Ralentizado x{factor} por {duracion}s (vel {velocidadOriginal:F1}->{velocidadMovimiento:F1})");
        coRalentizacion = StartCoroutine(RutinaRalentizacion(factor, duracion));
    }

    public void QuitarRalentizacion()
    {
        if (!estaRalentizado) return;
        if (coRalentizacion != null) StopCoroutine(coRalentizacion);
        coRalentizacion = null;
        velocidadMovimiento = velocidadOriginal;
        velocidadEnZona = velocidadZonaOriginal;
        estaRalentizado = false;
        factorRalentActual = 1f;
        velocidadOriginal = -1f;
        velocidadZonaOriginal = -1f;
        Debug.Log("[Player] Ralentización removida");
    }

    System.Collections.IEnumerator RutinaRalentizacion(float factor, float duracion)
    {
        yield return new WaitForSeconds(duracion);
        if (estaRalentizado)
        {
            velocidadMovimiento = velocidadOriginal;
            velocidadEnZona = velocidadZonaOriginal;
            estaRalentizado = false;
            factorRalentActual = 1f;
            velocidadOriginal = -1f;
            velocidadZonaOriginal = -1f;
            coRalentizacion = null;
            Debug.Log("[Player] Ralentización expirada");
        }
    }

    void OnDestroy()
    {
        if (coRalentizacion != null) StopCoroutine(coRalentizacion);
    }
}
