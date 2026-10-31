using UnityEngine;

/// <summary>Shows the player's area weapon only while that weapon is equipped.</summary>
public sealed class PlayerVisualPresenter : MonoBehaviour
{
    [SerializeField] private GameObject areaWeapon;

    private WeaponSystem weapons;

    /// <summary>Assigns the area weapon attached to the player's visual.</summary>
    /// <param name="weapon">The weapon model, or null when no model is available.</param>
    public void Configure(GameObject weapon)
    {
        areaWeapon = weapon;
        UpdateVisibility(weapons != null ? weapons.armaEquipada : WeaponSystem.TipoArma.Directa);
    }

    private void OnEnable()
    {
        weapons = GetComponent<WeaponSystem>();
        if (weapons != null)
        {
            weapons.OnCambioArma += HandleWeaponChanged;
            UpdateVisibility(weapons.armaEquipada);
        }
    }

    private void OnDisable()
    {
        if (weapons != null)
        {
            weapons.OnCambioArma -= HandleWeaponChanged;
        }
    }

    private void HandleWeaponChanged(WeaponSystem.TipoArma weapon)
    {
        UpdateVisibility(weapon);
    }

    private void UpdateVisibility(WeaponSystem.TipoArma weapon)
    {
        if (areaWeapon != null)
        {
            areaWeapon.SetActive(weapon == WeaponSystem.TipoArma.Area);
        }
    }
}
