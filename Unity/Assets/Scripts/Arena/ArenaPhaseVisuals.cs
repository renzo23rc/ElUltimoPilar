using System;
using System.Collections.Generic;
using UltimoPilar.Arena;
using UnityEngine;

/// <summary>
/// Holds the per-map monument variants that <see cref="ArenaTransform"/> shows on each phase.
/// Phase 1 keeps the original map pieces; later phases enable the matching child under this object.
/// Original pieces stay active so Pilar, hitboxes, and walk colliders keep working.
/// </summary>
public sealed class ArenaPhaseVisuals : MonoBehaviour
{
    private const int FirstPhase = 1;

    private const string StarPieceName = "Base.001";

    [SerializeField] private GameObject[] phaseVisuals;
    [SerializeField] private string[] originalPieceNames =
    {
        "Statue",
        "Torch",
        "Base",
        "Base.001"
    };
    [SerializeField] private Material originalStarMaterial;

    private Renderer[] originalRenderers = Array.Empty<Renderer>();
    private bool originalRenderersCaptured;

    private void Awake()
    {
        ResolvePhaseVisuals();
        CaptureOriginalRenderers();
        ShowPhase(FirstPhase);
    }

    /// <summary>Shows the monument variant that matches this Pilar phase.</summary>
    public void ShowPhase(int phase)
    {
        ResolvePhaseVisuals();
        CaptureOriginalRenderers();

        int variantIndex = ArenaPhaseVisualSelector.DeterioratedVariantIndex(
            phase,
            phaseVisuals.Length);
        bool showOriginals = variantIndex < 0;
        if (showOriginals)
        {
            ApplyOriginalStarMaterial();
        }

        SetOriginalRenderersEnabled(showOriginals);
        for (int i = 0; i < phaseVisuals.Length; i++)
        {
            if (phaseVisuals[i] != null)
            {
                phaseVisuals[i].SetActive(i == variantIndex);
            }
        }
    }

    private void ResolvePhaseVisuals()
    {
        if (phaseVisuals != null && phaseVisuals.Length > 0)
        {
            return;
        }

        int childCount = transform.childCount;
        phaseVisuals = new GameObject[childCount];
        for (int i = 0; i < childCount; i++)
        {
            phaseVisuals[i] = transform.GetChild(i).gameObject;
        }

        Array.Sort(phaseVisuals, ComparePhaseVisualNames);
    }

    private static int ComparePhaseVisualNames(GameObject left, GameObject right)
    {
        string leftName = left != null ? left.name : string.Empty;
        string rightName = right != null ? right.name : string.Empty;
        return string.CompareOrdinal(leftName, rightName);
    }

    private void CaptureOriginalRenderers()
    {
        if (originalRenderersCaptured)
        {
            return;
        }

        if (originalPieceNames == null || originalPieceNames.Length == 0)
        {
            originalRenderersCaptured = true;
            return;
        }

        var renderers = new List<Renderer>();
        for (int i = 0; i < originalPieceNames.Length; i++)
        {
            Transform piece = FindMapPiece(originalPieceNames[i]);
            if (piece == null)
            {
                continue;
            }

            Renderer[] pieceRenderers = piece.GetComponentsInChildren<Renderer>(true);
            renderers.AddRange(pieceRenderers);
        }

        originalRenderers = renderers.ToArray();
        originalRenderersCaptured = true;
    }

    private void ApplyOriginalStarMaterial()
    {
        if (originalStarMaterial == null)
        {
            return;
        }

        Transform star = FindMapPiece(StarPieceName);
        if (star == null)
        {
            return;
        }

        Renderer[] starRenderers = star.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < starRenderers.Length; i++)
        {
            if (starRenderers[i] != null)
            {
                starRenderers[i].sharedMaterial = originalStarMaterial;
            }
        }
    }

    private void SetOriginalRenderersEnabled(bool enabled)
    {
        for (int i = 0; i < originalRenderers.Length; i++)
        {
            if (originalRenderers[i] != null)
            {
                originalRenderers[i].enabled = enabled;
            }
        }
    }

    private Transform FindMapPiece(string name)
    {
        Transform mapRoot = transform.parent;
        if (mapRoot == null)
        {
            return null;
        }

        foreach (Transform child in mapRoot)
        {
            if (child == transform)
            {
                continue;
            }

            Transform found = FindChildByName(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return mapRoot.name == name ? mapRoot : null;
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform found = FindChildByName(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
