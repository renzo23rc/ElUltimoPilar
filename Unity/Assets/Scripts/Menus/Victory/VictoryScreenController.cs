using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Navigates the victory screen and loads the menu or the level that was just won.</summary>
public sealed class VictoryScreenController : MonoBehaviour
{
    public const string SceneName = "VictoryScreen";

    private const int RestartOption = 0;
    private const int MainMenuOption = 1;
    private const int OptionCount = 2;
    private const float CursorRowSpacingPixels = 98f;
    private const string DefaultMainMenuSceneName = "MainMenu";
    private const string DefaultLevelSceneName = "Obelisco";
    private const float BackgroundClear = 1f;

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
    private Image restartUnderlineImage;
    private Image menuUnderlineImage;
    private RectTransform cursorRect;
    private int selectedIndex;
    private bool confirmed;

    /// <summary>Moves a selection by one step and wraps at both ends.</summary>
    public static int MoveSelection(int current, int direction, int optionCount)
    {
        if (optionCount <= 0)
            return 0;

        int next = current + direction;
        if (next < 0)
            return optionCount - 1;

        if (next >= optionCount)
            return 0;

        return next;
    }

    /// <summary>Remembers the won level and opens the victory screen on the next frame.</summary>
    public static void OpenFromLevel(string levelSceneName)
    {
        if (string.IsNullOrEmpty(levelSceneName) || levelSceneName == SceneName)
            return;

        LevelSceneName = levelSceneName;
        LoadVictorySceneNextFrame();
    }

    private static async void LoadVictorySceneNextFrame()
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
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TrySubscribe();
    }

    private static void TrySubscribe()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null)
            return;

        manager.OnVictoria -= HandleVictory;
        manager.OnVictoria += HandleVictory;
    }

    private static void HandleVictory()
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
        MenuMusicPlayer.EnsureOn(gameObject);
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
            Debug.LogError("VictoryScreen: no hay un Canvas.");
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
        moveUp = CreateButtonAction("VictoryMoveUp", HandleMoveUp, "<Keyboard>/upArrow", "<Keyboard>/w", "<Gamepad>/dpad/up");
        moveDown = CreateButtonAction("VictoryMoveDown", HandleMoveDown, "<Keyboard>/downArrow", "<Keyboard>/s", "<Gamepad>/dpad/down");
        confirm = CreateButtonAction("VictoryConfirm", HandleConfirm, "<Keyboard>/enter", "<Keyboard>/numpadEnter", "<Keyboard>/space", "<Gamepad>/buttonSouth");
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
        MenuSfx.PlaySelect();
        SetSelection(MoveSelection(selectedIndex, -1, OptionCount));
    }

    private void HandleMoveDown(InputAction.CallbackContext context)
    {
        MenuSfx.PlaySelect();
        SetSelection(MoveSelection(selectedIndex, 1, OptionCount));
    }

    private void HandleConfirm(InputAction.CallbackContext context)
    {
        if (confirmed)
            return;

        confirmed = true;
        Time.timeScale = 1f;
        if (selectedIndex == RestartOption)
            MenuSfx.PlaySelect();
        else
            MenuSfx.PlayBack();
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

        float offset = selectedIndex == RestartOption ? CursorRowSpacingPixels : 0f;
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
            Debug.LogError("VictoryScreen: la escena '" + sceneName + "' no esta en File > Build Settings.");
            return false;
        }

        SceneManager.LoadScene(sceneName);
        return true;
    }

    private static void DisposeAction(ref InputAction action, System.Action<InputAction.CallbackContext> handler)
    {
        if (action == null)
            return;

        action.performed -= handler;
        action.Dispose();
        action = null;
    }
}
