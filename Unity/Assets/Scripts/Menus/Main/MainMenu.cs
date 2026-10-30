using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    const string PlayButtonName = "PlayButton";

    [Header("Escenas")]
    [SerializeField] private string mapSelectionSceneName = "MapSelection";

    [Header("Botones")]
    [SerializeField] private Button playButton;

    InputAction confirmPlay;
    bool sceneLoadRequested;

    void Awake()
    {
        EnsureEventSystem();
        EnsureRaycasters();
        WirePlayButton();
        CreateConfirmAction();
    }

    void OnEnable()
    {
        confirmPlay?.Enable();
    }

    void OnDisable()
    {
        confirmPlay?.Disable();
    }

    void OnDestroy()
    {
        if (confirmPlay == null)
            return;

        confirmPlay.performed -= HandleConfirmPlay;
        confirmPlay.Dispose();
        confirmPlay = null;
    }

    void WirePlayButton()
    {
        if (playButton == null)
            playButton = FindPlayButton();

        if (playButton == null)
        {
            Debug.LogError("MainMenu: no hay un boton llamado PlayButton.");
            return;
        }

        LimitClicksToVisiblePixels(playButton);

        if (!IsWiredInInspector(playButton))
        {
            playButton.onClick.RemoveListener(GoToMapSelection);
            playButton.onClick.AddListener(GoToMapSelection);
        }
    }

    void CreateConfirmAction()
    {
        confirmPlay = new InputAction("MainMenuConfirm", InputActionType.Button);
        confirmPlay.AddBinding("<Keyboard>/enter");
        confirmPlay.AddBinding("<Keyboard>/numpadEnter");
        confirmPlay.AddBinding("<Gamepad>/buttonSouth");
        confirmPlay.performed += HandleConfirmPlay;
    }

    void HandleConfirmPlay(InputAction.CallbackContext context)
    {
        GoToMapSelection();
    }

    public void GoToMapSelection()
    {
        if (sceneLoadRequested)
            return;

        if (!Application.CanStreamedLevelBeLoaded(mapSelectionSceneName))
        {
            Debug.LogError("La escena '" + mapSelectionSceneName + "' no esta en File > Build Settings.");
            return;
        }

        sceneLoadRequested = true;
        SceneManager.LoadScene(mapSelectionSceneName);
    }

    public static Button FindPlayButton()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].gameObject.name == PlayButtonName)
                return buttons[i];
        }

        return null;
    }

    static bool IsWiredInInspector(Button button)
    {
        int count = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            if (button.onClick.GetPersistentMethodName(i) == nameof(GoToMapSelection)
                && button.onClick.GetPersistentTarget(i) != null)
                return true;
        }

        return false;
    }

    public static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

        StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacy == null)
            return;

        if (Application.isPlaying)
            Destroy(legacy);
        else
            DestroyImmediate(legacy);
    }

    static void EnsureRaycasters()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].GetComponent<GraphicRaycaster>() == null)
                canvases[i].gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    static void LimitClicksToVisiblePixels(Button button)
    {
        if (!button.TryGetComponent(out Image image) || image.sprite == null)
            return;

        try
        {
            image.alphaHitTestMinimumThreshold = 0.1f;
        }
        catch (System.InvalidOperationException)
        {
            // La textura no es readable: el rectangulo entero del boton sigue recibiendo el clic.
        }
    }
}
