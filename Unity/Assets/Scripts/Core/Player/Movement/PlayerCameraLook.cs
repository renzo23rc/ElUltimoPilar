using UnityEngine;

/// <summary>
/// Owns one player's camera pitch, yaw on the body, and field of view, including their initial state.
/// </summary>
public sealed class PlayerCameraLook
{
    private const float MaximumLookAngleDegrees = 80f;
    private const float HalfTurnDegrees = 180f;
    private const float FullTurnDegrees = 360f;

    private float pitchDegrees;
    private float initialFieldOfView;
    private Quaternion initialLocalRotation;
    private Transform lookPivot;
    private bool initialStateCaptured;

    /// <summary>Captures the camera's initial field of view and the look pivot rotation once.</summary>
    /// <param name="camera">The player camera. Pitch is applied to its holder when one exists.</param>
    /// <param name="body">The player root. Yaw is applied here.</param>
    public void CaptureInitialState(Camera camera, Transform body)
    {
        if (initialStateCaptured || camera == null)
        {
            return;
        }

        lookPivot = ResolveLookPivot(camera, body);
        initialFieldOfView = camera.fieldOfView;
        initialLocalRotation = lookPivot.localRotation;
        initialStateCaptured = true;
    }

    /// <summary>Applies a look delta: pitch on the camera, yaw on the body, and aligns the muzzle.</summary>
    /// <param name="camera">The player camera.</param>
    /// <param name="body">The player root.</param>
    /// <param name="muzzle">The optional muzzle that follows the camera.</param>
    /// <param name="lookDeltaDegrees">The yaw (x) and pitch (y) deltas in degrees.</param>
    public void Apply(Camera camera, Transform body, Transform muzzle, Vector2 lookDeltaDegrees)
    {
        float lookSensitivity = MenuPreferences.LookSensitivity;
        float verticalDirection = MenuPreferences.InvertY ? -1f : 1f;
        pitchDegrees = Mathf.Clamp(
            pitchDegrees - lookDeltaDegrees.y * lookSensitivity * verticalDirection,
            -MaximumLookAngleDegrees,
            MaximumLookAngleDegrees);
        if (camera == null)
        {
            return;
        }

        lookPivot = ResolveLookPivot(camera, body);
        lookPivot.localRotation = Quaternion.Euler(pitchDegrees, 0f, 0f);
        if (body != null)
        {
            body.Rotate(Vector3.up * lookDeltaDegrees.x * lookSensitivity);
        }

        if (muzzle != null)
        {
            muzzle.rotation = lookPivot.rotation;
        }
    }

    /// <summary>Overrides the field of view, as inside a gravity zone.</summary>
    public void SetFieldOfView(Camera camera, float fieldOfViewDegrees)
    {
        if (camera != null)
        {
            camera.fieldOfView = fieldOfViewDegrees;
        }
    }

    /// <summary>Restores the captured field of view.</summary>
    public void RestoreFieldOfView(Camera camera)
    {
        if (camera == null)
        {
            return;
        }

        CaptureInitialState(camera, camera.transform.root);
        if (initialStateCaptured)
        {
            camera.fieldOfView = initialFieldOfView;
        }
    }

    /// <summary>Restores the captured rotation and field of view.</summary>
    public void Reset(Camera camera)
    {
        pitchDegrees = 0f;
        if (camera != null && initialStateCaptured)
        {
            Transform pivot = lookPivot != null ? lookPivot : camera.transform;
            pivot.localRotation = initialLocalRotation;
            pitchDegrees = initialLocalRotation.eulerAngles.x;
            if (pitchDegrees > HalfTurnDegrees)
            {
                pitchDegrees -= FullTurnDegrees;
            }
        }

        RestoreFieldOfView(camera);
    }

    // El pitch vive en el CamHolder. La cámara solo cabecea en local y mira al frente.
    private static Transform ResolveLookPivot(Camera camera, Transform body)
    {
        Transform parent = camera.transform.parent;
        if (parent != null && parent != body)
        {
            return parent;
        }

        return camera.transform;
    }
}
