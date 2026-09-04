using UnityEngine;

namespace UltimoPilar.Core.Pilar;

/// <summary>Applies phase colors to a renderer without phase branching.</summary>
public sealed class PilarVisualPresenter
{
    private const int FirstPhase = 1;
    private const float DefaultLerpSpeedPerSecond = 2f;
    private Color[] colors;
    private readonly Color fallbackColor;

    /// <summary>Creates a presenter using one color for each one-based phase.</summary>
    public PilarVisualPresenter(Color[] colors)
    {
        this.colors = colors ?? System.Array.Empty<Color>();
        fallbackColor = Color.white;
    }

    /// <summary>Updates the colors used for subsequent presentations.</summary>
    public void UpdateColors(Color[] updatedColors)
    {
        colors = updatedColors ?? System.Array.Empty<Color>();
    }

    /// <summary>Moves the renderer toward the color configured for the phase.</summary>
    public void Present(Renderer renderer, int phase, float deltaTime)
    {
        if (renderer == null)
        {
            return;
        }

        Color targetColor = GetColor(phase);
        renderer.material.color = Color.Lerp(
            renderer.material.color,
            targetColor,
            deltaTime * DefaultLerpSpeedPerSecond);
    }

    private Color GetColor(int phase)
    {
        int colorIndex = phase - FirstPhase;
        return colorIndex >= 0 && colorIndex < colors.Length
            ? colors[colorIndex]
            : fallbackColor;
    }
}
