using UnityEngine;

/// <summary>Owns settings page visibility and restores focus whenever a page changes.</summary>
public sealed class MenuSettingsPanelNavigator : MonoBehaviour
{
    private GameObject audioPage;
    private GameObject displayPage;

    public void Initialize(GameObject audioPageObject, GameObject displayPageObject)
    {
        audioPage = audioPageObject;
        displayPage = displayPageObject;
    }

    public void ShowPage(GameObject page)
    {
        audioPage.SetActive(page == audioPage);
        displayPage.SetActive(page == displayPage);
        MenuUiFactory.FocusFirst(page);
    }
}
