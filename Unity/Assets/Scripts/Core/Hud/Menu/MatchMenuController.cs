using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Composes and controls the interactive match menus for a runtime scene.
/// </summary>
public sealed class MatchMenuController : MonoBehaviour
{
    private const int MenuFontSize = 32;
    private const int DetailsFontSize = 22;
    private const int ConfirmationFontSize = 28;
    private const string WaitingTitle = "ÚLTIMO PILAR";
    private const string StartLabel = "Iniciar noche";
    private const string SettingsLabel = "Configuración";
    private const string BackLabel = "Volver al menú";
    private const string ContinueLabel = "Continuar";
    private const string RestartLabel = "Reiniciar";
    private const string AbandonLabel = "Abandonar";
    private const string ReplayLabel = "Jugar otra noche";
    private const string ConfirmLabel = "Sí, continuar";
    private const string CancelLabel = "No, volver";

    private GameManager manager;
    private InputAction menuBackAction;
    private int menuBackPressedFrame = -1;
    private int pausedTransitionFrame = -1;
    private Hud hud;
    private GameObject waitingPanel;
    private GameObject pausePanel;
    private GameObject resultPanel;
    private GameObject confirmationPanel;
    private GameObject settingsPanel;
    private Text waitingDetailsLabel;
    private Text resultDetailsLabel;
    private Text confirmationLabel;
    private MatchState visibleState;
    private bool settingsVisible;
    private ConfirmationAction confirmationAction;
    private bool resultRecorded;

    /// <summary>Gets whether a menu overlay currently captures gameplay input.</summary>
    public bool IsOverlayCapturingInput => manager != null && IsOverlayVisible();

    /// <summary>Gets whether the waiting overlay is visible and permits a player to join.</summary>
    public bool AllowsJoining => manager != null && waitingPanel != null && waitingPanel.activeSelf &&
        !settingsVisible && !confirmationPanel.activeSelf;

    private enum ConfirmationAction
    {
        None,
        Restart,
        Abandon
    }

    /// <summary>Initializes the composed menu and connects it to the match APIs.</summary>
    public void Configure(GameManager gameManager, Hud matchHud, Canvas canvas)
    {
        manager = gameManager;
        hud = matchHud;
        Build(canvas);
        manager.SetMenuInputFocused(true);
        manager.OnMatchResult += HandleMatchResult;
        CreateMenuBackAction();
        RenderState(true);
    }

    private void Build(Canvas canvas)
    {
        MenuUiFactory.EnsureEventSystem();
        Transform root = canvas.transform;
        waitingPanel = MenuUiFactory.CreatePanel(root, "WaitingMenu");
        MenuUiFactory.CreateLabel(waitingPanel.transform, WaitingTitle, MenuFontSize);
        waitingDetailsLabel = MenuUiFactory.CreateLabel(waitingPanel.transform, string.Empty, DetailsFontSize);
        MenuUiFactory.CreateButton(waitingPanel.transform, StartLabel, StartMatch);
        MenuUiFactory.CreateButton(waitingPanel.transform, SettingsLabel, OpenSettings);
        MenuUiFactory.CreateButton(waitingPanel.transform, BackLabel, ReturnToMenu);

        pausePanel = MenuUiFactory.CreatePanel(root, "PauseMenu");
        MenuUiFactory.CreateLabel(pausePanel.transform, "PAUSA", MenuFontSize);
        MenuUiFactory.CreateButton(pausePanel.transform, ContinueLabel, ResumeMatch);
        MenuUiFactory.CreateButton(pausePanel.transform, RestartLabel, ConfirmRestart);
        MenuUiFactory.CreateButton(pausePanel.transform, SettingsLabel, OpenSettings);
        MenuUiFactory.CreateButton(pausePanel.transform, AbandonLabel, ConfirmAbandon);

        resultPanel = MenuUiFactory.CreatePanel(root, "ResultMenu");
        MenuUiFactory.CreateLabel(resultPanel.transform, "RESULTADO", MenuFontSize);
        resultDetailsLabel = MenuUiFactory.CreateLabel(resultPanel.transform, string.Empty, DetailsFontSize);
        MenuUiFactory.CreateButton(resultPanel.transform, ReplayLabel, RestartMatch);
        MenuUiFactory.CreateButton(resultPanel.transform, BackLabel, ReturnToMenu);

        confirmationPanel = MenuUiFactory.CreatePanel(root, "ConfirmationMenu");
        confirmationLabel = MenuUiFactory.CreateLabel(confirmationPanel.transform, string.Empty, ConfirmationFontSize);
        MenuUiFactory.CreateButton(confirmationPanel.transform, ConfirmLabel, ConfirmPendingAction);
        MenuUiFactory.CreateButton(confirmationPanel.transform, CancelLabel, CancelConfirmation);

        settingsPanel = MenuSettingsPanel.Create(root, CloseSettings);
        settingsPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (menuBackAction != null)
            menuBackAction.Enable();
    }

    private void Update()
    {
        if (manager == null)
            return;

        MatchState state = manager.EstadoActual;
        if (state != visibleState)
        {
            if (state == MatchState.Paused && visibleState == MatchState.Playing)
                pausedTransitionFrame = Time.frameCount;
            if (state == MatchState.WaitingToStart)
                resultRecorded = false;
            RenderState(true);
        }

        manager.SetMenuInputFocused(IsOverlayVisible());
        UpdateCursor(state);
        ProcessMenuShortcuts();
    }

    private void RenderState(bool focus)
    {
        visibleState = manager.EstadoActual;
        bool terminal = visibleState == MatchState.Victory || visibleState == MatchState.Defeat;
        bool playerResultScreen = UsesPlayerResultScreen(visibleState);
        waitingPanel.SetActive(visibleState == MatchState.WaitingToStart);
        pausePanel.SetActive(visibleState == MatchState.Paused);
        resultPanel.SetActive(terminal && !playerResultScreen);
        confirmationPanel.SetActive(false);
        settingsPanel.SetActive(false);
        settingsVisible = false;
        confirmationAction = ConfirmationAction.None;

        if (visibleState == MatchState.WaitingToStart)
        {
            waitingDetailsLabel.text = "Iniciar noche\nStart/Options para sumar mandos";
            if (focus)
                MenuUiFactory.FocusFirst(waitingPanel);
        }
        else if (terminal && !playerResultScreen)
        {
            MatchResult result = manager.CurrentResult;
            string score = result == null ? "Puntaje no disponible" : $"Puntaje: {result.Score}";
            resultDetailsLabel.text = $"{(visibleState == MatchState.Victory ? "¡VICTORIA!" : "DERROTA")}\n{score}\nOleada: {manager.oleadaActual}/{manager.totalOleadas}";
            if (focus)
                MenuUiFactory.FocusFirst(resultPanel);
        }
        else if (visibleState == MatchState.Paused && focus)
        {
            MenuUiFactory.FocusFirst(pausePanel);
        }

        bool overlay = IsOverlayVisible();
        manager.SetMenuInputFocused(overlay);
        if (hud != null)
            hud.SetMenuComposed(true);
    }

    private static bool UsesPlayerResultScreen(MatchState state)
    {
        if (state == MatchState.Victory)
            return Application.CanStreamedLevelBeLoaded(VictoryScreenController.SceneName);

        if (state == MatchState.Defeat)
            return Application.CanStreamedLevelBeLoaded(DefeatScreenController.SceneName);

        return false;
    }

    private bool IsOverlayVisible()
    {
        return settingsVisible || confirmationPanel.activeSelf ||
            manager.EstadoActual != MatchState.Playing;
    }

    private void UpdateCursor(MatchState state)
    {
        bool gameplay = state == MatchState.Playing && !settingsVisible && !confirmationPanel.activeSelf;
        Cursor.lockState = gameplay ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !gameplay;
    }

    private void CreateMenuBackAction()
    {
        menuBackAction = new InputAction("MenuBack", InputActionType.Button);
        menuBackAction.AddBinding("<Keyboard>/escape");
        menuBackAction.AddBinding("<Gamepad>/start");
        menuBackAction.performed += HandleMenuBackPerformed;
        if (isActiveAndEnabled)
            menuBackAction.Enable();
    }

    private void HandleMenuBackPerformed(InputAction.CallbackContext context)
    {
        menuBackPressedFrame = Time.frameCount;
    }

    private void ProcessMenuShortcuts()
    {
        if (!manager.IsMenuInputCaptured || menuBackAction == null || !menuBackAction.triggered)
            return;

        // GameManager/player Pause owns the transition into Paused; ignore that same press here.
        if (manager.EstadoActual == MatchState.Paused && menuBackPressedFrame == Time.frameCount &&
            pausedTransitionFrame == Time.frameCount)
            return;

        if (settingsVisible)
        {
            CloseSettings();
            return;
        }

        if (confirmationPanel.activeSelf)
        {
            CancelConfirmation();
            return;
        }

        if (manager.EstadoActual == MatchState.Paused)
            ResumeMatch();
    }

    private void StartMatch()
    {
        resultRecorded = false;
        manager.IniciarJuego();
        RenderState(false);
    }

    private void ResumeMatch()
    {
        manager.ReanudarJuego();
        RenderState(false);
    }

    private void RestartMatch()
    {
        resultRecorded = false;
        manager.ReiniciarJuego();
        RenderState(false);
    }

    private void OpenSettings()
    {
        waitingPanel.SetActive(false);
        pausePanel.SetActive(false);
        resultPanel.SetActive(false);
        settingsVisible = true;
        settingsPanel.SetActive(true);
        manager.SetMenuInputFocused(true);
        MenuUiFactory.FocusFirst(settingsPanel);
    }

    private void CloseSettings()
    {
        settingsVisible = false;
        settingsPanel.SetActive(false);
        RenderState(true);
    }

    private void ConfirmRestart()
    {
        ShowConfirmation(ConfirmationAction.Restart, "¿Reiniciar la noche? Se perderá el progreso actual.");
    }

    private void ConfirmAbandon()
    {
        ShowConfirmation(ConfirmationAction.Abandon, "¿Abandonar la noche? Se perderá el progreso actual.");
    }

    private void ShowConfirmation(ConfirmationAction action, string message)
    {
        pausePanel.SetActive(false);
        confirmationAction = action;
        confirmationPanel.SetActive(true);
        confirmationLabel.text = message;
        manager.SetMenuInputFocused(true);
        MenuUiFactory.FocusFirst(confirmationPanel);
    }

    private void ConfirmPendingAction()
    {
        ConfirmationAction action = confirmationAction;
        confirmationAction = ConfirmationAction.None;
        confirmationPanel.SetActive(false);
        if (action == ConfirmationAction.Restart)
            RestartMatch();
        else if (action == ConfirmationAction.Abandon)
            ReturnToMenu();
    }

    private void CancelConfirmation()
    {
        confirmationAction = ConfirmationAction.None;
        confirmationPanel.SetActive(false);
        pausePanel.SetActive(true);
        manager.SetMenuInputFocused(true);
        MenuUiFactory.FocusFirst(pausePanel);
    }

    private void ReturnToMenu()
    {
        MenuSceneLoader.LoadMenu();
    }

    private void HandleMatchResult(MatchResult result)
    {
        if (result == null || resultRecorded)
            return;

        resultRecorded = true;
        MenuScores.Record(result, manager.oleadaActual, manager.totalOleadas, manager.PlayerCount);
    }

    private void OnDisable()
    {
        if (menuBackAction != null)
            menuBackAction.Disable();
    }

    private void OnDestroy()
    {
        if (menuBackAction != null)
        {
            menuBackAction.performed -= HandleMenuBackPerformed;
            menuBackAction.Dispose();
            menuBackAction = null;
        }

        if (manager != null)
        {
            manager.OnMatchResult -= HandleMatchResult;
            manager.SetMenuInputFocused(false);
        }
    }
}
