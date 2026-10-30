using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Loads the menu and the existing playable scenes by their asset names.</summary>
public static class MenuSceneLoader
{
    private const string MenuSceneName = "Menu";
    private const string SampleMatchSceneName = "Obelisco";
    private const string StatueMatchSceneName = "EstatuaDeLaLibertad";
    private const string CristoMatchSceneName = "CristoRedentor";

    /// <summary>Loads the menu and restores normal time after leaving a paused match.</summary>
    public static void LoadMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MenuSceneName, LoadSceneMode.Single);
    }

    /// <summary>Loads the existing sample match scene.</summary>
    public static void LoadSampleMatch()
    {
        LoadMatchScene(SampleMatchSceneName);
    }

    /// <summary>Loads the existing Statue of Liberty match scene.</summary>
    public static void LoadStatueMatch()
    {
        LoadMatchScene(StatueMatchSceneName);
    }

    /// <summary>Loads the Cristo Redentor match scene.</summary>
    public static void LoadCristoMatch()
    {
        LoadMatchScene(CristoMatchSceneName);
    }

    /// <summary>Requests application exit; in the Editor it stops Play mode.</summary>
    public static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void LoadMatchScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
