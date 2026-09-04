using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides centralized combat feedback such as camera shake and hitstop.
/// </summary>
public class CombatFeedback : MonoBehaviour
{
    private const float MinimumShakeIntensity = 0.001f;
    private const float ZeroHealthThreshold = 0f;
    private const float RandomOffsetMinimum = -1f;
    private const float RandomOffsetMaximum = 1f;
    private const float ShakeDecayScale = 0.12f;

    /// <summary>
    /// Raised when combat hit feedback is notified. The argument indicates whether the hit killed its target.
    /// </summary>
    public static event Action<bool> OnCombatHit;

    private static CombatFeedback instancia;

    [Header("Configuración")]
    public float shakePorDisparoMaximo = 0.06f;
    public float shakePorGolpe = 0.12f;
    public float shakePorMuerte = 0.22f;
    public float decaimientoShake = 6f;
    public float duracionHitstopMuerte = 0.06f;
    public float escalaHitstop = 0.08f;

    private float intensidadActual;
    private float hitstopRestante;
    private readonly Dictionary<Camera, Vector3> offsetsAplicados = new Dictionary<Camera, Vector3>();

    /// <summary>
    /// Gets the shared combat feedback instance.
    /// </summary>
    public static CombatFeedback Instance
    {
        get
        {
            if (instancia == null)
            {
                instancia = Crear();
            }

            return instancia;
        }
    }

    private static CombatFeedback Crear()
    {
        GameObject gameObject = new GameObject("CombatFeedback");
        CombatFeedback feedback = gameObject.AddComponent<CombatFeedback>();
        DontDestroyOnLoad(gameObject);
        return feedback;
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instancia == this)
        {
            instancia = null;
        }
    }

    /// <summary>
    /// Determines whether a damage request reduces a target to zero or less health.
    /// </summary>
    /// <param name="request">The damage request to evaluate.</param>
    /// <param name="targetHealthBeforeDamage">The target health before applying the request.</param>
    /// <returns><see langword="true"/> when the resulting health is at or below zero.</returns>
    public static bool IsLethalDamage(DamageRequest request, float targetHealthBeforeDamage)
    {
        return targetHealthBeforeDamage - request.Amount <= ZeroHealthThreshold;
    }

    /// <summary>
    /// Notifies the feedback system of a shot.
    /// </summary>
    /// <param name="intensidad">The requested shake intensity.</param>
    public static void NotifyShot(float intensidad)
    {
        Instance.AgregarShake(Mathf.Min(Mathf.Max(intensidad, 0f), Instance.shakePorDisparoMaximo));
    }

    /// <summary>
    /// Notifies the feedback system of a hit.
    /// </summary>
    /// <param name="mato">Whether the hit killed its target.</param>
    public static void NotifyHit(bool mato)
    {
        Instance.AgregarShake(mato ? Instance.shakePorMuerte : Instance.shakePorGolpe);
        if (mato)
        {
            Instance.hitstopRestante = Mathf.Max(Instance.hitstopRestante, Instance.duracionHitstopMuerte);
        }

        OnCombatHit?.Invoke(mato);
    }

    private void AgregarShake(float cantidad)
    {
        intensidadActual = Mathf.Min(intensidadActual + cantidad, shakePorMuerte);
    }

    private void LateUpdate()
    {
        RevertirOffsets();

        if (hitstopRestante > 0f)
        {
            hitstopRestante -= Time.unscaledDeltaTime;
            if (JuegoEnCurso())
            {
                Time.timeScale = escalaHitstop;
            }

            if (hitstopRestante <= 0f && JuegoEnCurso())
            {
                Time.timeScale = 1f;
            }
        }

        if (intensidadActual > MinimumShakeIntensity)
        {
            AplicarShake();
            intensidadActual = Mathf.Max(
                0f,
                intensidadActual - decaimientoShake * Time.unscaledDeltaTime * ShakeDecayScale);
        }
    }

    private bool JuegoEnCurso()
    {
        GameManager manager = GameManager.Instance;
        return manager == null || manager.EstadoActual == MatchState.Playing;
    }

    private void RevertirOffsets()
    {
        if (offsetsAplicados.Count == 0)
        {
            return;
        }

        List<Camera> camaras = new List<Camera>(offsetsAplicados.Keys);
        foreach (Camera camara in camaras)
        {
            if (camara == null)
            {
                offsetsAplicados.Remove(camara);
                continue;
            }

            camara.transform.position -= offsetsAplicados[camara];
            offsetsAplicados.Remove(camara);
        }
    }

    private void AplicarShake()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null)
        {
            return;
        }

        foreach (PlayerController jugador in manager.Players)
        {
            if (jugador == null || jugador.camaraJugador == null)
            {
                continue;
            }

            Camera camara = jugador.camaraJugador;
            if (!camara.isActiveAndEnabled)
            {
                continue;
            }

            Vector3 offset = new Vector3(
                UnityEngine.Random.Range(RandomOffsetMinimum, RandomOffsetMaximum),
                UnityEngine.Random.Range(RandomOffsetMinimum, RandomOffsetMaximum),
                0f) * intensidadActual;
            camara.transform.position += offset;
            offsetsAplicados[camara] = offset;
        }
    }
}
