/**
 * CombatFeedback.cs
 * Feedback de combate centralizado: screen shake por jugador y hitstop breve.
 * API estática para que armas y enemigos notifiquen sin acoplarse a la UI.
 * El shake se aplica y revierte dentro del mismo ciclo de render para no
 * mover permanentemente ninguna cámara.
 */
using UnityEngine;
using System;
using System.Collections.Generic;

public class CombatFeedback : MonoBehaviour
{
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

    public static CombatFeedback Instance
    {
        get
        {
            if (instancia == null)
                instancia = Crear();
            return instancia;
        }
    }

    private static CombatFeedback Crear()
    {
        var go = new GameObject("CombatFeedback");
        var feedback = go.AddComponent<CombatFeedback>();
        DontDestroyOnLoad(go);
        return feedback;
    }

    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }

    public static void NotifyShot(float intensidad)
    {
        Instance.AgregarShake(Mathf.Min(Mathf.Max(intensidad, 0f), Instance.shakePorDisparoMaximo));
    }

    public static void NotifyHit(bool mato)
    {
        Instance.AgregarShake(mato ? Instance.shakePorMuerte : Instance.shakePorGolpe);
        if (mato)
            Instance.hitstopRestante = Mathf.Max(Instance.hitstopRestante, Instance.duracionHitstopMuerte);
        OnCombatHit?.Invoke(mato);
    }

    private void AgregarShake(float cantidad)
    {
        intensidadActual = Mathf.Min(intensidadActual + cantidad, shakePorMuerte);
    }

    void LateUpdate()
    {
        RevertirOffsets();

        if (hitstopRestante > 0f)
        {
            hitstopRestante -= Time.unscaledDeltaTime;
            if (JuegoEnCurso())
                Time.timeScale = escalaHitstop;
            if (hitstopRestante <= 0f && JuegoEnCurso())
                Time.timeScale = 1f;
        }

        if (intensidadActual > 0.001f)
        {
            AplicarShake();
            intensidadActual = Mathf.Max(0f, intensidadActual - decaimientoShake * Time.unscaledDeltaTime * 0.12f);
        }
    }

    private bool JuegoEnCurso()
    {
        var manager = GameManager.Instance;
        return manager == null || manager.EstadoActual == MatchState.Playing;
    }

    private void RevertirOffsets()
    {
        if (offsetsAplicados.Count == 0) return;
        var camaras = new List<Camera>(offsetsAplicados.Keys);
        foreach (var camara in camaras)
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
        var manager = GameManager.Instance;
        if (manager == null) return;
        foreach (var jugador in manager.Players)
        {
            if (jugador == null || jugador.camaraJugador == null) continue;
            var camara = jugador.camaraJugador;
            if (!camara.isActiveAndEnabled) continue;
            Vector3 offset = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                0f) * intensidadActual;
            camara.transform.position += offset;
            offsetsAplicados[camara] = offset;
        }
    }
}
