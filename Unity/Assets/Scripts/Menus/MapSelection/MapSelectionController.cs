using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Placeholder map picker. Each button loads one playable scene.</summary>
public sealed class MapSelectionController : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const int TitleFontSize = 42;
    private const int ButtonFontSize = 28;
    private const float ButtonWidth = 560f;
    private const float ButtonHeight = 76f;
    private const float ButtonSpacing = 18f;
    private const float TitleSpacing = 36f;
    private const int BackOptionOffset = 1;
    private static readonly Color IdleButtonColor = new Color(0.22f, 0.24f, 0.28f, 1f);
    private static readonly Color SelectedButtonColor = new Color(0.35f, 0.48f, 0.32f, 1f);

    private static readonly MapOption[] Maps =
    {
        new MapOption("Obelisco", "SampleScene"),
        new MapOption("Estatua de la Libertad", "EstatuaDeLaLibertad"),
        new MapOption("Cristo Redentor", "CristoRedentor"),
    };

    private readonly Button[] mapButtons = new Button[Maps.Length];
    private Button backButton;
    private InputAction moveUp;
    private InputAction moveDown;
    private InputAction confirm;
    private InputAction back;
    private int selectedIndex;
    private bool sceneLoadRequested;

    private void Awake()
    {
        Time.timeScale = 1f;
        EnsureEventSystem();
        ConfigureCamera();
        BuildScreen();
        CreateActions();
        ShowSelection();
    }

    private void OnEnable()
    {
        moveUp?.Enable();
        moveDown?.Enable();
        confirm?.Enable();
        back?.Enable();
    }

    private void OnDisable()
    {
        moveUp?.Disable();
        moveDown?.Disable();
        confirm?.Disable();
        back?.Disable();
    }

    private void OnDestroy()
    {
        DisposeAction(ref moveUp, HandleMoveUp);
        DisposeAction(ref moveDown, HandleMoveDown);
        DisposeAction(ref confirm, HandleConfirm);
        DisposeAction(ref back, HandleBack);
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    private static void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.13f, 0.15f, 1f);
    }

    private void BuildScreen()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvasObject = new GameObject("MapSelectionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = 5;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var column = new GameObject("Opciones", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        column.layer = 5;
        column.transform.SetParent(canvasObject.transform, false);
        RectTransform columnRect = column.GetComponent<RectTransform>();
        columnRect.anchorMin = new Vector2(0.5f, 0.5f);
        columnRect.anchorMax = new Vector2(0.5f, 0.5f);
        columnRect.pivot = new Vector2(0.5f, 0.5f);
        columnRect.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = ButtonSpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = column.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateLabel(column.transform, font, "Elegi un mapa", TitleFontSize, TitleSpacing);
        for (int i = 0; i < Maps.Length; i++)
        {
            int optionIndex = i;
            mapButtons[i] = CreateButton(column.transform, font, Maps[i].Label, () => LoadMap(optionIndex));
        }

        backButton = CreateButton(column.transform, font, "Volver", LoadMainMenu);
    }

    private static void CreateLabel(Transform parent, Font font, string message, int fontSize, float preferredHeight)
    {
        var labelObject = new GameObject("Titulo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(LayoutElement));
        labelObject.layer = 5;
        labelObject.transform.SetParent(parent, false);
        LayoutElement layout = labelObject.GetComponent<LayoutElement>();
        layout.preferredWidth = ButtonWidth;
        layout.preferredHeight = preferredHeight;
        Text text = labelObject.GetComponent<Text>();
        text.font = font;
        text.text = message;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private Button CreateButton(Transform parent, Font font, string message, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(message, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.layer = 5;
        buttonObject.transform.SetParent(parent, false);
        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredWidth = ButtonWidth;
        layout.preferredHeight = ButtonHeight;

        Image image = buttonObject.GetComponent<Image>();
        image.color = IdleButtonColor;

        var textObject = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.layer = 5;
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = message;
        text.fontSize = ButtonFontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        return button;
    }

    private void CreateActions()
    {
        moveUp = CreateButtonAction("MapMoveUp", HandleMoveUp, "<Keyboard>/upArrow", "<Keyboard>/w", "<Gamepad>/dpad/up");
        moveDown = CreateButtonAction("MapMoveDown", HandleMoveDown, "<Keyboard>/downArrow", "<Keyboard>/s", "<Gamepad>/dpad/down");
        confirm = CreateButtonAction("MapConfirm", HandleConfirm, "<Keyboard>/enter", "<Keyboard>/numpadEnter", "<Keyboard>/space", "<Gamepad>/buttonSouth");
        back = CreateButtonAction("MapBack", HandleBack, "<Keyboard>/escape", "<Gamepad>/buttonEast");
    }

    private static InputAction CreateButtonAction(string actionName, System.Action<InputAction.CallbackContext> handler, params string[] bindings)
    {
        var action = new InputAction(actionName, InputActionType.Button);
        for (int i = 0; i < bindings.Length; i++)
            action.AddBinding(bindings[i]);

        action.performed += handler;
        return action;
    }

    private void HandleMoveUp(InputAction.CallbackContext context)
    {
        MoveSelection(-1);
    }

    private void HandleMoveDown(InputAction.CallbackContext context)
    {
        MoveSelection(1);
    }

    private void MoveSelection(int direction)
    {
        int optionCount = Maps.Length + BackOptionOffset;
        selectedIndex = VictoryScreenController.MoveSelection(selectedIndex, direction, optionCount);
        ShowSelection();
    }

    private void HandleConfirm(InputAction.CallbackContext context)
    {
        if (selectedIndex >= Maps.Length)
        {
            LoadMainMenu();
            return;
        }

        LoadMap(selectedIndex);
    }

    private void HandleBack(InputAction.CallbackContext context)
    {
        LoadMainMenu();
    }

    private void ShowSelection()
    {
        for (int i = 0; i < mapButtons.Length; i++)
            Tint(mapButtons[i], i == selectedIndex);

        Tint(backButton, selectedIndex == Maps.Length);
    }

    private static void Tint(Button button, bool selected)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = selected ? SelectedButtonColor : IdleButtonColor;
    }

    private void LoadMap(int optionIndex)
    {
        LoadScene(Maps[optionIndex].SceneName);
    }

    private void LoadMainMenu()
    {
        LoadScene(MainMenuSceneName);
    }

    private void LoadScene(string sceneName)
    {
        if (sceneLoadRequested)
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("MapSelection: la escena '" + sceneName + "' no esta en File > Build Settings.");
            return;
        }

        sceneLoadRequested = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private static void DisposeAction(ref InputAction action, System.Action<InputAction.CallbackContext> handler)
    {
        if (action == null)
            return;

        action.performed -= handler;
        action.Dispose();
        action = null;
    }

    private readonly struct MapOption
    {
        public MapOption(string label, string sceneName)
        {
            Label = label;
            SceneName = sceneName;
        }

        public string Label { get; }
        public string SceneName { get; }
    }
}
