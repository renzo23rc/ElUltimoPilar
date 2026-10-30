using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Starts Play mode from the player's main menu instead of the scene open in the editor.</summary>
[InitializeOnLoad]
internal static class PlayFromMainMenu
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    static PlayFromMainMenu()
    {
        EditorApplication.delayCall += Apply;
    }

    private static void Apply()
    {
        SceneAsset mainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
        if (mainMenu == null || EditorSceneManager.playModeStartScene == mainMenu)
            return;

        EditorSceneManager.playModeStartScene = mainMenu;
    }
}
