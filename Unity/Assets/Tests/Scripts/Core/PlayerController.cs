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

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadMovimiento = 8f;
    public float gravedad = -20f;
    public float sensibilidadMouse = 0.5f;
    
    [Header("Vida")]
    public float vidaMaxima = 100f;
    public float vidaActual = 100f;
    
    [Header("Referencias")]
    public Camera camaraJugador;
    public Transform puntoDisparo;
    public LayerMask capaEnemigos;
    public LayerMask capaPilar;
    
    [Header("Munición por Arma")]
    public int municionDirecta = 60;
    public int municionArea = 12;
    
    private CharacterController controller;
    private Vector3 velocidadVertical;
    private float rotacionX = 0f;
    
    // Componentes
    private EnergySystem energia;
    private WeaponSystem armas;

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
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.juegoActivo) return;
        
        LeerInput();
        ManejarGravedad();
        ManejarDisparo();
    }

    void LeerInput()
    {
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
        
        Vector3 movimiento = (forward * inputMovimiento.y + right * inputMovimiento.x) * velocidadMovimiento;
        controller.Move(movimiento * Time.deltaTime);
        
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
        if (controller.isGrounded && velocidadVertical.y < 0)
        {
            velocidadVertical.y = -1f;
        }
        else
        {
            velocidadVertical.y += gravedad * Time.deltaTime;
        }
        controller.Move(velocidadVertical * Time.deltaTime);
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
        vidaActual = Mathf.Max(0, vidaActual - cantidad);
        Debug.Log($"[Player] Daño recibido: {cantidad}. Vida: {vidaActual}/{vidaMaxima}");
        
        if (vidaActual <= 0)
        {
            Debug.Log("[Player] Jugador derribado.");
        }
    }

    public void Curar(float cantidad)
    {
        vidaActual = Mathf.Min(vidaMaxima, vidaActual + cantidad);
    }

    public void ReponerMunicion()
    {
        municionDirecta = 60;
        municionArea = 12;
        Debug.Log("[Player] Munición repuesta al final de oleada");
    }
}
