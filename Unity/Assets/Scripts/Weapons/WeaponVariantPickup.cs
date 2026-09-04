/**
 * WeaponVariantPickup.cs
 * Drop de variante temporal de arma: potencia un tipo de arma por segundos.
 * Al recogerlo aplica la variante al WeaponSystem del jugador y desaparece.
 */
using UnityEngine;
using System;

public class WeaponVariantPickup : MonoBehaviour
{
private static readonly float FullCircleRadians = Mathf.PI * 2f;
    [Header("Variante")]
    public WeaponSystem.TipoArma tipoPotenciado = WeaponSystem.TipoArma.Directa;
    public WeaponSystem.WeaponVariant variant = WeaponSystem.WeaponVariant.PrecisionRifle;
    public float multiplicadorDaño = 2f;
    public float duracionSegundos = 12f;

    [Header("Presentación")]
    public float velocidadRotacion = 120f;
    public float velocidadLevitacion = 2f;
    public float alturaLevitacion = 0.4f;

    [Header("Recolección")]
    public float rangoAtraccion = 5f;
    public float velocidadAtraccion = 8f;
    public float radioTrigger = 0.9f;

    public event Action<WeaponSystem> OnRecogida;

    private Vector3 posicionInicial;
    private float tiempo;
    private bool collected;

    void Start()
    {
        posicionInicial = transform.position;
        tiempo = UnityEngine.Random.Range(0f, FullCircleRadians);
        var rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = new Color(1f, 0.55f, 0.1f);
            if (rend.material.HasProperty("_EmissionColor"))
                rend.material.SetColor("_EmissionColor", new Color(1f, 0.4f, 0f) * 0.8f);
        }
    }

    void OnEnable()
    {
        posicionInicial = transform.position;
        collected = false;
        EnsureTriggerAndRigidbody();
    }

    void Update()
    {
        tiempo += Time.deltaTime;
        transform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime);
        float y = posicionInicial.y + Mathf.Sin(tiempo * velocidadLevitacion) * alturaLevitacion;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
        AtraerHaciaJugador();
    }

    void EnsureTriggerAndRigidbody()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        if (radioTrigger > 0f) col.radius = radioTrigger;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        else
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void AtraerHaciaJugador()
    {
        PlayerController jugador = ResolverJugadorCercano();
        if (jugador == null) return;
        float distancia = Vector3.Distance(transform.position, jugador.transform.position);
        if (distancia <= rangoAtraccion)
        {
            Vector3 direccion = (jugador.transform.position - transform.position).normalized;
            // Mantener levitación en Y pero atraer en XZ y un poco en Y.
            direccion.y = 0f;
            if (direccion.sqrMagnitude < 0.001f) direccion = (jugador.transform.position - transform.position).normalized;
            transform.position += direccion * velocidadAtraccion * Time.deltaTime;
            posicionInicial += direccion * velocidadAtraccion * Time.deltaTime;
        }
    }

    PlayerController ResolverJugadorCercano()
    {
        GameManager manager = GameManager.Instance;
        if (manager != null && manager.PlayerCount > 0)
        {
            PlayerController cercano = null;
            float minDist = float.MaxValue;
            foreach (PlayerController jugador in manager.Players)
            {
                if (jugador == null) continue;
                float d = Vector3.Distance(transform.position, jugador.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    cercano = jugador;
                }
            }
            if (cercano != null) return cercano;
        }
        return FindFirstObjectByType<PlayerController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected)
return;

        var player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        var armas = player.GetComponent<WeaponSystem>();
        if (armas == null) return;

        collected = true;
        ApplyVariantTo(armas);
        OnRecogida?.Invoke(armas);
        AudioAdapter.Play(AudioAdapter.Sfx.Variant);
        Debug.Log($"[Variante] {player.name} recogió x{multiplicadorDaño} {armas.ActiveVariantDisplayName} por {duracionSegundos}s");
        Destroy(gameObject);
    }

    private void ApplyVariantTo(WeaponSystem armas)
    {
            if (variant == WeaponSystem.WeaponVariant.PrecisionRifle
                && tipoPotenciado != WeaponSystem.TipoArma.Directa)
        {
armas.ApplyVariant(tipoPotenciado, multiplicadorDaño, duracionSegundos);
return;
        }

        armas.ApplyVariant(variant, multiplicadorDaño, duracionSegundos);
    }

    void OnTriggerStay(Collider other)
    {
        OnTriggerEnter(other);
    }
}
