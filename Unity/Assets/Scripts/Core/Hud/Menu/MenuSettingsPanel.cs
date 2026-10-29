using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Builds the live settings submenu using the public preference contract.</summary>
public static class MenuSettingsPanel
{
    private const int LabelFontSize = 20;
    private const int ValueFontSize = 20;
    private const float VolumeMinimum = 0f;
    private const float VolumeMaximum = 1f;
    private const float LookSensitivityMinimum = 0.1f;
    private const float LookSensitivityMaximum = 5f;
    private const float ShakeIntensityMinimum = 0f;
    private const float ShakeIntensityMaximum = 2f;
    private const float AdjustmentStep = 0.1f;

    /// <summary>Creates settings controls and a back button beneath the supplied parent.</summary>
    public static GameObject Create(Transform parent, UnityAction onBack)
    {
        GameObject panel = MenuUiFactory.CreatePanel(parent, "Configuración");
        MenuUiFactory.CreateLabel(panel.transform, "CONFIGURACIÓN", 36);

        GameObject tabRow = CreateHorizontalRow(panel.transform, "SettingsTabs", 62f);
        Button audioTab = MenuUiFactory.CreateButton(tabRow.transform, "Audio y controles", null);
        Button displayTab = MenuUiFactory.CreateButton(tabRow.transform, "Pantalla", null);
        GameObject audioPage = CreateVerticalPage(panel.transform, "AudioYControles");
        GameObject displayPage = CreateVerticalPage(panel.transform, "Pantalla");

        CreateFloatControl(audioPage.transform, "Volumen general", MenuPreferences.MasterVolume,
            VolumeMinimum, VolumeMaximum, MenuPreferences.SetMasterVolume, FormatPercentage);
        CreateFloatControl(audioPage.transform, "Volumen de música", MenuPreferences.MusicVolume,
            VolumeMinimum, VolumeMaximum, MenuPreferences.SetMusicVolume, FormatPercentage);
        CreateFloatControl(audioPage.transform, "Volumen de efectos", MenuPreferences.EffectsVolume,
            VolumeMinimum, VolumeMaximum, MenuPreferences.SetEffectsVolume, FormatPercentage);
        CreateFloatControl(audioPage.transform, "Sensibilidad de cámara", MenuPreferences.LookSensitivity,
            LookSensitivityMinimum, LookSensitivityMaximum, MenuPreferences.SetLookSensitivity, FormatDecimal);
        CreateFloatControl(audioPage.transform, "Intensidad de movimiento", MenuPreferences.ShakeIntensity,
            ShakeIntensityMinimum, ShakeIntensityMaximum, MenuPreferences.SetShakeIntensity, FormatDecimal);
        CreateToggle(audioPage.transform, "Invertir eje vertical", MenuPreferences.InvertY, MenuPreferences.SetInvertY);

        CreateDisplayControls(displayPage.transform);
        displayPage.SetActive(false);

        MenuSettingsPanelNavigator navigator = panel.AddComponent<MenuSettingsPanelNavigator>();
        navigator.Initialize(audioPage, displayPage);
        Bind(audioTab, () => navigator.ShowPage(audioPage));
        Bind(displayTab, () => navigator.ShowPage(displayPage));
        MenuUiFactory.CreateButton(panel.transform, "Volver", onBack);
        MenuUiFactory.FocusFirst(panel);
        return panel;
    }

    private static void CreateFloatControl(Transform parent, string title, float initialValue, float minimum,
        float maximum, UnityAction<float> setter, Func<float, string> formatValue)
    {
        GameObject row = CreateHorizontalRow(parent, title, 62f);
        MenuUiFactory.CreateLabel(row.transform, title, LabelFontSize);
        Text valueLabel = MenuUiFactory.CreateLabel(row.transform, formatValue(initialValue), ValueFontSize);
        Button decreaseButton = MenuUiFactory.CreateButton(row.transform, "−", null);
        Button increaseButton = MenuUiFactory.CreateButton(row.transform, "+", null);
        ConfigureCompactControl(row, valueLabel, decreaseButton, increaseButton);

        float currentValue = Mathf.Clamp(initialValue, minimum, maximum);
        Bind(decreaseButton, () => ChangeValue(-AdjustmentStep));
        Bind(increaseButton, () => ChangeValue(AdjustmentStep));
        void ChangeValue(float delta)
        {
            currentValue = Mathf.Clamp(currentValue + delta, minimum, maximum);
            setter(currentValue);
            valueLabel.text = formatValue(currentValue);
        }
    }

    private static void CreateToggle(Transform parent, string title, bool initialValue, UnityAction<bool> setter)
    {
        Button button = MenuUiFactory.CreateButton(parent, FormatToggle(title, initialValue), null);
        Bind(button, () =>
        {
            initialValue = !initialValue;
            setter(initialValue);
            SetButtonText(button, FormatToggle(title, initialValue));
        });
    }

    private static void CreateDisplayControls(Transform parent)
    {
        bool pendingFullscreen = MenuPreferences.Fullscreen;
        bool pendingVSync = MenuPreferences.VSync;
        string[] qualityNames = QualitySettings.names;
        bool hasQuality = qualityNames != null && qualityNames.Length > 0;
        int pendingQuality = hasQuality
            ? Mathf.Clamp(MenuPreferences.QualityLevel, 0, qualityNames.Length - 1)
            : 0;
        List<Resolution> uniqueResolutions = CollectUniqueResolutions();
        int pendingResolution = uniqueResolutions.FindIndex(item =>
            item.width == MenuPreferences.ResolutionWidth && item.height == MenuPreferences.ResolutionHeight);

        Button fullscreenButton = MenuUiFactory.CreateButton(
            parent, FormatToggle("Pantalla completa", pendingFullscreen), null);
        Bind(fullscreenButton, () =>
        {
            pendingFullscreen = !pendingFullscreen;
            SetButtonText(fullscreenButton, FormatToggle("Pantalla completa", pendingFullscreen));
        });

        Text qualityValue = null;
        if (hasQuality)
        {
            GameObject row = CreateHorizontalRow(parent, "Calidad", 62f);
            MenuUiFactory.CreateLabel(row.transform, "Calidad gráfica", LabelFontSize);
            qualityValue = MenuUiFactory.CreateLabel(row.transform, qualityNames[pendingQuality], ValueFontSize);
            Button previousButton = MenuUiFactory.CreateButton(row.transform, "−", null);
            Button nextButton = MenuUiFactory.CreateButton(row.transform, "+", null);
            ConfigureCompactControl(row, qualityValue, previousButton, nextButton);
            Bind(previousButton, () => StepQuality(-1));
            Bind(nextButton, () => StepQuality(1));
        }

        Text resolutionValue = null;
        if (uniqueResolutions.Count > 0)
        {
            string initialResolution = pendingResolution >= 0
                ? FormatResolution(uniqueResolutions[pendingResolution])
                : $"{MenuPreferences.ResolutionWidth} × {MenuPreferences.ResolutionHeight}";
            GameObject row = CreateHorizontalRow(parent, "Resolución", 62f);
            MenuUiFactory.CreateLabel(row.transform, "Resolución", LabelFontSize);
            resolutionValue = MenuUiFactory.CreateLabel(row.transform, initialResolution, ValueFontSize);
            Button previousButton = MenuUiFactory.CreateButton(row.transform, "−", null);
            Button nextButton = MenuUiFactory.CreateButton(row.transform, "+", null);
            ConfigureCompactControl(row, resolutionValue, previousButton, nextButton);
            Bind(previousButton, () => StepResolution(-1));
            Bind(nextButton, () => StepResolution(1));
        }

        Button vsyncButton = MenuUiFactory.CreateButton(
            parent, FormatToggle("Sincronización vertical", pendingVSync), null);
        Bind(vsyncButton, () =>
        {
            pendingVSync = !pendingVSync;
            SetButtonText(vsyncButton, FormatToggle("Sincronización vertical", pendingVSync));
        });

        Button applyButton = MenuUiFactory.CreateButton(parent, "Aplicar pantalla", null);
        Bind(applyButton, () =>
        {
            MenuPreferences.SetFullscreen(pendingFullscreen);
            if (hasQuality)
                MenuPreferences.SetQualityLevel(pendingQuality);
            if (pendingResolution >= 0 && pendingResolution < uniqueResolutions.Count)
            {
                Resolution resolution = uniqueResolutions[pendingResolution];
                MenuPreferences.SetResolution(resolution.width, resolution.height);
            }

            MenuPreferences.SetVSync(pendingVSync);
            SetButtonText(applyButton, "Pantalla aplicada");
        });

        void StepQuality(int direction)
        {
            if (!hasQuality || qualityValue == null)
                return;
            pendingQuality = (pendingQuality + direction + qualityNames.Length) % qualityNames.Length;
            qualityValue.text = qualityNames[pendingQuality];
        }

        void StepResolution(int direction)
        {
            if (uniqueResolutions.Count == 0 || resolutionValue == null)
                return;
            if (pendingResolution < 0)
                pendingResolution = direction > 0 ? -1 : 0;
            pendingResolution = (pendingResolution + direction + uniqueResolutions.Count) % uniqueResolutions.Count;
            resolutionValue.text = FormatResolution(uniqueResolutions[pendingResolution]);
        }
    }

    private static List<Resolution> CollectUniqueResolutions()
    {
        List<Resolution> uniqueResolutions = new List<Resolution>();
        Resolution[] availableResolutions = Screen.resolutions;
        if (availableResolutions == null)
            return uniqueResolutions;

        foreach (Resolution resolution in availableResolutions)
        {
            bool alreadyAdded = uniqueResolutions.Exists(item =>
                item.width == resolution.width && item.height == resolution.height);
            if (!alreadyAdded)
                uniqueResolutions.Add(resolution);
        }

        return uniqueResolutions;
    }

    private static GameObject CreateHorizontalRow(Transform parent, string name, float preferredHeight)
    {
        GameObject row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        row.GetComponent<LayoutElement>().preferredHeight = preferredHeight;
        return row;
    }

    private static GameObject CreateVerticalPage(Transform parent, string name)
    {
        GameObject page = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        page.transform.SetParent(parent, false);
        VerticalLayoutGroup layout = page.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return page;
    }

    private static void ConfigureCompactControl(GameObject row, Text valueLabel, Button decreaseButton, Button increaseButton)
    {
        LayoutElement titleLayout = row.transform.GetChild(0).GetComponent<LayoutElement>();
        LayoutElement valueLayout = valueLabel.GetComponent<LayoutElement>();
        titleLayout.flexibleWidth = 3f;
        valueLayout.flexibleWidth = 2f;
        valueLayout.preferredHeight = 48f;
        SetButtonText(decreaseButton, "−");
        SetButtonText(increaseButton, "+");
        decreaseButton.GetComponent<LayoutElement>().flexibleWidth = 0.7f;
        increaseButton.GetComponent<LayoutElement>().flexibleWidth = 0.7f;
    }

    private static void Bind(Button button, UnityAction action)
    {
        MenuSettingsActionBinding binding = button.gameObject.AddComponent<MenuSettingsActionBinding>();
        binding.Bind(button, action);
    }

    private static string FormatToggle(string title, bool value)
    {
        return $"{title}: {(value ? "Sí" : "No")}";
    }

    private static string FormatPercentage(float value)
    {
        return $"{Mathf.RoundToInt(value * 100f)}%";
    }

    private static string FormatDecimal(float value)
    {
        return value.ToString("0.0");
    }

    private static string FormatResolution(Resolution resolution)
    {
        return $"{resolution.width} × {resolution.height}";
    }

    private static void SetButtonText(Button button, string text)
    {
        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
            label.text = text;
    }
}

