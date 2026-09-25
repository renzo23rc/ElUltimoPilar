using UltimoPilar.Core.Shared;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Per-player HUD rendered through that player's camera, so each split-screen viewport
/// shows its own identity, health, energy, weapon, variant, crosshair, and revive prompt.
/// </summary>
public sealed class PlayerHud : MonoBehaviour
{
    private const float PlaneDistanceMeters = 0.5f;
    private const int CanvasSortingOrder = 10;
    private const float HudPadding = 20f;
    private const float KillCrosshairDurationSeconds = 0.35f;
    private const float HitCrosshairDurationSeconds = 0.15f;
    private const float RevivePromptExtraRangeMeters = 0.5f;
    private const float StatusVerticalOffset = -150f;
    private const float VariantOffsetFromBottom = 120f;
    private const int IdentityFontSize = 30;
    private const int StatFontSize = 24;
    private const int AmmoFontSize = 28;
    private const int WeaponFontSize = 22;
    private const int VariantFontSize = 26;
    private const int StatusFontSize = 28;
    private const int CrosshairFontSize = 34;
    private const string CrosshairGlyph = "+";
    private static readonly Vector2 IdentityOffset = new Vector2(HudPadding, 20f);
    private static readonly Vector2 HealthTextOffset = new Vector2(HudPadding, 62f);
    private static readonly Vector2 HealthBarOffset = new Vector2(HudPadding, 94f);
    private static readonly Vector2 EnergyTextOffset = new Vector2(HudPadding, 120f);
    private static readonly Vector2 EnergyBarOffset = new Vector2(HudPadding, 152f);
    private static readonly Vector2 AmmoOffset = new Vector2(HudPadding, 60f);
    private static readonly Vector2 WeaponOffset = new Vector2(HudPadding, 24f);
    private static readonly Vector2 LabelSize = new Vector2(520f, 40f);
    private static readonly Vector2 BarSize = new Vector2(300f, 18f);
    private static readonly Vector2 WideLabelSize = new Vector2(700f, 44f);
    private static readonly Vector2 CrosshairSize = new Vector2(50f, 50f);
    private static readonly Color VariantColor = new Color(1f, 0.55f, 0.1f);
    private static readonly Color EnergyColor = new Color(0.2f, 0.8f, 1f);
    private static readonly Color[] PlayerColors =
    {
        new Color(0.3f, 0.85f, 1f),
        new Color(1f, 0.6f, 0.2f),
        new Color(0.45f, 1f, 0.45f),
        new Color(1f, 0.45f, 0.9f)
    };

    private PlayerController player;
    private EnergySystem energy;
    private WeaponSystem weapons;
    private Text identityText;
    private Text healthText;
    private Image healthBar;
    private Text energyText;
    private Image energyBar;
    private Text ammoText;
    private Text weaponText;
    private Text variantText;
    private Text statusText;
    private Text crosshairText;
    private float crosshairTimer;

    /// <summary>Gets the player this HUD belongs to.</summary>
    public PlayerController Player => player;

    /// <summary>Creates the HUD for a player under the given parent.</summary>
    public static PlayerHud Create(PlayerController player, Transform parent)
    {
        var hudObject = new GameObject($"PlayerHud_{player.name}", typeof(RectTransform));
        hudObject.transform.SetParent(parent, false);
        PlayerHud hud = hudObject.AddComponent<PlayerHud>();
        hud.Bind(player);
        return hud;
    }

    /// <summary>Returns the identity color of a zero-based roster slot.</summary>
    public static Color GetPlayerColor(int slot)
    {
        return PlayerColors[Mathf.Abs(slot) % PlayerColors.Length];
    }

    /// <summary>Flashes the crosshair for a hit or a kill.</summary>
    public void FlashCrosshair(bool killed)
    {
        crosshairTimer = killed ? KillCrosshairDurationSeconds : HitCrosshairDurationSeconds;
        if (crosshairText != null)
            crosshairText.color = killed ? Color.red : Color.yellow;
    }

    private void Bind(PlayerController target)
    {
        player = target;
        energy = target.GetComponent<EnergySystem>();
        weapons = target.GetComponent<WeaponSystem>();
        BuildCanvas();
        CombatFeedback.OnCombatHit += FlashCrosshair;
    }

    private void OnDestroy()
    {
        CombatFeedback.OnCombatHit -= FlashCrosshair;
    }

    private void BuildCanvas()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        // Screen Space - Camera: el canvas se dibuja solo en el viewport de la cámara del jugador.
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = player.camaraJugador;
        canvas.planeDistance = PlaneDistanceMeters;
        canvas.sortingOrder = CanvasSortingOrder;
        HudUiFactory.ConfigureScaler(gameObject.AddComponent<CanvasScaler>());

        Transform root = transform;
        identityText = HudUiFactory.CreateText(root, "Identidad", IdentityFontSize, TextAnchor.UpperLeft);
        HudUiFactory.AnchorTopLeft(identityText.rectTransform, IdentityOffset, LabelSize);
        healthText = HudUiFactory.CreateText(root, "Vida", StatFontSize, TextAnchor.UpperLeft);
        HudUiFactory.AnchorTopLeft(healthText.rectTransform, HealthTextOffset, LabelSize);
        healthBar = HudUiFactory.CreateBar(root, "BarraVida");
        HudUiFactory.AnchorBar(healthBar, rect => HudUiFactory.AnchorTopLeft(rect, HealthBarOffset, BarSize));
        energyText = HudUiFactory.CreateText(root, "Energia", StatFontSize, TextAnchor.UpperLeft);
        HudUiFactory.AnchorTopLeft(energyText.rectTransform, EnergyTextOffset, LabelSize);
        energyBar = HudUiFactory.CreateBar(root, "BarraEnergia");
        energyBar.color = EnergyColor;
        HudUiFactory.AnchorBar(energyBar, rect => HudUiFactory.AnchorTopLeft(rect, EnergyBarOffset, BarSize));

        ammoText = HudUiFactory.CreateText(root, "Municion", AmmoFontSize, TextAnchor.LowerRight);
        HudUiFactory.AnchorBottomRight(ammoText.rectTransform, AmmoOffset, LabelSize);
        weaponText = HudUiFactory.CreateText(root, "Arma", WeaponFontSize, TextAnchor.LowerRight);
        HudUiFactory.AnchorBottomRight(weaponText.rectTransform, WeaponOffset, LabelSize);

        variantText = HudUiFactory.CreateText(root, "Variante", VariantFontSize, TextAnchor.MiddleCenter);
        variantText.color = VariantColor;
        HudUiFactory.AnchorBottomCenter(variantText.rectTransform, VariantOffsetFromBottom, WideLabelSize);
        statusText = HudUiFactory.CreateText(root, "Estado", StatusFontSize, TextAnchor.MiddleCenter);
        HudUiFactory.AnchorCenter(statusText.rectTransform, StatusVerticalOffset, WideLabelSize);
        crosshairText = HudUiFactory.CreateText(root, "Crosshair", CrosshairFontSize, TextAnchor.MiddleCenter);
        crosshairText.text = CrosshairGlyph;
        HudUiFactory.AnchorCenter(crosshairText.rectTransform, 0f, CrosshairSize);
    }

    private void Update()
    {
        if (player == null)
            return;

        GameManager manager = GameManager.Instance;
        int slot = FindSlot(manager);
        RefreshIdentity(slot);
        RefreshHealth();
        RefreshEnergy();
        RefreshWeapon();
        RefreshStatus(manager);
        RefreshCrosshair();
    }

    private int FindSlot(GameManager manager)
    {
        if (manager == null)
            return 0;

        for (int i = 0; i < manager.Players.Count; i++)
        {
            if (manager.Players[i] == player)
                return i;
        }

        return 0;
    }

    private void RefreshIdentity(int slot)
    {
        string role = player.HasRole ? DefenderRoleCatalog.GetDisplayName(player.Role) : string.Empty;
        identityText.text = string.IsNullOrEmpty(role) ? $"P{slot + 1}" : $"P{slot + 1} · {role}";
        identityText.color = GetPlayerColor(slot);
    }

    private void RefreshHealth()
    {
        healthText.text = player.estaDerribado
            ? "Vida: DERRIBADO"
            : $"Vida: {player.vidaActual:F0}/{player.vidaMaxima:F0}";
        healthText.color = player.estaDerribado ? Color.red : Color.white;
        UiFill.Set(healthBar, player.estaDerribado ? 0f : player.vidaActual / player.vidaMaxima);
        healthBar.color = player.estaDerribado ? Color.red : Color.green;
    }

    private void RefreshEnergy()
    {
        if (energy == null)
            return;

        energyText.text = $"Energía: {energy.energiaActual:F0}/{energy.energiaMaxima:F0}";
        UiFill.Set(energyBar, energy.energiaActual / energy.energiaMaxima);
    }

    private void RefreshWeapon()
    {
        WeaponSystem.Arma weapon = weapons != null ? weapons.ObtenerArmaActual() : null;
        if (weapon == null)
            return;

        ammoText.text = weapon.municionMaxima < 0 ? "∞" : $"{weapon.municionActual}/{weapon.municionMaxima}";
        weaponText.text = weapon.nombre;

        string displayName = Hud.GetVariantDisplayName(weapons.VarianteActiva, weapons.ActiveVariantDisplayName);
        variantText.text = Hud.FormatVariantLabel(
            displayName,
            weapons.VariantMultipliesDamage,
            weapons.multiplicadorVariante,
            weapons.tiempoVarianteRestante);
    }

    private void RefreshStatus(GameManager manager)
    {
        if (player.estaDerribado)
        {
            statusText.text = "DERRIBADO — esperá que un aliado te reanime";
            statusText.color = Color.red;
            return;
        }

        PlayerController ally = manager == null
            ? null
            : PlayerLocator.FindClosest(manager.Players, player.transform.position, IsDownedAlly, player.rangoReanimacion + RevivePromptExtraRangeMeters);
        if (ally == null)
        {
            statusText.text = string.Empty;
            return;
        }

        float distance = Vector3.Distance(player.transform.position, ally.transform.position);
        statusText.text = $"[E / Triángulo] reanimar a {ally.name} ({distance:F1}m)";
        statusText.color = Color.yellow;
    }

    private bool IsDownedAlly(PlayerController candidate)
    {
        return candidate != player && candidate.estaDerribado;
    }

    private void RefreshCrosshair()
    {
        if (crosshairTimer <= 0f)
            return;

        crosshairTimer -= Time.unscaledDeltaTime;
        if (crosshairTimer <= 0f)
            crosshairText.color = Color.white;
    }
}
