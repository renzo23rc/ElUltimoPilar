using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Builds and navigates the runtime main menu.</summary>
public sealed class MenuSceneController : MonoBehaviour
{
    private const int TitleFontSize = 52;
    private const int SubtitleFontSize = 22;
    private const int BodyFontSize = 23;
    private const float ScoresSummaryHeight = 420f;
    private const float HelpSummaryHeight = 330f;
    private const string MenuCameraName = "MenuCamera";
    private const float MenuCameraNearMeters = 0.1f;
    private const float MenuCameraFarMeters = 10f;
    private readonly List<GameObject> panels = new List<GameObject>();
    private readonly List<ButtonBinding> buttonBindings = new List<ButtonBinding>();
    private GameObject mainPanel;
    private GameObject playPanel;
    private GameObject scoresPanel;
    private GameObject helpPanel;
    private GameObject settingsPanel;
    private Text scoresSummary;
    private Text helpSummary;
    private int helpPage;

    private void Start()
    {
        MenuUiFactory.EnsureEventSystem();
        EnsureMenuCamera();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Canvas canvas = MenuUiFactory.CreateCanvas("MainMenuCanvas");
        CreateMainPanel(canvas.transform);
        CreatePlayPanel(canvas.transform);
        CreateScoresPanel(canvas.transform);
        CreateHelpPanel(canvas.transform);
        settingsPanel = MenuSettingsPanel.Create(canvas.transform, ShowMainMenu);
        panels.Add(settingsPanel);
        ShowPanel(mainPanel);
    }

    private static void EnsureMenuCamera()
    {
        if (Object.FindFirstObjectByType<Camera>() != null)
            return;

        GameObject cameraObject = new GameObject(MenuCameraName);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
        camera.nearClipPlane = MenuCameraNearMeters;
        camera.farClipPlane = MenuCameraFarMeters;
        camera.useOcclusionCulling = false;
    }

    private void OnDestroy()
    {
        foreach (ButtonBinding binding in buttonBindings)
        {
            if (binding.Button != null)
                binding.Button.onClick.RemoveListener(binding.Action);
        }

        buttonBindings.Clear();
    }

    private void CreateMainPanel(Transform parent)
    {
        mainPanel = CreatePanel(parent, "MainMenu");
        MenuUiFactory.CreateLabel(mainPanel.transform, "ÚLTIMO PILAR", TitleFontSize);
        MenuUiFactory.CreateLabel(mainPanel.transform, "Defendé el Pilar. Sobreviví a las oleadas.", SubtitleFontSize);
        CreateButton(mainPanel.transform, "Jugar", ShowPlayMenu);
        CreateButton(mainPanel.transform, "Configuración", ShowSettings);
        CreateButton(mainPanel.transform, "Mejores puntajes", ShowScores);
        CreateButton(mainPanel.transform, "Cómo jugar", ShowHelp);
        CreateButton(mainPanel.transform, "Salir", MenuSceneLoader.QuitApplication);
    }

    private void CreatePlayPanel(Transform parent)
    {
        playPanel = CreatePanel(parent, "PlayMenu");
        MenuUiFactory.CreateLabel(playPanel.transform, "ELEGÍ UNA ESCENA", TitleFontSize - 12);
        CreateButton(playPanel.transform, "Obelisco", MenuSceneLoader.LoadSampleMatch);
        CreateButton(playPanel.transform, "EstatuaDeLaLibertad", MenuSceneLoader.LoadStatueMatch);
        CreateButton(playPanel.transform, "CristoRedentor", MenuSceneLoader.LoadCristoMatch);
        CreateButton(playPanel.transform, "Volver", ShowMainMenu);
    }

    private void CreateScoresPanel(Transform parent)
    {
        scoresPanel = CreatePanel(parent, "ScoresMenu");
        MenuUiFactory.CreateLabel(scoresPanel.transform, "MEJORES PUNTAJES", TitleFontSize - 12);
        scoresSummary = MenuUiFactory.CreateLabel(scoresPanel.transform, string.Empty, BodyFontSize);
        scoresSummary.horizontalOverflow = HorizontalWrapMode.Wrap;
        scoresSummary.verticalOverflow = VerticalWrapMode.Overflow;
        scoresSummary.GetComponent<LayoutElement>().preferredHeight = ScoresSummaryHeight;
        scoresSummary.resizeTextForBestFit = true;
        scoresSummary.resizeTextMinSize = 14;
        scoresSummary.resizeTextMaxSize = BodyFontSize;
        CreateButton(scoresPanel.transform, "Volver", ShowMainMenu);
    }

    private void CreateHelpPanel(Transform parent)
    {
        helpPanel = CreatePanel(parent, "HowToMenu");
        MenuUiFactory.CreateLabel(helpPanel.transform, "CÓMO JUGAR", TitleFontSize - 12);
        helpSummary = MenuUiFactory.CreateLabel(helpPanel.transform, string.Empty, 18);
        helpSummary.alignment = TextAnchor.MiddleLeft;
        helpSummary.resizeTextForBestFit = true;
        helpSummary.resizeTextMinSize = 14;
        helpSummary.resizeTextMaxSize = 18;
        helpSummary.GetComponent<LayoutElement>().preferredHeight = HelpSummaryHeight;
        CreateButton(helpPanel.transform, "Siguiente", ShowNextHelpPage);
        CreateButton(helpPanel.transform, "Volver", ShowMainMenu);
        UpdateHelpPage();
    }

    private GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = MenuUiFactory.CreatePanel(parent, name);
        panels.Add(panel);
        return panel;
    }

    private void CreateButton(Transform parent, string text, UnityEngine.Events.UnityAction action)
    {
        Button button = MenuUiFactory.CreateButton(parent, text, null);
        button.onClick.AddListener(action);
        buttonBindings.Add(new ButtonBinding(button, action));
    }

    private void ShowMainMenu()
    {
        ShowPanel(mainPanel);
    }

    private void ShowPlayMenu()
    {
        ShowPanel(playPanel);
    }

    private void ShowSettings()
    {
        ShowPanel(settingsPanel);
    }

    private void ShowScores()
    {
        scoresSummary.text = MenuScores.GetSummary();
        ShowPanel(scoresPanel);
    }

    private void ShowHelp()
    {
        helpPage = 0;
        UpdateHelpPage();
        ShowPanel(helpPanel);
    }

    private void ShowNextHelpPage()
    {
        helpPage = (helpPage + 1) % 2;
        UpdateHelpPage();
    }

    private void UpdateHelpPage()
    {
        bool keyboardPage = helpPage == 0;
        helpSummary.text = keyboardPage
            ? "TECLADO Y MOUSE\nWASD: mover · Mouse: mirar · Clic izquierdo: disparar\nShift: correr · Space: saltar · E (mantener): reanimar\n1–3: seleccionar arma\nH: curar · J: habilidad · Esc: pausar"
            : "MANDO Y MENÚS\nStick izquierdo: mover · Stick derecho: mirar\nPulsar stick izquierdo: correr · RT: disparar\nA (Xbox) / X (PlayStation): saltar\nLB/RB: arma anterior/siguiente · Cruceta arriba: curar\nCruceta abajo: habilidad · Botón norte: interactuar/reanimar\nStart: pausar · En inicio, Start/Options suma mandos\nEn menús, navegá y confirmá la opción seleccionada.";
        Button nextButton = helpPanel.transform.GetChild(helpPanel.transform.childCount - 2).GetComponent<Button>();
        Text nextLabel = nextButton.GetComponentInChildren<Text>();
        if (nextLabel != null)
            nextLabel.text = keyboardPage ? "Siguiente" : "Anterior";
    }

    private void ShowPanel(GameObject selectedPanel)
    {
        foreach (GameObject panel in panels)
            panel.SetActive(panel == selectedPanel);

        MenuUiFactory.FocusFirst(selectedPanel);
    }

    private sealed class ButtonBinding
    {
        public ButtonBinding(Button button, UnityEngine.Events.UnityAction action)
        {
            Button = button;
            Action = action;
        }

        public Button Button { get; }
        public UnityEngine.Events.UnityAction Action { get; }
    }
}
