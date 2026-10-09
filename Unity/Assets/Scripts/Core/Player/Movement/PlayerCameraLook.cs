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
    private bool initialStateCaptured;

    /// <summary>Captures the camera's initial field of view and rotation once.</summary>
    public void CaptureInitialState(Camera camera)
    {
        if (initialStateCaptured || camera == null)
        {
            return;
        }

        initialFieldOfView = camera.fieldOfView;
        initialLocalRotation = camera.transform.localRotation;
        initialStateCaptured = true;
    }

    /// <summary>Applies a look delta: pitch on the camera, yaw on the body, and aligns the muzzle.</summary>
    /// <param name="camera">The player camera.</param>
    /// <param name="body">The player root.</param>
    /// <param name="muzzle">The optional muzzle that follows the camera.</param>
    /// <param name="lookDeltaDegrees">The yaw (x) and pitch (y) deltas in degrees.</param>
    public void Apply(Camera camera, Transform body, Transform muzzle, Vector2 lookDeltaDegrees)
    {
        pitchDegrees = Mathf.Clamp(pitchDegrees - lookDeltaDegrees.y, -MaximumLookAngleDegrees, MaximumLookAngleDegrees);
        if (camera == null)
        {
            return;
        }

        camera.transform.localRotation = Quaternion.Euler(pitchDegrees, 0, 0);
        body.Rotate(Vector3.up * lookDeltaDegrees.x);
        if (muzzle != null)
        {
            muzzle.rotation = camera.transform.rotation;
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
        CaptureInitialState(camera);
        if (camera != null && initialStateCaptured)
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
            camera.transform.localRotation = initialLocalRotation;
            pitchDegrees = initialLocalRotation.eulerAngles.x;
            if (pitchDegrees > HalfTurnDegrees)
            {
                pitchDegrees -= FullTurnDegrees;
            }
        }

        RestoreFieldOfView(camera);
    }
}
