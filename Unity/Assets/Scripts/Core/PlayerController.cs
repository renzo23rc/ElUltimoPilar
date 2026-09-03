/**
 * PlayerController.cs
 * Movimiento, cámara, disparo básico y recolección de energía.
 * Usa el NUEVO Input System de Unity (configurado en Player Settings).
 * Lee snapshots de comando desde el PlayerInputAdapter del jugador.
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
    private PlayerInput playerInput;
    private PlayerInputAdapter inputAdapter;

    [Header("Ralentización (Weaver)")]
    public bool estaRalentizado = false;
    private float velocidadOriginal = -1f;
    private float velocidadZonaOriginal = -1f;
    private Coroutine coRalentizacion;
    private Coroutine fallCoroutine;
    private float factorRalentActual = 1f;

    private float velocidadMovimientoInicial;
    private float velocidadZonaInicial;
    private int municionDirectaInicial;
    private int municionAreaInicial;
    private float fovCamaraInicial;
    private Quaternion rotacionCamaraInicial;
    private bool estadoInicialCapturado;
    private bool estadoCamaraInicialCapturado;

    void Awake()
    {
        ResolverReferencias();
        CapturarEstadoInicial();
    }

    void OnEnable()
    {
        ResolverReferencias();
        inputAdapter?.Enable();
        GameManager.Instance?.RegisterPlayer(this);
    }

    void Start()
    {
        ResolverReferencias();
        CapturarEstadoInicial();
        inputAdapter?.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        GameManager.Instance?.RegisterPlayer(this);
    }

    void ResolverReferencias()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (energia == null) energia = GetComponent<EnergySystem>();
        if (armas == null) armas = GetComponent<WeaponSystem>();
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        if (inputAdapter == null && playerInput != null)
            inputAdapter = new PlayerInputAdapter(playerInput);
        if (camaraJugador == null) camaraJugador = GetComponentInChildren<Camera>();
        if (puntoDisparo == null) puntoDisparo = camaraJugador?.transform;
    }

    void CapturarEstadoInicial()
    {
        if (!estadoInicialCapturado)
        {
            velocidadMovimientoInicial = velocidadMovimiento;
            velocidadZonaInicial = velocidadEnZona;
            municionDirectaInicial = municionDirecta;
            municionAreaInicial = municionArea;
            estadoInicialCapturado = true;
        }

        if (!estadoCamaraInicialCapturado && camaraJugador != null)
        {
            fovCamaraInicial = camaraJugador.fieldOfView;
            rotacionCamaraInicial = camaraJugador.transform.localRotation;
            estadoCamaraInicialCapturado = true;
        }
    }

    void OnDisable()
    {
        inputAdapter?.Disable();
        GameManager.Instance?.UnregisterPlayer(this);
    }

    void Update()
    {
        PlayerCommand command = LeerComando();
        EmitirComando(command);

        GameManager gameManager = GameManager.Instance;
        bool juegoEstabaActivo = gameManager != null && gameManager.juegoActivo;
        if (!juegoEstabaActivo) return;
        if (gameManager.juegoPausado) return;

        if (estaDerribado)
        {
            ManejarEstadoDerribado();
            // Aún aplicar gravedad suave para que no flote derribado
            if (controller != null && controller.enabled)
                ManejarGravedad();
            return;
        }

        ManejarMirada(command);
        ManejarMovimiento(command);
        ManejarSalto(command);
        ManejarCuracion(command);
        ManejarHabilidad(command);
        ManejarGravedad();
        ManejarDisparo(command);
        ManejarInteraccion(command);
    }

    PlayerCommand LeerComando()
    {
        if (inputAdapter == null) return default(PlayerCommand);
        return inputAdapter.CurrentCommand;
    }

    void EmitirComando(PlayerCommand command)
    {
        OnCommandIssued?.Invoke(this, command);
    }

    void ManejarMirada(PlayerCommand command)
    {
        Vector2 lookDelta = new Vector2(command.LookX, command.LookY) * sensibilidadMouse;
        rotacionX -= lookDelta.y;
        rotacionX = Mathf.Clamp(rotacionX, -80f, 80f);

        if (camaraJugador != null)
        {
            camaraJugador.transform.localRotation = Quaternion.Euler(rotacionX, 0, 0);
            transform.Rotate(Vector3.up * lookDelta.x);
        }
    }

    void ManejarMovimiento(PlayerCommand command)
    {
        if (controller == null) return;

        Vector2 inputMovimiento = new Vector2(command.MoveX, command.MoveY);
        Transform referenciaMovimiento = camaraJugador != null ? camaraJugador.transform : transform;
        Vector3 forward = referenciaMovimiento.forward;
        Vector3 right = referenciaMovimiento.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        float vel = enZonaGravedad ? velocidadEnZona : velocidadMovimiento;
        Vector3 movimiento = (forward * inputMovimiento.y + right * inputMovimiento.x) * vel;
        // Flotacion extra en zona: movimiento mas esponjoso
        if (enZonaGravedad) movimiento.y += Mathf.Sin(Time.time * 2.5f) * 0.8f * Time.deltaTime * 60f;
        controller.Move(movimiento * Time.deltaTime);
    }

    void ManejarSalto(PlayerCommand command)
    {
        if (command.Jump)
            IntentarSaltar();
    }

    void ManejarCuracion(PlayerCommand command)
    {
        if (command.Heal && energia != null)
            energia.GastarEnCuracion();
    }

    void ManejarHabilidad(PlayerCommand command)
    {
        if (command.Ability && energia != null)
            energia.ActivarHabilidad();
    }

    void ManejarInteraccion(PlayerCommand command)
    {
        ManejarReanimacionCoop(command);
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
        if (camaraJugador != null)
        {
            CapturarEstadoInicial();
            if (estadoCamaraInicialCapturado)
                camaraJugador.fieldOfView = fovCamaraInicial;
        }
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

    void ManejarDisparo(PlayerCommand command)
    {
        if (armas != null)
        {
            armas.ConsumeCommand(command);
            return;
        }

        if (command.Fire)
            Disparar();
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
    public event Action<PlayerController, PlayerCommand> OnCommandIssued;
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
        // GameManager observa este evento a través de la frontera de jugadores registrados.
        // Feedback HUD via evento, Hud lo escuchará
    }

    public void CaerEnPozo(Vector3 pozoPos)
    {
        if (estaDerribado) return;
        Debug.Log($"[Player] {name} cayendo al pozo en {pozoPos} - instakill con caída");
        // Animación de caída: deshabilitar controller momentáneamente y mover hacia abajo
        fallCoroutine = StartCoroutine(RutinaCaidaPozo(pozoPos));
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
        OnDerribado?.Invoke(this);
        Debug.Log($"[Player] {name} derribado por pozo");
        fallCoroutine = null;

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

    public void ResetState()
    {
        ResolverReferencias();
        CapturarEstadoInicial();

        if (fallCoroutine != null)
        {
            StopCoroutine(fallCoroutine);
            fallCoroutine = null;
        }
        if (coRalentizacion != null)
        {
            StopCoroutine(coRalentizacion);
            coRalentizacion = null;
        }

        velocidadMovimiento = velocidadMovimientoInicial;
        velocidadEnZona = velocidadZonaInicial;
        estaRalentizado = false;
        factorRalentActual = 1f;
        velocidadOriginal = -1f;
        velocidadZonaOriginal = -1f;

        vidaActual = vidaMaxima;
        estaDerribado = false;
        enZonaGravedad = false;
        tiempoEnAire = 0f;
        velocidadVertical = Vector3.zero;

        municionDirecta = armas?.armaDirecta?.municionMaxima ?? municionDirectaInicial;
        municionArea = armas?.armaArea?.municionMaxima ?? municionAreaInicial;

        rotacionX = 0f;
        if (camaraJugador != null && estadoCamaraInicialCapturado)
        {
            camaraJugador.fieldOfView = fovCamaraInicial;
            camaraJugador.transform.localRotation = rotacionCamaraInicial;
            rotacionX = rotacionCamaraInicial.eulerAngles.x;
            if (rotacionX > 180f) rotacionX -= 360f;
        }
    }

    void ManejarEstadoDerribado()
    {
        // Solo mostrar mensaje pulsante en HUD, sin movimiento
        // El jugador derribado no puede moverse ni disparar
    }

    void ManejarReanimacionCoop(PlayerCommand command)
    {
        // Si estoy vivo, buscar aliados derribados cerca y usar la acción Interact para reanimar.
        if (estaDerribado || !command.Interact) return;

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
        ResolverReferencias();
        if (armas != null)
        {
            armas.ReponerMunicion();
            municionDirecta = armas.armaDirecta?.municionActual ?? municionDirecta;
            municionArea = armas.armaArea?.municionActual ?? municionArea;
        }
        else
        {
            municionDirecta = municionDirectaInicial;
            municionArea = municionAreaInicial;
        }
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
        if (fallCoroutine != null) StopCoroutine(fallCoroutine);
        if (coRalentizacion != null) StopCoroutine(coRalentizacion);
    }
}
