using System.Collections.Generic;
using UnityEngine;
using UltimoPilar.Core.Shared;

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
    private const float VisualAlphaBase = 0.1f;
    private const float VisualAlphaSpeed = 2.2f;
    private const float VisualAlphaAmount = 0.04f;
    private const float VisualWidthMeters = 10f;
    private const float VisualHeightMeters = 4f;
    private const float TornadoFrequency = 2f;
    private const string GravityMusicResource = "Audio/Musica/gravity_field";
    private const float GravityMusicVolume = 0.25f;
    private static readonly Color VisualColor = new Color(0.6f, 0.1f, 1f, VisualAlphaBase);

    private Renderer rend;
    private AudioSource gravityMusic;
    private readonly HashSet<PlayerController> playersInside = new HashSet<PlayerController>();
    private Material ownedMaterial;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        gravityMusic = gameObject.AddComponent<AudioSource>();
        gravityMusic.playOnAwake = false;
        gravityMusic.loop = true;
        gravityMusic.spatialBlend = 0f;
        gravityMusic.clip = Resources.Load<AudioClip>(GravityMusicResource);
        gravityMusic.volume = GravityMusicVolume * MenuPreferences.MusicVolume;
        // Clon único del material, registrado para liberarse con la zona.
        // El pulso por frame reutiliza este clon en vez de renderer.material.
        if (rend != null)
        {
            ownedMaterial = rend.material;
            OwnedMaterialCleanup.Track(gameObject, ownedMaterial);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            pc.EntrarZonaGravedad();
            if (playersInside.Add(pc))
            {
                RefreshGravityMusic();
            }
        }

        if (TryGetEnemyBody(other, out Rigidbody rb))
        {
            rb.AddForce(Vector3.up * fuerzaAscenso, ForceMode.VelocityChange);
            rb.linearDamping = EnemyDamping; // flotacion
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (TryGetEnemyBody(other, out Rigidbody rb))
        {
            // Flotacion continua exagerada + tornado leve
            rb.AddForce(Vector3.up * EnemyUpwardAcceleration, ForceMode.Acceleration);
            float fase = Time.time * TornadoFrequency;
            rb.AddForce(new Vector3(Mathf.Sin(fase) * fuerzaTornado, 0f, Mathf.Cos(fase) * fuerzaTornado), ForceMode.Acceleration);
            // Enemigos casi no avanzan en zona
            rb.linearVelocity = new Vector3(rb.linearVelocity.x * EnemyHorizontalVelocityRetention, rb.linearVelocity.y, rb.linearVelocity.z * EnemyHorizontalVelocityRetention);
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            pc.SalirZonaGravedad();
            if (playersInside.Remove(pc))
            {
                RefreshGravityMusic();
            }
        }

        // Solo se restaura el damping de los enemigos que esta zona modificó.
        if (TryGetEnemyBody(other, out Rigidbody rb))
        {
            rb.linearDamping = ExitDamping;
        }
    }

    static bool TryGetEnemyBody(Collider other, out Rigidbody rb)
    {
        // Los enemigos estacionarios (kinematic, como el Nido) no reciben fuerzas.
        rb = other.attachedRigidbody;
        return rb != null && !rb.isKinematic && rb.GetComponent<Enemy>() != null;
    }

    void Update()
    {
        // Pulso visual exagerado
        float scale = VisualPulseBase + Mathf.Sin(Time.time * VisualPulseSpeed) * VisualPulseAmount;
        transform.localScale = new Vector3(VisualWidthMeters * scale, VisualHeightMeters, VisualWidthMeters * scale);
        if (gravityMusic != null)
        {
            gravityMusic.volume = GravityMusicVolume * MenuPreferences.MusicVolume;
        }

        if (ownedMaterial != null)
        {
            Color baseColor = VisualColor;
            float alpha = VisualAlphaBase + Mathf.Sin(Time.time * VisualAlphaSpeed) * VisualAlphaAmount;
            ownedMaterial.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }
    }

    private void RefreshGravityMusic()
    {
        if (gravityMusic == null || gravityMusic.clip == null)
        {
            return;
        }

        if (playersInside.Count > 0)
        {
            if (!gravityMusic.isPlaying)
            {
                gravityMusic.Play();
            }

            return;
        }

        gravityMusic.Stop();
    }
}
