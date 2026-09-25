using UnityEngine;

/// <summary>Expands and fades the ground shockwave ring of the area weapon, then destroys it.</summary>
public class OndaAreaAnim : MonoBehaviour
{
    private const float InitialScaleMeters = 0.5f;
    private const float RingThicknessMeters = 0.02f;
    private const float InitialAlpha = 0.8f;
    private const float FinalAlpha = 0f;

    private float radio;
    private float duracion;
    private float transcurrido;
    private Material material;

    /// <summary>Starts the animation.</summary>
    /// <param name="r">The final ring diameter in meters.</param>
    /// <param name="d">The duration in seconds.</param>
    public void Init(float r, float d)
    {
        radio = r;
        duracion = d;
        transcurrido = 0f;
        Renderer rend = GetComponent<Renderer>();
        material = rend != null ? rend.sharedMaterial : null;
    }

    void Update()
    {
        transcurrido += Time.deltaTime;
        float progreso = duracion > 0f ? transcurrido / duracion : 1f;
        float escala = Mathf.Lerp(InitialScaleMeters, radio, progreso);
        transform.localScale = new Vector3(escala, RingThicknessMeters, escala);

        if (material != null)
        {
            Color color = material.color;
            color.a = Mathf.Lerp(InitialAlpha, FinalAlpha, progreso);
            material.color = color;
        }

        if (transcurrido >= duracion)
        {
            Destroy(gameObject);
        }
    }
}
