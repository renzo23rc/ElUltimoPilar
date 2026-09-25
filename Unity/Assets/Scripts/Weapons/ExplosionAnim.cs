using UltimoPilar.Core.Shared;
using UnityEngine;

/// <summary>Expands and fades the procedural area-weapon explosion sphere, then destroys it.</summary>
public class ExplosionAnim : MonoBehaviour
{
    private const float InitialScaleMeters = 0.2f;
    private const float VerticalFlattenRatio = 0.6f;
    private const float InitialAlpha = 0.5f;
    private const float FinalAlpha = 0f;

    private float radio;
    private float duracion;
    private float transcurrido;
    private Color color;
    private Material material;

    /// <summary>Starts the animation.</summary>
    /// <param name="r">The final explosion radius in meters.</param>
    /// <param name="d">The duration in seconds.</param>
    /// <param name="c">The explosion color.</param>
    public void Init(float r, float d, Color c)
    {
        radio = r;
        duracion = d;
        color = c;
        transcurrido = 0f;
        Renderer rend = GetComponent<Renderer>();
        material = rend != null ? rend.sharedMaterial : null;
    }

    void Update()
    {
        transcurrido += Time.deltaTime;
        float progreso = duracion > 0f ? transcurrido / duracion : 1f;
        float escala = Mathf.Lerp(InitialScaleMeters, radio * 2f, progreso);
        transform.localScale = new Vector3(escala, escala * VerticalFlattenRatio, escala);

        if (material != null)
        {
            Color actual = color;
            actual.a = Mathf.Lerp(InitialAlpha, FinalAlpha, progreso);
            MaterialColorHelper.SetBaseAndEmissionColor(material, actual);
        }

        if (transcurrido >= duracion)
        {
            Destroy(gameObject);
        }
    }
}
