/**
 * PlayerController.cs
 * Movimiento, cámara, estado derribado/reanimación y ralentización del jugador.
 * Usa el NUEVO Input System de Unity (configurado en Player Settings).
 * Lee snapshots de comando desde el PlayerInputAdapter del jugador.
 *
 * Colocar en el GameObject del jugador.
 * Requiere: CharacterController y una cámara hija.
 */
using System;
using System.Collections;
using UltimoPilar.Core.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IPlayerRosterMember
{
    private const float MinimumHealth = 0f;
    private const float MaximumHealth = 100f;
    private const float MaximumLookAngleDegrees = 80f;
    private const float HalfTurnDegrees = 180f;
    private const float FullTurnDegrees = 360f;
    private const float GroundedVerticalSpeed = -2f;
    private const float GravityZoneGroundedSpeed = -0.5f;
    private const float GravityZoneMaximumFallSpeed = -3f;
    private const float GroundHeightMeters = 1.2f;
    private const float ZoneJumpMultiplier = 2.2f;
    private const float DoubleJumpGuardSeconds = 999f;
    private const float GravityZoneFieldOfViewDegrees = 75f;
    private const float ZoneBobFrequencyHertz = 2.5f;
    private const float ZoneBobSpeedMetersPerSecond = 0.8f;
    private const float ZoneFloatFrequencyHertz = 3f;
    private const float ZoneFloatAccelerationMetersPerSecondSquared = 9f;
    private const float FallDurationSeconds = 0.6f;
    private const float FallCenterImpulseMeters = 2f;
    private const float FallDistanceMeters = 6f;
    private const string GamepadControlSchemeName = "Gamepad";

    private static readonly object UnattributedSlowdownSource = new object();

    [Header("Movimiento")]
    public float velocidadMovimiento = 8f;
    public float gravedad = -20f;
    public float sensibilidadMouse = 0.5f;
    [Tooltip("Velocidad de giro de la cámara con el stick derecho, en grados por segundo a fondo.")]
    [SerializeField, Min(0f)] private float gamepadLookSpeedDegreesPerSecond = 180f;
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
    public float vidaMaxima = MaximumHealth;
    public float vidaActual = MaximumHealth;

    [Header("Referencias")]
    public Camera camaraJugador;
    public Transform puntoDisparo;
    public LayerMask capaEnemigos;
    public LayerMask capaPilar;

    [Header("Munición por Arma")]
    public int municionDirecta = 80; // Espejo de WeaponSystem, que es la fuente de verdad.
    public int municionArea = 16;

    private CharacterController controller;
    private Vector3 velocidadVertical;
    private float rotacionX = 0f;

    // Componentes
    private EnergySystem energia;
    private WeaponSystem armas;
    private PlayerInput playerInput;
    private PlayerInputAdapter inputAdapter;

    [Header("Ralentización")]
    public bool estaRalentizado = false;
    private readonly SlowdownTracker slowdowns = new SlowdownTracker();
    private Coroutine fallCoroutine;

    private int municionDirectaInicial;
    private int municionAreaInicial;
    private float fovCamaraInicial;
    private Quaternion rotacionCamaraInicial;
    private Vector3 posicionAparicion;
    private Quaternion rotacionAparicion;
    private bool estadoInicialCapturado;
    private bool estadoCamaraInicialCapturado;

    // ===== SISTEMA DERRIBADO / REANIMACIÓN CO-OP =====
    [Header("Estado Derribado (co-op)")]
    public bool estaDerribado = false;
    public float vidaAlRevivir = 50f;
    public float rangoReanimacion = 3f;
    public Key reanimarKey = Key.E;

    public event Action<PlayerController> OnDerribado;
    public event Action<PlayerController> OnReanimado;
    public event Action<PlayerController, PlayerCommand> OnCommandIssued;

    /// <summary>Gets whether the player is downed.</summary>
    public bool IsDowned => estaDerribado;

    /// <summary>Gets the position where the player appears and returns after a pit fall or restart.</summary>
    public Vector3 SpawnPosition => posicionAparicion;

    private float FactorRalentizacion => slowdowns.GetFactor(Time.time);

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

    void OnDisable()
    {
        inputAdapter?.Disable();
        GameManager.Instance?.UnregisterPlayer(this);
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
        if (!MuzzleTransformResolver.IsUsable(puntoDisparo, camaraJugador))
            puntoDisparo = MuzzleTransformResolver.Ensure(transform, camaraJugador);
    }

    void CapturarEstadoInicial()
    {
        if (!estadoInicialCapturado)
        {
            municionDirectaInicial = municionDirecta;
            municionAreaInicial = municionArea;
            posicionAparicion = transform.position;
            rotacionAparicion = transform.rotation;
            estadoInicialCapturado = true;
        }

        if (!estadoCamaraInicialCapturado && camaraJugador != null)
        {
            fovCamaraInicial = camaraJugador.fieldOfView;
            rotacionCamaraInicial = camaraJugador.transform.localRotation;
            estadoCamaraInicialCapturado = true;
        }
    }

    void Update()
    {
        PlayerCommand command = LeerComando();
        OnCommandIssued?.Invoke(this, command);

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || !gameManager.juegoActivo || gameManager.juegoPausado) return;

        estaRalentizado = slowdowns.IsActive(Time.time);
        if (estaDerribado)
        {
            // Aún aplicar gravedad para que no flote derribado.
            if (controller != null && controller.enabled && fallCoroutine == null)
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
        ManejarReanimacionCoop(command);
    }

    PlayerCommand LeerComando()
    {
        return inputAdapter == null ? default(PlayerCommand) : inputAdapter.CurrentCommand;
    }

    void ManejarMirada(PlayerCommand command)
    {
        Vector2 lookDelta = new Vector2(command.LookX, command.LookY) * EscalaDeMirada();
        rotacionX = Mathf.Clamp(rotacionX - lookDelta.y, -MaximumLookAngleDegrees, MaximumLookAngleDegrees);

        if (camaraJugador != null)
        {
            camaraJugador.transform.localRotation = Quaternion.Euler(rotacionX, 0, 0);
            transform.Rotate(Vector3.up * lookDelta.x);
            if (puntoDisparo != null)
                puntoDisparo.rotation = camaraJugador.transform.rotation;
        }
    }

    // El mouse entrega un desplazamiento por frame; el stick, una posición entre -1 y 1 que debe
    // convertirse en velocidad (grados por segundo) para no depender de los FPS.
    float EscalaDeMirada()
    {
        bool usaGamepad = playerInput != null && playerInput.currentControlScheme == GamepadControlSchemeName;
        return usaGamepad ? gamepadLookSpeedDegreesPerSecond * Time.deltaTime : sensibilidadMouse;
    }

    void ManejarMovimiento(PlayerCommand command)
    {
        if (controller == null) return;

        Transform referenciaMovimiento = camaraJugador != null ? camaraJugador.transform : transform;
        Vector3 forward = referenciaMovimiento.forward;
        Vector3 right = referenciaMovimiento.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        float velocidadBase = enZonaGravedad ? velocidadEnZona : velocidadMovimiento;
        Vector3 movimiento = (forward * command.MoveY + right * command.MoveX) * (velocidadBase * FactorRalentizacion);
        // Flotación en zona: vaivén vertical expresado como velocidad (independiente de los FPS).
        if (enZonaGravedad)
            movimiento.y += Mathf.Sin(Time.time * ZoneBobFrequencyHertz) * ZoneBobSpeedMetersPerSecond;
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

    void ManejarGravedad()
    {
        float gravActual = enZonaGravedad ? gravedadZona : gravedad;
        if (controller.isGrounded)
        {
            tiempoEnAire = 0f;
            if (velocidadVertical.y < 0)
                velocidadVertical.y = enZonaGravedad ? GravityZoneGroundedSpeed : GroundedVerticalSpeed;
        }
        else
        {
            tiempoEnAire += Time.deltaTime;
            velocidadVertical.y += gravActual * Time.deltaTime;
            // En zona, limitar la caída y sumar una flotación oscilante.
            if (enZonaGravedad)
            {
                velocidadVertical.y = Mathf.Max(velocidadVertical.y, GravityZoneMaximumFallSpeed);
                velocidadVertical.y += Mathf.Sin(Time.time * ZoneFloatFrequencyHertz)
                    * ZoneFloatAccelerationMetersPerSecondSquared * Time.deltaTime;
            }
        }

        controller.Move(velocidadVertical * Time.deltaTime);
    }

    // Llamado por ZonaGravedadEffect
    public void EntrarZonaGravedad()
    {
        if (enZonaGravedad) return;
        enZonaGravedad = true;
        velocidadVertical.y = impulsoZona;
        if (camaraJugador != null) camaraJugador.fieldOfView = GravityZoneFieldOfViewDegrees;
    }

    public void SalirZonaGravedad()
    {
        if (!enZonaGravedad) return;
        enZonaGravedad = false;
        RestaurarCampoDeVision();
    }

    void IntentarSaltar()
    {
        // El respaldo por altura cubre isGrounded inestable, pero solo cayendo: evita saltos en el aire al subir.
        bool cercaDelSuelo = transform.position.y <= GroundHeightMeters && velocidadVertical.y <= 0f;
        bool puedeSaltar = controller.isGrounded || tiempoEnAire < coyoteTime || cercaDelSuelo;
        if (!puedeSaltar) return;

        float gravParaSalto = enZonaGravedad ? gravedadZona : gravedad;
        float altura = enZonaGravedad ? alturaSalto * ZoneJumpMultiplier : alturaSalto;
        velocidadVertical.y = Mathf.Sqrt(altura * -2f * gravParaSalto);
        tiempoEnAire = DoubleJumpGuardSeconds;
    }

    void ManejarDisparo(PlayerCommand command)
    {
        armas?.ConsumeCommand(command);
    }

    public void RecibirDaño(float cantidad)
    {
        if (estaDerribado) return;
        vidaActual = Mathf.Max(MinimumHealth, vidaActual - cantidad);

        if (vidaActual <= MinimumHealth)
            EntrarDerribado();
    }

    public void Curar(float cantidad)
    {
        if (estaDerribado) return;
        vidaActual = Mathf.Min(vidaMaxima, vidaActual + cantidad);
    }

    public void EntrarDerribado()
    {
        if (estaDerribado) return;
        estaDerribado = true;
        CompletarDerribo();
    }

    void CompletarDerribo()
    {
        vidaActual = MinimumHealth;
        QuitarRalentizacion();
        Debug.Log($"[Player] {name} DERRIBADO - Esperando reanimación ({rangoReanimacion}m)");
        OnDerribado?.Invoke(this);
    }

    public void CaerEnPozo(Vector3 pozoPos)
    {
        if (estaDerribado) return;
        fallCoroutine = StartCoroutine(RutinaCaidaPozo(pozoPos));
    }

    IEnumerator RutinaCaidaPozo(Vector3 pozoPos)
    {
        // Bloquea input y daño durante la animación de caída.
        estaDerribado = true;
        Vector3 inicio = transform.position;
        Vector3 dirCentro = pozoPos - inicio;
        dirCentro.y = 0;
        Vector3 destino = inicio + dirCentro.normalized * FallCenterImpulseMeters + Vector3.down * FallDistanceMeters;

        float t = 0f;
        while (t < FallDurationSeconds)
        {
            t += Time.deltaTime;
            Vector3 caida = Vector3.Lerp(inicio, destino, t / FallDurationSeconds);
            if (controller != null && controller.enabled)
                controller.Move(caida - transform.position);
            else
                transform.position = caida;
            yield return null;
        }

        // Queda derribado en su punto de aparición, fuera del pozo, para que un aliado pueda reanimarlo.
        TeletransportarA(posicionAparicion, transform.rotation);
        fallCoroutine = null;
        CompletarDerribo();
    }

    public void Reanimar()
    {
        if (!estaDerribado) return;
        estaDerribado = false;
        vidaActual = vidaAlRevivir;
        velocidadVertical.y = 0;
        OnReanimado?.Invoke(this);
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

        QuitarRalentizacion();
        vidaActual = vidaMaxima;
        estaDerribado = false;
        enZonaGravedad = false;
        tiempoEnAire = 0f;
        velocidadVertical = Vector3.zero;
        TeletransportarA(posicionAparicion, rotacionAparicion);

        municionDirecta = armas?.armaDirecta?.municionMaxima ?? municionDirectaInicial;
        municionArea = armas?.armaArea?.municionMaxima ?? municionAreaInicial;

        rotacionX = 0f;
        if (camaraJugador != null && estadoCamaraInicialCapturado)
        {
            camaraJugador.transform.localRotation = rotacionCamaraInicial;
            rotacionX = rotacionCamaraInicial.eulerAngles.x;
            if (rotacionX > HalfTurnDegrees) rotacionX -= FullTurnDegrees;
        }

        RestaurarCampoDeVision();
    }

    void RestaurarCampoDeVision()
    {
        CapturarEstadoInicial();
        if (camaraJugador != null && estadoCamaraInicialCapturado)
            camaraJugador.fieldOfView = fovCamaraInicial;
    }

    void TeletransportarA(Vector3 posicion, Quaternion rotacion)
    {
        // El CharacterController pisa las asignaciones directas de transform si queda habilitado.
        bool controllerHabilitado = controller != null && controller.enabled;
        if (controllerHabilitado) controller.enabled = false;
        transform.SetPositionAndRotation(posicion, rotacion);
        if (controllerHabilitado) controller.enabled = true;
    }

    void ManejarReanimacionCoop(PlayerCommand command)
    {
        // Si estoy vivo, reanimo al aliado derribado más cercano dentro del rango con la acción Interact.
        if (estaDerribado || !command.Interact) return;

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null) return;

        PlayerController objetivo = PlayerLocator.FindClosest(
            gameManager.Players,
            transform.position,
            IsDownedAlly,
            rangoReanimacion);
        objetivo?.Reanimar();
    }

    bool IsDownedAlly(PlayerController candidate)
    {
        return candidate != this && candidate.estaDerribado;
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
    }

    // ===== RALENTIZACIÓN (sin stack: gana la más fuerte, cada fuente se gestiona por separado) =====

    /// <summary>Applies an unattributed slowdown; reapplying refreshes it.</summary>
    public void AplicarRalentizacion(float factor, float duracion)
    {
        AplicarRalentizacion(UnattributedSlowdownSource, factor, duracion);
    }

    /// <summary>Applies or refreshes the slowdown owned by a source.</summary>
    /// <param name="fuente">The owner of the slowdown, such as a zone or an ability.</param>
    /// <param name="factor">The speed multiplier between 0 and 1.</param>
    /// <param name="duracion">The duration in seconds.</param>
    public void AplicarRalentizacion(object fuente, float factor, float duracion)
    {
        slowdowns.Apply(fuente, factor, duracion, Time.time);
        estaRalentizado = slowdowns.IsActive(Time.time);
    }

    /// <summary>Removes every active slowdown.</summary>
    public void QuitarRalentizacion()
    {
        slowdowns.Clear();
        estaRalentizado = false;
    }

    /// <summary>Removes only the slowdown owned by a source, keeping the others.</summary>
    /// <param name="fuente">The owner of the slowdown.</param>
    public void QuitarRalentizacion(object fuente)
    {
        slowdowns.Remove(fuente);
        estaRalentizado = slowdowns.IsActive(Time.time);
    }

    void OnDestroy()
    {
        if (fallCoroutine != null) StopCoroutine(fallCoroutine);
    }
}
