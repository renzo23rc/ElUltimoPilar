/**
 * OndaExpansion.cs
 * Helper para animar la onda visual de habilidades.
 */
using UnityEngine;

public class OndaExpansion : MonoBehaviour
{
    private const float InitialScaleMeters = 0.1f;
    private const float VerticalFlattenRatio = 0.2f;
    private const float LingerSeconds = 0.2f;

    private float radioObjetivo;
    private float duracion;
    private float timer;

    public void Iniciar(float radio, float tiempo)
    {
        radioObjetivo = radio;
        duracion = tiempo;
        timer = 0f;
        transform.localScale = Vector3.one * InitialScaleMeters;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progreso = duracion > 0f ? timer / duracion : 1f;
        float escala = Mathf.Lerp(InitialScaleMeters, radioObjetivo, progreso);
        transform.localScale = new Vector3(escala, escala * VerticalFlattenRatio, escala);

        if (timer >= duracion + LingerSeconds)
        {
            Destroy(gameObject);
        }
    }
}
