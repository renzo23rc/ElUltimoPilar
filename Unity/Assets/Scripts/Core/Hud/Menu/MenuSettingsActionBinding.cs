using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Removes a dynamically installed button callback with its owning UI object.</summary>
public sealed class MenuSettingsActionBinding : MonoBehaviour
{
    private Button button;
    private UnityAction action;

    public void Bind(Button targetButton, UnityAction callback)
    {
        button = targetButton;
        action = callback;
        button.onClick.AddListener(action);
    }

    private void OnDestroy()
    {
        if (button != null && action != null)
            button.onClick.RemoveListener(action);
    }
}
