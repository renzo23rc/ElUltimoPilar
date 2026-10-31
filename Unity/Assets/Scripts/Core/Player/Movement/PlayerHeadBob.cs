using UnityEngine;

/// <summary>
/// Bobs the player camera and keeps the holder on the look axis, at the head's height and forward offset.
/// Lateral bone swing is ignored so a body turn does not pull the view off center.
/// </summary>
[DefaultExecutionOrder(50)]
public sealed class PlayerHeadBob : MonoBehaviour
{
    private const string HeadBoneName = "DEF-spine.006";
    private const float DefaultAmplitudeMeters = 0.015f;
    private const float DefaultFrequencyHertz = 10f;
    private const float ToggleSpeedMetersPerSecond = 3f;
    private const float FocusDistanceMeters = 15f;
    private const float ResetLerpPerSecond = 1f;
    private const float LateralAmplitudeScale = 2f;
    private const float FrequencyHalfDivisor = 2f;
    private const float MinimumPlanarForwardSqr = 0.0001f;

    [SerializeField] private bool bobEnabled = true;
    [SerializeField, Range(0f, 0.1f)] private float amplitudeMeters = DefaultAmplitudeMeters;
    [SerializeField, Range(0f, 30f)] private float frequencyHertz = DefaultFrequencyHertz;
    [SerializeField] private Vector3 eyeOffsetMeters = new Vector3(0f, 0f, 0.35f);

    private CharacterController characterController;
    private Transform cameraTransform;
    private Transform cameraHolder;
    private Transform headBone;
    private Vector3 startPosition;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        ResolveCamera();
    }

    private void LateUpdate()
    {
        ResolveCamera();

        if (cameraTransform == null || cameraHolder == null)
        {
            return;
        }

        PlaceHolderAtHead();

        if (!bobEnabled)
        {
            cameraTransform.localPosition = startPosition;
            cameraTransform.localRotation = Quaternion.identity;
            return;
        }

        if (IsMovingOnGround())
        {
            cameraTransform.localPosition = startPosition + FootstepMotion();
        }
        else
        {
            ResetPosition();
        }

        cameraTransform.LookAt(FocusTarget());
    }

    private void ResolveCamera()
    {
        if (cameraTransform != null && cameraHolder != null && headBone != null)
        {
            return;
        }

        if (cameraTransform == null)
        {
            PlayerController player = GetComponent<PlayerController>();
            Camera ownedCamera = player != null ? player.camaraJugador : null;
            if (ownedCamera == null)
            {
                ownedCamera = GetComponentInChildren<Camera>(true);
            }

            if (ownedCamera == null)
            {
                return;
            }

            cameraTransform = ownedCamera.transform;
            startPosition = cameraTransform.localPosition;
        }

        if (cameraHolder == null && cameraTransform.parent != null && cameraTransform.parent != transform)
        {
            cameraHolder = cameraTransform.parent;
        }

        if (headBone == null)
        {
            headBone = FindChildByName(transform, HeadBoneName);
        }
    }

    // El clip de caminar adelanta el hueso. Se copia solo ese avance: el costado queda en el eje de la mirada.
    private void PlaceHolderAtHead()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < MinimumPlanarForwardSqr)
        {
            forward = Vector3.forward;
        }
        else
        {
            forward.Normalize();
        }

        float eyeHeight = cameraHolder.localPosition.y;
        float forwardMeters = eyeOffsetMeters.z;
        if (headBone != null)
        {
            Vector3 headOnPlayer = transform.InverseTransformPoint(headBone.position);
            eyeHeight = headOnPlayer.y;
            forwardMeters = headOnPlayer.z + eyeOffsetMeters.z;
        }

        cameraHolder.position = transform.position
            + (transform.right * eyeOffsetMeters.x)
            + (Vector3.up * (eyeHeight + eyeOffsetMeters.y))
            + (forward * forwardMeters);
    }

    private bool IsMovingOnGround()
    {
        if (characterController == null || !characterController.isGrounded)
        {
            return false;
        }

        Vector3 planarVelocity = characterController.velocity;
        planarVelocity.y = 0f;
        return planarVelocity.magnitude >= ToggleSpeedMetersPerSecond;
    }

    private Vector3 FootstepMotion()
    {
        Vector3 motion = Vector3.zero;
        motion.y = Mathf.Sin(Time.time * frequencyHertz) * amplitudeMeters;
        motion.x = Mathf.Cos(Time.time * frequencyHertz / FrequencyHalfDivisor) * amplitudeMeters * LateralAmplitudeScale;
        return motion;
    }

    private void ResetPosition()
    {
        if (cameraTransform.localPosition == startPosition)
        {
            return;
        }

        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition,
            startPosition,
            ResetLerpPerSecond * Time.deltaTime);
    }

    private Vector3 FocusTarget()
    {
        return cameraHolder.position + (cameraHolder.forward * FocusDistanceMeters);
    }

    private static Transform FindChildByName(Transform root, string boneName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == boneName)
            {
                return children[i];
            }
        }

        return null;
    }
}
