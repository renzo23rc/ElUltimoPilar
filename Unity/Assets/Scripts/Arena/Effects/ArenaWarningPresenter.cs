using System;
using System.Collections;
using UnityEngine;
using UltimoPilar.Core.Shared;

namespace UltimoPilar.Arena;

/// <summary>Presents arena warnings through injected HUD and audio ports.</summary>
public sealed class ArenaWarningPresenter
{
    private const float AudioVolume = 0.7f;
    private const float MinimumDurationSeconds = 0f;
    private const float PitMarkerHeightMeters = 0.2f;
    private const float PitMarkerScale = 1.2f;
    private const float PitRingHeightMeters = 0.05f;
    private const float PitRingScale = 1.5f;
    private const float PitRingVerticalOffsetMeters = 0.3f;
    private const float FloorPulseBaseFrequencyHz = 1f;
    private const float FloorPulseFrequencyIncreaseHz = 5f;
    private const float FloorPulseBlend = 0.5f;
    private const float PitMarkerPulseFrequencyHz = 4f;
    private const float PitMarkerPulseAmount = 0.4f;
    private const float PitRingPulseFrequencyHz = 6f;
    private const float PitRingPulseAmount = 0.3f;
    private const float PitMarkerAlphaBase = 0.4f;
    private const float PitMarkerAlphaPulseAmount = 0.4f;
    private const float PitMarkerAlphaPulseFrequencyHz = 5f;
    private const float PitMarkerScaleBase = 1f;
    private const float PingPongLength = 1f;
    private const int TransparentRenderQueue = 3000;
    private const string UniversalLitShaderName = "Universal Render Pipeline/Lit";
    private const string StandardShaderName = "Standard";
    private const string SpritesDefaultShaderName = "Sprites/Default";
    private const string PitMarkerName = "AdvertenciaPozo";
    private const string PitRingName = "AnilloAdvertenciaPozo";
    private const string GravityWarningMessage = "¡ALERTA: Zona gravedad alterada!";
    private const string EmergencyWarningMessage = "¡PROTOCOLO EMERGENCIA! Torretas activadas";
    private static readonly Color PitWarningColor = Color.red;
    private static readonly Color GravityWarningColor = new Color(0.6f, 0.2f, 1f);
    private static readonly Color EmergencyWarningColor = Color.cyan;
    private static readonly Color PitMarkerColor = new Color(1f, 0f, 0f, 0.5f);

    private readonly Renderer floorRenderer;
    private readonly GameObject pitObject;
    private readonly Color floorWarningColor;
    private readonly AudioClip transformationSound;
    private readonly Func<Vector3> audioPositionPort;
    private readonly Action<string, Color, float> hudWarningPort;
    private readonly Action<AudioClip, Vector3, float> audioPort;

    private GameObject warningMarker;
    private GameObject warningRing;
    private Color initialFloorColor;
    private bool initialFloorColorCaptured;

    /// <summary>Initializes a warning presenter with scene references and side-effect ports.</summary>
    /// <param name="floorRenderer">The renderer whose material blinks during warnings.</param>
    /// <param name="pitObject">The pit object used to position the warning marker.</param>
    /// <param name="floorWarningColor">The color used for the floor warning.</param>
    /// <param name="transformationSound">The optional transformation warning sound.</param>
    /// <param name="audioPositionPort">The injected provider for the warning sound position.</param>
    /// <param name="hudWarningPort">The injected HUD warning dispatcher.</param>
    /// <param name="audioPort">The injected audio dispatcher.</param>
    public ArenaWarningPresenter(
        Renderer floorRenderer,
        GameObject pitObject,
        Color floorWarningColor,
        AudioClip transformationSound,
        Func<Vector3> audioPositionPort,
        Action<string, Color, float> hudWarningPort,
        Action<AudioClip, Vector3, float> audioPort)
    {
        this.floorRenderer = floorRenderer;
        this.pitObject = pitObject;
        this.floorWarningColor = floorWarningColor;
        this.transformationSound = transformationSound;
        this.audioPositionPort = audioPositionPort;
        this.hudWarningPort = hudWarningPort;
        this.audioPort = audioPort;
    }

    /// <summary>Presents the central pit warning.</summary>
    /// <param name="durationSeconds">The warning duration in seconds.</param>
    /// <returns>The warning presentation coroutine.</returns>
    public IEnumerator PresentPitWarning(float durationSeconds)
    {
        DispatchAudio();
        DispatchHudWarning($"¡ADVERTENCIA: ¡POZO SE ABRE! ¡Aléjate del centro! ({durationSeconds}s)", PitWarningColor, durationSeconds);

        if (durationSeconds <= MinimumDurationSeconds)
        {
            yield break;
        }

        CreatePitWarningMarker();
        yield return BlinkFloorAndPitMarker(durationSeconds);
    }

    /// <summary>Presents the altered gravity warning.</summary>
    /// <param name="durationSeconds">The warning duration in seconds.</param>
    /// <returns>The warning presentation coroutine.</returns>
    public IEnumerator PresentGravityWarning(float durationSeconds)
    {
        DispatchAudio();
        DispatchHudWarning(GravityWarningMessage, GravityWarningColor, durationSeconds);
        yield return BlinkFloor(durationSeconds);
    }

    /// <summary>Presents the emergency protocol warning.</summary>
    /// <param name="durationSeconds">The warning duration in seconds.</param>
    /// <returns>The warning presentation coroutine.</returns>
    public IEnumerator PresentEmergencyWarning(float durationSeconds)
    {
        DispatchAudio();
        DispatchHudWarning(EmergencyWarningMessage, EmergencyWarningColor, durationSeconds);
        yield return BlinkFloor(durationSeconds);
    }

    /// <summary>Clears generated warning objects and restores the initial floor color.</summary>
    public void Reset()
    {
        CaptureInitialFloorColor();
        ClearWarningObjects();
        RestoreInitialFloorColor();
    }

    private void DispatchHudWarning(string message, Color color, float durationSeconds)
    {
        if (hudWarningPort == null)
        {
            Debug.LogWarning($"[ArenaTransform] HUD warning unavailable: {message}");
            return;
        }

        hudWarningPort(message, color, durationSeconds);
    }

    private void DispatchAudio()
    {
        if (transformationSound == null || audioPositionPort == null || audioPort == null)
        {
            return;
        }

        audioPort(transformationSound, audioPositionPort(), AudioVolume);
    }

    private IEnumerator BlinkFloor(float durationSeconds)
    {
        if (durationSeconds <= MinimumDurationSeconds)
        {
            yield break;
        }

        Color originalColor = GetFloorColor();
        float elapsedSeconds = 0f;
        while (elapsedSeconds < durationSeconds)
        {
            elapsedSeconds += Time.deltaTime;
            UpdateFloorColor(originalColor, elapsedSeconds, durationSeconds);
            yield return null;
        }

        SetFloorColor(originalColor);
    }

    private IEnumerator BlinkFloorAndPitMarker(float durationSeconds)
    {
        if (durationSeconds <= MinimumDurationSeconds)
        {
            yield break;
        }

        Color originalColor = GetFloorColor();
        float elapsedSeconds = 0f;
        while (elapsedSeconds < durationSeconds)
        {
            elapsedSeconds += Time.deltaTime;
            UpdateFloorColor(originalColor, elapsedSeconds, durationSeconds);
            UpdatePitMarker();
            yield return null;
        }

        SetFloorColor(originalColor);
        ClearWarningObjects();
    }

    private void UpdateFloorColor(Color originalColor, float elapsedSeconds, float durationSeconds)
    {
        if (floorRenderer == null)
        {
            return;
        }

        float progress = elapsedSeconds / durationSeconds;
        float frequency = FloorPulseBaseFrequencyHz + progress * FloorPulseFrequencyIncreaseHz;
        float pulse = Mathf.PingPong(Time.time * frequency, PingPongLength);
        Color warningColor = Color.Lerp(originalColor, floorWarningColor, pulse * FloorPulseBlend);
        SetFloorColor(warningColor);
    }

    private Color GetFloorColor()
    {
        if (floorRenderer == null)
        {
            return Color.gray;
        }

        return floorRenderer.material.color;
    }

    private void SetFloorColor(Color color)
    {
        if (floorRenderer == null)
        {
            return;
        }

        MaterialColorHelper.SetBaseAndEmissionColor(floorRenderer.material, color);
    }

    private void CreatePitWarningMarker()
    {
        if (pitObject == null)
        {
            return;
        }

        Vector3 basePosition = pitObject.transform.position + Vector3.up * PitMarkerHeightMeters;
        Vector3 baseScale = pitObject.transform.localScale;
        warningMarker = CreateWarningCylinder(
            PitMarkerName,
            basePosition,
            new Vector3(baseScale.x * PitMarkerScale, PitMarkerHeightMeters, baseScale.z * PitMarkerScale),
            PitMarkerColor);
        warningRing = CreateWarningCylinder(
            PitRingName,
            basePosition + Vector3.up * PitRingVerticalOffsetMeters,
            new Vector3(baseScale.x * PitRingScale, PitRingHeightMeters, baseScale.z * PitRingScale),
            Color.yellow);
    }

    private static GameObject CreateWarningCylinder(
        string objectName,
        Vector3 position,
        Vector3 scale,
        Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        if (marker == null)
        {
            return null;
        }

        marker.name = objectName;
        Collider collider = marker.GetComponent<Collider>();
        if (collider != null)
        {
            UnityEngine.Object.Destroy(collider);
        }

        marker.transform.position = position;
        marker.transform.localScale = scale;
        ConfigureObjectMaterialColor(marker, color);
        return marker;
    }

    private static void ConfigureObjectMaterialColor(GameObject target, Color color)
    {
        if (target == null)
        {
            return;
        }

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Shader shader = Shader.Find(UniversalLitShaderName)
            ?? Shader.Find(StandardShaderName)
            ?? Shader.Find(SpritesDefaultShaderName);
        if (shader != null)
        {
            var material = new Material(shader)
            {
                renderQueue = TransparentRenderQueue
            };
            renderer.material = material;
        }

        SetObjectMaterialColor(target, color);
    }

    private static void SetObjectMaterialColor(GameObject target, Color color)
    {
        if (target == null)
        {
            return;
        }

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        MaterialColorHelper.SetBaseAndEmissionColor(renderer.material, color);
    }

    private void UpdatePitMarker()
    {
        if (warningMarker != null)
        {
            Vector3 baseScale = pitObject == null ? warningMarker.transform.localScale : pitObject.transform.localScale;
                float pulse = PitMarkerScaleBase
                    + Mathf.PingPong(Time.time * PitMarkerPulseFrequencyHz, PitMarkerPulseAmount);
            warningMarker.transform.localScale = new Vector3(
                baseScale.x * PitMarkerScale * pulse,
                PitMarkerHeightMeters,
                baseScale.z * PitMarkerScale * pulse);

            float alpha = PitMarkerAlphaBase
                + Mathf.PingPong(Time.time * PitMarkerAlphaPulseFrequencyHz, PitMarkerAlphaPulseAmount);
            SetObjectMaterialColor(warningMarker, new Color(1f, 0f, 0f, alpha));
        }

        if (warningRing != null)
        {
            Vector3 baseScale = pitObject == null ? warningRing.transform.localScale : pitObject.transform.localScale;
            float ringScale = PitRingScale
                + Mathf.Sin(Time.time * PitRingPulseFrequencyHz) * PitRingPulseAmount;
            warningRing.transform.localScale = new Vector3(
                baseScale.x * ringScale,
                PitRingHeightMeters,
                baseScale.z * ringScale);
        }
    }

    private void CaptureInitialFloorColor()
    {
        if (initialFloorColorCaptured || floorRenderer == null)
        {
            return;
        }

        initialFloorColor = floorRenderer.material.color;
        initialFloorColorCaptured = true;
    }

    private void RestoreInitialFloorColor()
    {
        if (!initialFloorColorCaptured)
        {
            return;
        }

        SetFloorColor(initialFloorColor);
    }

    private void ClearWarningObjects()
    {
        if (warningMarker != null)
        {
            UnityEngine.Object.Destroy(warningMarker);
            warningMarker = null;
        }

        if (warningRing != null)
        {
            UnityEngine.Object.Destroy(warningRing);
            warningRing = null;
        }
    }
}
