/**
 * Weaver.cs
 * Tejedor: Enemigo a distancia que no ataca directamente.
 * Lanza un campo que ralentiza al jugador y reduce visibilidad cerca del Pilar.
 * Aplica daño por segundo dentro del área.
 */
using UltimoPilar.Core.Shared;
using UnityEngine;

public class Weaver : Enemy
{
    private const float MovementSpeedMetersPerSecond = 1.2f;
    private const float MaximumHealth = 35f;
    private const int EnergyDropAmount = 4;
    private const float NoDamage = 0f;
    private const float RetreatRangeMultiplier = 0.6f;
    private const float RotationSharpness = 3f;
    private const float FieldHeightMeters = 0.1f;
    private const float FieldThicknessMeters = 0.1f;
    private static readonly Color FieldColor = new Color(0.5f, 0f, 0.5f, 0.3f);

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
        velocidadMovimiento = MovementSpeedMetersPerSecond;
        vidaMaxima = MaximumHealth;
        vidaActual = vidaMaxima;
        energiaDrop = EnergyDropAmount;
        rangoAtaque = rangoLanzamiento;

        // No tiene daño directo
        dañoAlPilar = NoDamage;
        dañoAlJugador = NoDamage;
    }

    protected override void Comportamiento()
    {
        if (pilarObjetivo == null)
        {
            return;
        }

        timerCampo -= Time.deltaTime;

        Vector3 haciaPilar = pilarObjetivo.transform.position - transform.position;
        haciaPilar.y = 0;
        float distanciaPilar = haciaPilar.magnitude;

        // Mantenerse a distancia media del Pilar
        if (distanciaPilar > rangoLanzamiento)
        {
            MoverHacia(haciaPilar.normalized);
        }
        else if (distanciaPilar < rangoLanzamiento * RetreatRangeMultiplier)
        {
            MoverHacia(-haciaPilar.normalized);
        }
        else if (timerCampo <= 0)
        {
            LanzarCampo();
            timerCampo = cooldownCampo;
        }

        if (haciaPilar != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(haciaPilar),
                Time.deltaTime * RotationSharpness);
        }
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

        GameObject campo = Instantiate(prefabCampo, pilarObjetivo.transform.position, Quaternion.identity);
        if (campo.TryGetComponent(out WeaverZone zona))
        {
            ConfigurarZona(zona);
        }
    }

    void CrearCampo(Vector3 posicion)
    {
        GameObject campo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        campo.name = "CampoTejedor";
        Destroy(campo.GetComponent<Collider>()); // No necesitamos collider físico
        campo.transform.position = posicion + Vector3.up * FieldHeightMeters;
        campo.transform.localScale = new Vector3(radioCampo * 2f, FieldThicknessMeters, radioCampo * 2f);
        OwnedMaterialCleanup.Assign(campo.GetComponent<Renderer>(), RuntimeMaterialFactory.CreateLit(FieldColor));

        ConfigurarZona(campo.AddComponent<WeaverZone>());
    }

    void ConfigurarZona(WeaverZone zona)
    {
        zona.dañoPorSegundo = dañoPorSegundo;
        zona.factorRalentizacion = factorRalentizacion;
        zona.duracion = duracionCampo;
    }
}
