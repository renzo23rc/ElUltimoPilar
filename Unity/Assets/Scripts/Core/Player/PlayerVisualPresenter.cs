using UnityEngine;

/// <summary>Shows only the model corresponding to the player's equipped weapon.</summary>
public sealed class PlayerVisualPresenter : MonoBehaviour
{
    [SerializeField] private GameObject areaWeapon;
    [SerializeField] private GameObject directWeapon;
    [SerializeField] private GameObject meleeWeapon;

    private WeaponSystem weapons;

    /// <summary>Assigns the area weapon attached to the player's visual.</summary>
    /// <param name="weapon">The weapon model, or null when no model is available.</param>
    public void Configure(GameObject weapon)
    {
        areaWeapon = weapon;
        UpdateVisibility(weapons != null ? weapons.armaEquipada : WeaponSystem.TipoArma.Directa);
    }

    /// <summary>Checks whether a transform belongs to a configured weapon model.</summary>
    /// <param name="candidate">The transform to inspect, including weapon roots.</param>
    /// <returns>True if the transform belongs to one of the player's weapon models.</returns>
    public bool ContainsWeaponVisual(Transform candidate)
    {
        return candidate != null
            && (IsPartOf(candidate, areaWeapon) || IsPartOf(candidate, directWeapon) || IsPartOf(candidate, meleeWeapon));
    }

    private static bool IsPartOf(Transform candidate, GameObject weapon)
    {
        return weapon != null && candidate.IsChildOf(weapon.transform);
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

        if (directWeapon != null)
        {
            directWeapon.SetActive(weapon == WeaponSystem.TipoArma.Directa);
        }

        if (meleeWeapon != null)
        {
            meleeWeapon.SetActive(weapon == WeaponSystem.TipoArma.CuerpoACuerpo);
        }
    }
}
