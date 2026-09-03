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
    public float multiplicadorDaño = 2f;
    public float duracionSegundos = 12f;

    [Header("Presentación")]
    public float velocidadRotacion = 120f;
    public float velocidadLevitacion = 2f;
    public float alturaLevitacion = 0.4f;

    public event Action<WeaponSystem> OnRecogida;

    private Vector3 posicionInicial;
    private float tiempo;

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
    }

    void Update()
    {
        tiempo += Time.deltaTime;
        transform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime);
        float y = posicionInicial.y + Mathf.Sin(tiempo * velocidadLevitacion) * alturaLevitacion;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        var armas = player.GetComponent<WeaponSystem>();
        if (armas == null) return;

        armas.ApplyVariant(tipoPotenciado, multiplicadorDaño, duracionSegundos);
        OnRecogida?.Invoke(armas);
        AudioAdapter.Play(AudioAdapter.Sfx.Variant);
        Debug.Log($"[Variante] {player.name} recogió x{multiplicadorDaño} {tipoPotenciado} por {duracionSegundos}s");
        Destroy(gameObject);
    }

    void OnTriggerStay(Collider other)
    {
        OnTriggerEnter(other);
    }
}
