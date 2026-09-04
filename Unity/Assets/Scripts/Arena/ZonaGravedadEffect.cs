using UnityEngine;

// Script de zona de gravedad exagerado - va en el objeto ZonaGravedad
public class ZonaGravedadEffect : MonoBehaviour
{
    [Header("Efecto Exagerado")]
    public float fuerzaAscenso = 18f;
    public float radioEfecto = 5f;
    public float fuerzaTornado = 4f;

    private const float EnemyDamping = 2.5f;
    private const float EnemyUpwardAcceleration = 9f;
    private const float EnemyHorizontalVelocityRetention = 0.92f;
    private const float ExitDamping = 0.1f;
    private const float VisualPulseBase = 1f;
    private const float VisualPulseSpeed = 1.8f;
    private const float VisualPulseAmount = 0.07f;
    private const float VisualAlphaBase = 0.35f;
    private const float VisualAlphaSpeed = 2.2f;
    private const float VisualAlphaAmount = 0.12f;
    private const float VisualWidthMeters = 10f;
    private const float VisualHeightMeters = 4f;
    private static readonly Color VisualColor = new Color(0.6f, 0.1f, 1f, VisualAlphaBase);
    
    void OnTriggerEnter(Collider other)
    {
        var pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.EntrarZonaGravedad();
            Debug.Log("[ZonaGravedad] Player ENTER - impulso exagerado");
        }
        var rb = other.GetComponent<Rigidbody>();
        if (rb != null && other.GetComponent<Enemy>() != null)
        {
            rb.AddForce(Vector3.up * fuerzaAscenso, ForceMode.VelocityChange);
            rb.linearDamping = EnemyDamping; // flotacion
        }
    }
    void OnTriggerStay(Collider other)
    {
        var rb = other.GetComponent<Rigidbody>();
        if (rb != null && other.GetComponent<Enemy>() != null)
        {
            // Flotacion continua exagerada + tornado leve
            rb.AddForce(Vector3.up * EnemyUpwardAcceleration, ForceMode.Acceleration);
            rb.AddForce(new Vector3(Mathf.Sin(Time.time*2f)*fuerzaTornado, 0, Mathf.Cos(Time.time*2f)*fuerzaTornado), ForceMode.Acceleration);
            // Enemigos casi no avanzan en zona
            rb.linearVelocity = new Vector3(rb.linearVelocity.x * EnemyHorizontalVelocityRetention, rb.linearVelocity.y, rb.linearVelocity.z * EnemyHorizontalVelocityRetention);
        }
    }
    void OnTriggerExit(Collider other)
    {
        var pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.SalirZonaGravedad();
            Debug.Log("[ZonaGravedad] Player EXIT");
        }
        var rb = other.GetComponent<Rigidbody>();
        if (rb != null) rb.linearDamping = ExitDamping;
    }
    void Update()
    {
        // Pulso visual exagerado
        float scale = VisualPulseBase + Mathf.Sin(Time.time * VisualPulseSpeed) * VisualPulseAmount;
        transform.localScale = new Vector3(VisualWidthMeters * scale, VisualHeightMeters, VisualWidthMeters * scale);
        var rend = GetComponent<Renderer>();
        if (rend != null)
        {
            Color baseColor = VisualColor;
            float alpha = VisualAlphaBase + Mathf.Sin(Time.time * VisualAlphaSpeed) * VisualAlphaAmount;
            rend.material.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }
    }
}
