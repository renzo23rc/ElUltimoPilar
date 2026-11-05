using UnityEngine;

/// <summary>
/// Keeps the animated body in place under the player root.
/// Some walk and run clips bake about two meters of forward travel into the torso bone, so the body slides ahead
/// and snaps back every loop. Because the first-person camera follows the head bone, that travel threw the view
/// back and forth, through the head. This component cancels only that forward travel by moving the visual root,
/// so the body keeps its natural sideways position under the camera.
/// </summary>
[DefaultExecutionOrder(40)]
public sealed class PlayerInPlaceLocomotion : MonoBehaviour
{
    private const string TravelBoneName = "torso";

    [SerializeField] private Animator animator;

    private Transform visualRoot;
    private Transform travelBone;
    private Vector3 baseLocalPosition;
    private Vector3 restLocalPosition;
    private bool initialized;

    /// <summary>Gets how far a bone has advanced along the visual forward axis from its rest position.</summary>
    /// <param name="current">The current bone position in the visual root space.</param>
    /// <param name="rest">The rest bone position in the same space.</param>
    /// <returns>A vector along the forward axis only; sideways sway and vertical bob are left to the animation.</returns>
    public static Vector3 ForwardTravel(Vector3 current, Vector3 rest)
    {
        return new Vector3(0f, 0f, current.z - rest.z);
    }

    /// <summary>Moves the visual root so the travel bone stays at its rest position. Runs after the Animator pose.</summary>
    public void CancelTravel()
    {
        if (!TryInitialize())
        {
            return;
        }

        visualRoot.localPosition = baseLocalPosition;
        Vector3 currentLocal = visualRoot.InverseTransformPoint(travelBone.position);
        Vector3 travelLocal = ForwardTravel(currentLocal, restLocalPosition);
        visualRoot.position -= visualRoot.TransformVector(travelLocal);
    }

    private void LateUpdate()
    {
        CancelTravel();
    }

    // El reposo se captura en el primer LateUpdate; solo se usa su coordenada de avance (Z).
    private bool TryInitialize()
    {
        if (initialized)
        {
            return true;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (animator == null)
        {
            return false;
        }

        visualRoot = animator.transform;
        travelBone = FindChildByName(visualRoot, TravelBoneName);
        if (travelBone == null)
        {
            Debug.LogWarning($"{nameof(PlayerInPlaceLocomotion)}: no se encontró el hueso '{TravelBoneName}' en {name}.", this);
            enabled = false;
            return false;
        }

        baseLocalPosition = visualRoot.localPosition;
        restLocalPosition = visualRoot.InverseTransformPoint(travelBone.position);
        initialized = true;
        return true;
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
