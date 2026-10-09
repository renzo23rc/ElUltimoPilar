/**
 * PlayerController.cs
 * Adaptador del jugador: lee un PlayerCommand por frame y lo reparte entre sus colaboradores
 * (PlayerMotor para moverse, PlayerCameraLook para mirar, WeaponSystem y EnergySystem).
 * Es dueño de la vida, el estado derribado/reanimación, la caída al pozo y la ralentización.
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
public class PlayerController : MonoBehaviour, IPlayerRosterMember, IDamageable, ISlowable
{
    private const float MinimumHealth = 0f;
    private const float MaximumHealth = 100f;
    private const float GravityZoneFieldOfViewDegrees = 75f;
    private const float FallDurationSeconds = 0.6f;
    private const float FallCenterImpulseMeters = 2f;
    private const float FallDistanceMeters = 6f;
    private const float PitRimMarginMeters = 1f;
    private const float GroundProbeHeightMeters = 10f;
    private const float GroundProbeDistanceMeters = 40f;
    private const float WalkableMinimumNormalY = 0.7f;
    private const float GroundSkinMeters = 0.05f;
    private const float HalfFactor = 0.5f;
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
    [Header("Zona Gravedad (auto)")]
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

    [Header("Ralentización")]
    public bool estaRalentizado = false;

    // ===== SISTEMA DERRIBADO / REANIMACIÓN CO-OP =====
    [Header("Estado Derribado (co-op)")]
    public bool estaDerribado = false;
    public float vidaAlRevivir = 50f;
    public float rangoReanimacion = 3f;
    public Key reanimarKey = Key.E;

    // Colaboradores
    private CharacterController controller;
    private PlayerMotor motor;
    private readonly PlayerCameraLook cameraLook = new PlayerCameraLook();
    private readonly SlowdownTracker slowdowns = new SlowdownTracker();
    private EnergySystem energia;
    private WeaponSystem armas;
    private PlayerInput playerInput;
    private PlayerInputAdapter inputAdapter;
    private Coroutine fallCoroutine;

    private int municionDirectaInicial;
    private int municionAreaInicial;
    private Vector3 posicionAparicion;
    private Quaternion rotacionAparicion;
    private bool estadoInicialCapturado;

    public event Action<PlayerController> OnDerribado;
    public event Action<PlayerController> OnReanimado;
    public event Action<PlayerController, PlayerCommand> OnCommandIssued;

    /// <summary>Gets whether the player is downed.</summary>
    public bool IsDowned => estaDerribado;

    /// <summary>Gets whether the player is floating inside a gravity zone.</summary>
    public bool enZonaGravedad => motor != null && motor.InGravityZone;

    /// <summary>Gets the defender role shown in the HUD (identity only for now).</summary>
    public DefenderRole Role { get; private set; }

    /// <summary>Gets whether a defender role was assigned.</summary>
    public bool HasRole { get; private set; }

    /// <summary>Gets the position where the player appears and returns after a pit fall or restart.</summary>
    public Vector3 SpawnPosition => posicionAparicion;

    private float FactorRalentizacion => slowdowns.GetFactor(Time.time);

    private PlayerMovementSettings MovementSettings => new PlayerMovementSettings(
        velocidadMovimiento,
        gravedad,
        alturaSalto,
        coyoteTime,
        gravedadZona,
        impulsoZona,
        velocidadEnZona);

    /// <summary>Assigns the defender role; it is kept for the rest of the session.</summary>
    /// <param name="role">The role to assign.</param>
    public void AssignRole(DefenderRole role)
    {
        Role = role;
        HasRole = true;
    }

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

    void OnDestroy()
    {
        if (fallCoroutine != null) StopCoroutine(fallCoroutine);
    }

    void ResolverReferencias()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (motor == null) motor = new PlayerMotor(controller, transform);
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

        cameraLook.CaptureInitialState(camaraJugador);
    }

    // ===== BUCLE POR FRAME =====

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
            if (motor.CanMove && fallCoroutine == null)
                motor.ApplyGravity(MovementSettings);
            return;
        }

        PlayerMovementSettings settings = MovementSettings;
        cameraLook.Apply(camaraJugador, transform, puntoDisparo, new Vector2(command.LookX, command.LookY) * EscalaDeMirada());
        Transform referenciaMovimiento = camaraJugador != null ? camaraJugador.transform : transform;
        motor.Move(referenciaMovimiento, command.MoveX, command.MoveY, settings, FactorRalentizacion);
        if (command.Jump) motor.TryJump(settings);
        if (command.Heal && energia != null) energia.GastarEnCuracion();
        if (command.Ability && energia != null) energia.ActivarHabilidad();
        motor.ApplyGravity(settings);
        armas?.ConsumeCommand(command);
        ManejarReanimacionCoop(command);
    }

    PlayerCommand LeerComando()
    {
        return inputAdapter == null ? default(PlayerCommand) : inputAdapter.CurrentCommand;
    }

    // El mouse entrega un desplazamiento por frame; el stick, una posición entre -1 y 1 que debe
    // convertirse en velocidad (grados por segundo) para no depender de los FPS.
    float EscalaDeMirada()
    {
        bool usaGamepad = playerInput != null && playerInput.currentControlScheme == GamepadControlSchemeName;
        return usaGamepad ? gamepadLookSpeedDegreesPerSecond * Time.deltaTime : sensibilidadMouse;
    }

    // ===== ZONA DE GRAVEDAD (llamado por ZonaGravedadEffect) =====

    public void EntrarZonaGravedad()
    {
        ResolverReferencias();
        if (motor.EnterGravityZone(MovementSettings))
            cameraLook.SetFieldOfView(camaraJugador, GravityZoneFieldOfViewDegrees);
    }

    public void SalirZonaGravedad()
    {
        ResolverReferencias();
        if (motor.ExitGravityZone())
            cameraLook.RestoreFieldOfView(camaraJugador);
    }

    // ===== VIDA, DERRIBO Y REANIMACIÓN =====

    public void RecibirDaño(float cantidad)
    {
        if (estaDerribado) return;
        vidaActual = Mathf.Max(MinimumHealth, vidaActual - cantidad);

        if (vidaActual <= MinimumHealth)
            EntrarDerribado();
    }

    void IDamageable.ReceiveDamage(DamageRequest request)
    {
        RecibirDaño(request.Amount);
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

    /// <summary>Starts the fall into a pit; the player ends downed at the start position.</summary>
    /// <param name="pozoPos">The center of the pit.</param>
    public void CaerEnPozo(Vector3 pozoPos)
    {
        CaerEnPozo(pozoPos, 0f);
    }

    /// <summary>Starts the fall into a pit; the player ends downed on the closest ground outside it.</summary>
    /// <param name="pozoPos">The center of the pit.</param>
    /// <param name="radioMortal">The horizontal radius of the pit; zero or less falls back to the start position.</param>
    public void CaerEnPozo(Vector3 pozoPos, float radioMortal)
    {
        if (estaDerribado) return;
        ResolverReferencias();
        fallCoroutine = StartCoroutine(RutinaCaidaPozo(pozoPos, radioMortal));
    }

    IEnumerator RutinaCaidaPozo(Vector3 pozoPos, float radioMortal)
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
            motor.MoveTo(Vector3.Lerp(inicio, destino, t / FallDurationSeconds));
            yield return null;
        }

        // Queda derribado en el piso más cercano fuera del pozo, para que un aliado pueda reanimarlo.
        motor.Teleport(BuscarReaparicionFueraDelPozo(pozoPos, radioMortal, inicio), transform.rotation);
        fallCoroutine = null;
        CompletarDerribo();
    }

    // Borde del pozo del lado por el que cayó; sin piso ahí (o sin radio) vuelve al punto de aparición.
    Vector3 BuscarReaparicionFueraDelPozo(Vector3 pozoPos, float radioMortal, Vector3 inicio)
    {
        if (radioMortal <= 0f) return posicionAparicion;

        Vector3 borde = PitRespawnPoint.FindRimPosition(pozoPos, radioMortal, inicio, posicionAparicion - pozoPos, PitRimMarginMeters);
        Vector3 origen = borde + (Vector3.up * GroundProbeHeightMeters);
        RaycastHit[] impactos = Physics.RaycastAll(origen, Vector3.down, GroundProbeDistanceMeters, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        bool hayPiso = false;
        float alturaPiso = float.MinValue;
        foreach (RaycastHit impacto in impactos)
        {
            bool esPisable = impacto.normal.y >= WalkableMinimumNormalY
                && impacto.collider.GetComponentInParent<PlayerController>() == null
                && impacto.collider.GetComponentInParent<Enemy>() == null;
            if (esPisable && impacto.point.y > alturaPiso)
            {
                alturaPiso = impacto.point.y;
                hayPiso = true;
            }
        }

        if (!hayPiso) return posicionAparicion;

        CharacterController controlador = GetComponent<CharacterController>();
        float mitadAltura = controlador != null ? (controlador.height * HalfFactor) - controlador.center.y : 1f;
        borde.y = alturaPiso + mitadAltura + GroundSkinMeters;
        return borde;
    }

    public void Reanimar()
    {
        if (!estaDerribado) return;
        estaDerribado = false;
        vidaActual = vidaAlRevivir;
        motor?.StopVerticalMotion();
        OnReanimado?.Invoke(this);
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

    // ===== REINICIO =====

    /// <summary>Restores the player, its weapons, and its energy to the state of a new match.</summary>
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
        motor.Reset();
        motor.Teleport(posicionAparicion, rotacionAparicion);

        municionDirecta = armas?.armaDirecta?.municionMaxima ?? municionDirectaInicial;
        municionArea = armas?.armaArea?.municionMaxima ?? municionAreaInicial;
        cameraLook.Reset(camaraJugador);

        // Cada jugador reinicia sus propios sistemas: el GameManager no necesita conocerlos.
        armas?.ResetState();
        energia?.ResetState();
    }

    // ===== MUNICIÓN =====

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
}
