using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Navigates the main menu art. Partida opens map selection and Salir quits.</summary>
public sealed class MainMenu : MonoBehaviour
{
    private const int PartidaOption = 0;
    private const int QuitOption = 1;
    private const int OptionCount = 2;
    private const float CursorRowSpacingPixels = 103f;
    private const string DefaultMapSelectionSceneName = "MapSelection";

    [Header("Sprites")]
    [SerializeField] private Sprite cursorSprite;

    [Header("Escenas")]
    [SerializeField] private string mapSelectionSceneName = DefaultMapSelectionSceneName;

    private InputAction moveUp;
    private InputAction moveDown;
    private InputAction confirm;
    private RectTransform cursorRect;
    private int selectedIndex;
    private bool sceneLoadRequested;

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

    /// <summary>Loads the map selection scene.</summary>
    public void GoToMapSelection()
    {
        if (sceneLoadRequested)
            return;

        if (!Application.CanStreamedLevelBeLoaded(mapSelectionSceneName))
        {
            Debug.LogError("MainMenu: la escena '" + mapSelectionSceneName + "' no esta en File > Build Settings.");
            return;
        }

        sceneLoadRequested = true;
        MenuSfx.PlaySelect();
        Time.timeScale = 1f;
        SceneManager.LoadScene(mapSelectionSceneName);
    }

    private static void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
    }

    private void BuildScreen()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("MainMenu: no hay un Canvas.");
            return;
        }

        MenuUiFactory.FitCanvas(canvas);
        for (int i = 0; i < canvas.transform.childCount; i++)
            PrepareArtChild(canvas.transform.GetChild(i));

        if (cursorSprite == null)
            Debug.LogError("MainMenu: falta el sprite del cursor.");

        Image cursorImage = MenuUiFactory.CreateArtLayer(canvas.transform, "Cursor", cursorSprite);
        cursorRect = cursorImage.rectTransform;
    }

    private static void PrepareArtChild(Transform child)
    {
        RectTransform rect = child as RectTransform;
        if (rect == null)
            return;

        MenuUiFactory.FitArtRect(rect);
        if (child.TryGetComponent(out Image image))
            image.raycastTarget = false;

        if (child.TryGetComponent(out Button button))
            button.enabled = false;
    }

    private void CreateActions()
    {
        moveUp = CreateButtonAction("MainMenuMoveUp", HandleMoveUp, "<Keyboard>/upArrow", "<Keyboard>/w", "<Gamepad>/dpad/up");
        moveDown = CreateButtonAction("MainMenuMoveDown", HandleMoveDown, "<Keyboard>/downArrow", "<Keyboard>/s", "<Gamepad>/dpad/down");
        confirm = CreateButtonAction("MainMenuConfirm", HandleConfirm, "<Keyboard>/enter", "<Keyboard>/numpadEnter", "<Keyboard>/space", "<Gamepad>/buttonSouth");
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
        if (sceneLoadRequested)
            return;

        MenuSfx.PlaySelect();
        SetSelection(VictoryScreenController.MoveSelection(selectedIndex, -1, OptionCount));
    }

    private void HandleMoveDown(InputAction.CallbackContext context)
    {
        if (sceneLoadRequested)
            return;

        MenuSfx.PlaySelect();
        SetSelection(VictoryScreenController.MoveSelection(selectedIndex, 1, OptionCount));
    }

    private void HandleConfirm(InputAction.CallbackContext context)
    {
        if (sceneLoadRequested)
            return;

        if (selectedIndex == QuitOption)
        {
            sceneLoadRequested = true;
            MenuSfx.PlayBack();
            MenuSceneLoader.QuitApplication();
            return;
        }

        GoToMapSelection();
    }

    private void SetSelection(int index)
    {
        selectedIndex = index;
        ShowSelection();
    }

    private void ShowSelection()
    {
        float offset = selectedIndex == PartidaOption ? 0f : -CursorRowSpacingPixels;
        MenuUiFactory.PlaceArtCursor(cursorRect, offset);
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
