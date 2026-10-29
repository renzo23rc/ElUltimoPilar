using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

/// <summary>Creates the shared runtime menu UI primitives.</summary>
public static class MenuUiFactory
{
    private const string BuiltinFontName = "LegacyRuntime.ttf";
    private const int MenuSortingOrder = 30;
    private static readonly Color PanelColor = new Color(0.035f, 0.055f, 0.09f, 0.96f);
    private static readonly Color ButtonColor = new Color(0.12f, 0.24f, 0.34f, 1f);
    private static readonly Color SelectedColor = new Color(0.2f, 0.52f, 0.68f, 1f);

    /// <summary>Creates a full-screen overlay canvas above the shared HUD (sorting order 20).</summary>
    public static Canvas CreateCanvas(string name)
    {
        GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = MenuSortingOrder;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    /// <summary>Ensures there is one active EventSystem using the Input System UI module.</summary>
    public static EventSystem EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }

        StandaloneInputModule[] legacyModules = eventSystem.GetComponents<StandaloneInputModule>();
        foreach (StandaloneInputModule legacyModule in legacyModules)
            legacyModule.enabled = false;

        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }
        else if (inputModule.actionsAsset == null)
        {
            inputModule.AssignDefaultActions();
        }

        // Keep controller/keyboard focus when clicking outside a selectable control.
        inputModule.deselectOnBackgroundClick = false;
        inputModule.enabled = true;
        return eventSystem;
    }

    /// <summary>Creates a centered, vertically navigable panel.</summary>
    public static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = PanelColor;
        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 12f;
        layout.padding = new RectOffset(24, 24, 24, 24);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(620f, 0f);
        return panel;
    }

    /// <summary>Creates a centered menu label that does not intercept pointer events.</summary>
    public static Text CreateLabel(Transform parent, string text, int fontSize)
    {
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        labelObject.transform.SetParent(parent, false);
        Text label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>(BuiltinFontName);
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        labelObject.GetComponent<LayoutElement>().preferredHeight = fontSize + 28f;
        return label;
    }

    /// <summary>Creates a selectable button with a visible background and non-raycast label.</summary>
    public static Button CreateButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);
        Image background = buttonObject.GetComponent<Image>();
        background.color = ButtonColor;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = SelectedColor;
        colors.selectedColor = SelectedColor;
        colors.pressedColor = new Color(0.08f, 0.18f, 0.26f, 1f);
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        if (onClick != null)
            button.onClick.AddListener(onClick);
        buttonObject.GetComponent<LayoutElement>().preferredHeight = 62f;
        Text label = CreateLabel(buttonObject.transform, text, 26);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(12f, 0f);
        label.rectTransform.offsetMax = new Vector2(-12f, 0f);
        return button;
    }

    /// <summary>Selects the first selectable control in a panel for keyboard and gamepad navigation.</summary>
    public static void FocusFirst(GameObject panel)
    {
        if (panel == null)
            return;

        EventSystem eventSystem = EnsureEventSystem();
        Selectable first = panel.GetComponentInChildren<Selectable>();
        eventSystem.SetSelectedGameObject(first != null ? first.gameObject : null);
    }
}
