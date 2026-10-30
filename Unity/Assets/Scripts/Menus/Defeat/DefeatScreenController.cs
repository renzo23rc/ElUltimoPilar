using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Navigates the defeat screen and loads the menu or restarts the level that was just lost.</summary>
public sealed class DefeatScreenController : MonoBehaviour
{
    public const string SceneName = "DefeatScreen";

    private const int RestartOption = 0;
    private const int MainMenuOption = 1;
    private const int OptionCount = 2;
    private const float CursorRowSpacingPixels = 95f;
    private const float BackgroundClear = 1f;
    private const string DefaultMainMenuSceneName = "MainMenu";
    private const string DefaultLevelSceneName = "SampleScene";

    public static string LevelSceneName { get; private set; }

    [Header("Sprites")]
    [SerializeField] private Sprite background;
    [SerializeField] private Sprite options;
    [SerializeField] private Sprite cursorSprite;
    [SerializeField] private Sprite restartUnderline;
    [SerializeField] private Sprite menuUnderline;

    [Header("Escenas")]
    [SerializeField] private string mainMenuSceneName = DefaultMainMenuSceneName;
    [SerializeField] private string fallbackLevelSceneName = DefaultLevelSceneName;

    private InputAction moveUp;
    private InputAction moveDown;
    private InputAction confirm;
    private static GameManager subscribedManager;
    private static bool watchingManager;

    private Image restartUnderlineImage;
    private Image menuUnderlineImage;
    private RectTransform cursorRect;
    private int selectedIndex;
    private bool confirmed;

    /// <summary>Remembers the lost level and opens the defeat screen on the next frame.</summary>
    public static void OpenFromLevel(string levelSceneName)
    {
        if (string.IsNullOrEmpty(levelSceneName) || levelSceneName == SceneName)
            return;

        LevelSceneName = levelSceneName;
        LoadDefeatSceneNextFrame();
    }

    private static async void LoadDefeatSceneNextFrame()
    {
        await Awaitable.NextFrameAsync();
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(SceneName))
            SceneManager.LoadScene(SceneName);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TrySubscribe();
        if (watchingManager)
            return;

        watchingManager = true;
        WatchForManager();
    }

    private static async void WatchForManager()
    {
        while (Application.isPlaying)
        {
            TrySubscribe();
            try
            {
                await Awaitable.NextFrameAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        watchingManager = false;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TrySubscribe();
    }

    private static void TrySubscribe()
    {
        GameManager manager = GameManager.Instance;
        if (manager == subscribedManager)
            return;

        if (subscribedManager != null)
            subscribedManager.OnDerrota -= HandleDefeat;

        subscribedManager = manager;
        if (manager == null)
            return;

        manager.OnDerrota += HandleDefeat;
    }

    private static void HandleDefeat()
    {
        OpenFromLevel(SceneManager.GetActiveScene().name);
    }

    private void Awake()
    {
        Time.timeScale = 1f;
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
    }

    private void OnDisable()
    {
        moveUp?.Disable();
        moveDown?.Disable();
        confirm?.Disable();
    }

    private void OnDestroy()
    {
        DisposeAction(ref moveUp, HandleMoveUp);
        DisposeAction(ref moveDown, HandleMoveDown);
        DisposeAction(ref confirm, HandleConfirm);
    }

    private void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(BackgroundClear, BackgroundClear, BackgroundClear, 1f);
    }

    private void BuildScreen()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("DefeatScreen: no hay un Canvas.");
            return;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        CreateLayer(canvas.transform, "Fondo", background);
        CreateLayer(canvas.transform, "Opciones", options);
        restartUnderlineImage = CreateLayer(canvas.transform, "SubrayadoReiniciar", restartUnderline);
        menuUnderlineImage = CreateLayer(canvas.transform, "SubrayadoMenu", menuUnderline);
        Image cursorImage = CreateLayer(canvas.transform, "Cursor", cursorSprite);
        cursorRect = cursorImage.rectTransform;
    }

    private static Image CreateLayer(Transform parent, string layerName, Sprite sprite)
    {
        var layer = new GameObject(layerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        layer.layer = 5;
        layer.transform.SetParent(parent, false);
        RectTransform rect = layer.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = layer.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.preserveAspect = false;
        return image;
    }

    private void CreateActions()
    {
        moveUp = CreateButtonAction("DefeatMoveUp", HandleMoveUp, "<Keyboard>/upArrow", "<Keyboard>/w", "<Gamepad>/dpad/up");
        moveDown = CreateButtonAction("DefeatMoveDown", HandleMoveDown, "<Keyboard>/downArrow", "<Keyboard>/s", "<Gamepad>/dpad/down");
        confirm = CreateButtonAction("DefeatConfirm", HandleConfirm, "<Keyboard>/enter", "<Keyboard>/numpadEnter", "<Keyboard>/space", "<Gamepad>/buttonSouth");
    }

    private static InputAction CreateButtonAction(string actionName, Action<InputAction.CallbackContext> handler, params string[] bindings)
    {
        var action = new InputAction(actionName, InputActionType.Button);
        for (int i = 0; i < bindings.Length; i++)
            action.AddBinding(bindings[i]);

        action.performed += handler;
        return action;
    }

    private void HandleMoveUp(InputAction.CallbackContext context)
    {
        SetSelection(VictoryScreenController.MoveSelection(selectedIndex, -1, OptionCount));
    }

    private void HandleMoveDown(InputAction.CallbackContext context)
    {
        SetSelection(VictoryScreenController.MoveSelection(selectedIndex, 1, OptionCount));
    }

    private void HandleConfirm(InputAction.CallbackContext context)
    {
        if (confirmed)
            return;

        confirmed = true;
        Time.timeScale = 1f;
        string sceneName = selectedIndex == RestartOption ? LevelToLoad() : mainMenuSceneName;
        if (!LoadScene(sceneName))
            confirmed = false;
    }

    private void SetSelection(int index)
    {
        selectedIndex = index;
        ShowSelection();
    }

    private void ShowSelection()
    {
        if (restartUnderlineImage != null)
            restartUnderlineImage.enabled = selectedIndex == RestartOption;

        if (menuUnderlineImage != null)
            menuUnderlineImage.enabled = selectedIndex == MainMenuOption;

        if (cursorRect == null)
            return;

        float offset = selectedIndex == RestartOption ? 0f : -CursorRowSpacingPixels;
        cursorRect.offsetMin = new Vector2(0f, offset);
        cursorRect.offsetMax = new Vector2(0f, offset);
    }

    private string LevelToLoad()
    {
        return string.IsNullOrEmpty(LevelSceneName) ? fallbackLevelSceneName : LevelSceneName;
    }

    private static bool LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("DefeatScreen: la escena '" + sceneName + "' no esta en File > Build Settings.");
            return false;
        }

        SceneManager.LoadScene(sceneName);
        return true;
    }

    private static void DisposeAction(ref InputAction action, Action<InputAction.CallbackContext> handler)
    {
        if (action == null)
            return;

        action.performed -= handler;
        action.Dispose();
        action = null;
    }
}
