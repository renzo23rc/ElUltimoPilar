using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and anchors the legacy UI elements shared by the match HUD and the per-player HUDs.
/// </summary>
public static class HudUiFactory
{
    /// <summary>The reference resolution every HUD is designed for.</summary>
    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    private const float MatchWidthOrHeight = 0.5f;
    private const string BuiltinFontName = "LegacyRuntime.ttf";
    private static readonly Vector2 DefaultTextSize = new Vector2(400f, 40f);
    private static readonly Vector2 OutlineDistance = new Vector2(1f, -1f);
    private static readonly Color BarBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    private static readonly Vector2 TopRight = new Vector2(1f, 1f);
    private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
    private static readonly Vector2 BottomRight = new Vector2(1f, 0f);

    /// <summary>Configures a scaler so the HUD keeps its proportions at any resolution or viewport.</summary>
    public static void ConfigureScaler(CanvasScaler scaler)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = MatchWidthOrHeight;
    }

    /// <summary>Creates an outlined legacy text.</summary>
    public static Text CreateText(Transform parent, string objectName, int fontSize, TextAnchor alignment)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>(BuiltinFontName);
        text.color = Color.white;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.rectTransform.sizeDelta = DefaultTextSize;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = OutlineDistance;
        return text;
    }

    /// <summary>Creates a bar and returns its fill image, stretched inside a dark background.</summary>
    public static Image CreateBar(Transform parent, string objectName)
    {
        var background = new GameObject(objectName + "_Fondo", typeof(RectTransform));
        background.transform.SetParent(parent, false);
        background.AddComponent<Image>().color = BarBackgroundColor;

        var fill = new GameObject(objectName + "_Fill", typeof(RectTransform));
        fill.transform.SetParent(background.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.green;

        RectTransform rect = fillImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return fillImage;
    }

    /// <summary>Anchors to the top-left corner; the offset is measured inward from the edges.</summary>
    public static void AnchorTopLeft(RectTransform rect, Vector2 offsetFromEdges, Vector2 size)
    {
        Anchor(rect, TopLeft, new Vector2(offsetFromEdges.x, -offsetFromEdges.y), size);
    }

    /// <summary>Anchors to the top-right corner; the offset is measured inward from the edges.</summary>
    public static void AnchorTopRight(RectTransform rect, Vector2 offsetFromEdges, Vector2 size)
    {
        Anchor(rect, TopRight, new Vector2(-offsetFromEdges.x, -offsetFromEdges.y), size);
    }

    /// <summary>Anchors to the top-center, the given distance below the top edge.</summary>
    public static void AnchorTopCenter(RectTransform rect, float offsetFromTop, Vector2 size)
    {
        Anchor(rect, TopCenter, new Vector2(0f, -offsetFromTop), size);
    }

    /// <summary>Anchors to the center with a vertical offset (positive is up).</summary>
    public static void AnchorCenter(RectTransform rect, float verticalOffset, Vector2 size)
    {
        Anchor(rect, Center, new Vector2(0f, verticalOffset), size);
    }

    /// <summary>Anchors to the bottom-center, the given distance above the bottom edge.</summary>
    public static void AnchorBottomCenter(RectTransform rect, float offsetFromBottom, Vector2 size)
    {
        Anchor(rect, BottomCenter, new Vector2(0f, offsetFromBottom), size);
    }

    /// <summary>Anchors to the bottom-right corner; the offset is measured inward from the edges.</summary>
    public static void AnchorBottomRight(RectTransform rect, Vector2 offsetFromEdges, Vector2 size)
    {
        Anchor(rect, BottomRight, new Vector2(-offsetFromEdges.x, offsetFromEdges.y), size);
    }

    /// <summary>Anchors the background of a bar created with <see cref="CreateBar"/>.</summary>
    public static void AnchorBar(Image fill, System.Action<RectTransform> anchor)
    {
        if (fill != null && fill.transform.parent != null && fill.transform.parent.TryGetComponent(out RectTransform background))
        {
            anchor(background);
        }
    }

    private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
