using UnityEngine;

/// <summary>Scales and fades a procedural impact marker, then destroys it.</summary>
public class ImpactoAnim : MonoBehaviour
{
    private const float HitGrowthScale = 1.6f;
    private const float MissShrinkScale = 0.6f;
    private const float OpaqueAlpha = 1f;
    private const float TransparentAlpha = 0f;

    private float duracion;
    private bool esHit;
    private float transcurrido;
    private Vector3 escalaInicial;
    private Material material;

    /// <summary>Starts the animation.</summary>
    /// <param name="d">The duration in seconds.</param>
    /// <param name="h">Whether the impact hit an enemy (grows) or missed (shrinks).</param>
    public void Init(float d, bool h)
    {
        duracion = d;
        esHit = h;
        transcurrido = 0f;
        escalaInicial = transform.localScale;
        Renderer rend = GetComponent<Renderer>();
        material = rend != null ? rend.sharedMaterial : null;
    }

    void Update()
    {
        transcurrido += Time.deltaTime;
        float progreso = duracion > 0f ? transcurrido / duracion : 1f;
        float escalaFinal = esHit ? HitGrowthScale : MissShrinkScale;
        transform.localScale = Vector3.Lerp(escalaInicial, escalaInicial * escalaFinal, progreso);

        if (material != null)
        {
            Color color = material.color;
            color.a = Mathf.Lerp(OpaqueAlpha, TransparentAlpha, progreso);
            material.color = color;
        }

        if (transcurrido >= duracion)
        {
            Destroy(gameObject);
        }
    }
}
