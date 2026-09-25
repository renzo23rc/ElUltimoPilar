using UltimoPilar.Core.Shared;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

internal static class EnemyHealthFraction
{
    private const float EmptyFraction = 0f;
    private const float FullFraction = 1f;

    public static float Calculate(float currentHealth, float maximumHealth)
    {
        if (!IsValidMaximum(maximumHealth) || float.IsNaN(currentHealth) || currentHealth <= EmptyFraction)
        {
            return EmptyFraction;
        }

        if (currentHealth >= maximumHealth)
        {
            return FullFraction;
        }

        return Mathf.Clamp01(currentHealth / maximumHealth);
    }

    private static bool IsValidMaximum(float maximumHealth)
    {
        return !float.IsNaN(maximumHealth)
            && !float.IsInfinity(maximumHealth)
            && maximumHealth > EmptyFraction;
    }
}

/// <summary>
/// World-space health bar above an enemy. It faces every camera right before that camera renders,
/// so it reads correctly in split-screen without searching for cameras each frame.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    private const float BarWidth = 2.2f;
    private const float BarHeight = 0.28f;
    private const float CanvasUnitsPerMeter = 100f;
    private const float CanvasScale = 0.016f;
    private const int CanvasSortingOrder = 100;
    private const int UiLayer = 5;
    private const float MinimumColliderExtentMeters = 0.01f;
    private const float ColliderHeadroomMeters = 0.6f;
    private const float ScaleHeadroomMeters = 0.7f;
    private const float MinimumHeightOffsetMeters = 1.1f;
    private const float MinimumHeightHeadroomMeters = 0.9f;
    private const float DefaultHeightOffsetMeters = 1.6f;
    private const float MinimumBillboardDistanceSqr = 0.001f;
    private static readonly Color BackgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);
    private static readonly Color FillColor = new Color(0.15f, 1f, 0.15f, 1f);
    private static readonly Vector2 CenterPivot = new Vector2(0.5f, 0.5f);

    [Header("References")]
    [SerializeField] private Enemy enemy;
    [SerializeField] private Image healthFill;

    [Header("Runtime")]
    [SerializeField] private Transform canvasTransform;

    public static float CalculateHealthFraction(float currentHealth, float maximumHealth)
    {
        return EnemyHealthFraction.Calculate(currentHealth, maximumHealth);
    }

    private void Awake()
    {
        ResolveEnemy();
        EnsureHealthBarUI();
    }

    private void OnEnable()
    {
        ResolveEnemy();
        RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;

        if (enemy == null)
        {
            return;
        }

        enemy.OnDañoRecibido += HandleDamageReceived;
        RefreshHealthBar();
    }

    private void Start()
    {
        RefreshHealthBar();
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;

        if (enemy == null)
        {
            return;
        }

        enemy.OnDañoRecibido -= HandleDamageReceived;
    }

    public void AssignReferences(Enemy targetEnemy, Image fill, Transform canvas)
    {
        enemy = targetEnemy;
        healthFill = fill;
        canvasTransform = canvas;
    }

    private void ResolveEnemy()
    {
        if (enemy == null)
        {
            enemy = GetComponentInParent<Enemy>();
        }
    }

    private void EnsureHealthBarUI()
    {
        Canvas existingCanvas = healthFill != null ? healthFill.GetComponentInParent<Canvas>() : null;
        if (existingCanvas != null)
        {
            RepairExistingBar(existingCanvas);
            return;
        }

        CreateBar();
    }

    // Normaliza barras serializadas antiguas (escala, alpha, altura y layer incorrectos).
    private void RepairExistingBar(Canvas existingCanvas)
    {
        canvasTransform = existingCanvas.transform;
        ConfigureCanvas(existingCanvas);

        Image[] images = existingCanvas.GetComponentsInChildren<Image>(true);
        if (images.Length > 0)
        {
            images[0].color = BackgroundColor;
        }

        if (images.Length > 1)
        {
            images[1].color = FillColor;
            healthFill = images[1];
        }

        foreach (Image image in images)
        {
            image.gameObject.layer = UiLayer;
        }
    }

    private void CreateBar()
    {
        var canvasObject = new GameObject("HealthBarCanvas");
        canvasTransform = canvasObject.transform;
        canvasTransform.SetParent(transform, false);
        canvasTransform.localRotation = Quaternion.identity;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        ConfigureCanvas(canvas);

        Image background = CreateStretchedImage("Background", canvasTransform, BackgroundColor);
        healthFill = CreateStretchedImage("Fill", background.transform, FillColor);
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillOrigin = 0;
        RefreshHealthBar();
    }

    private void ConfigureCanvas(Canvas canvas)
    {
        canvasTransform.localPosition = new Vector3(0f, CalculateHeightOffset(), 0f);
        canvasTransform.localScale = Vector3.one * CanvasScale;
        canvas.sortingOrder = CanvasSortingOrder;
        canvas.gameObject.layer = UiLayer;
        if (canvas.TryGetComponent(out RectTransform canvasRect))
        {
            canvasRect.sizeDelta = new Vector2(BarWidth * CanvasUnitsPerMeter, BarHeight * CanvasUnitsPerMeter);
        }
    }

    private float CalculateHeightOffset()
    {
        if (enemy == null)
        {
            return DefaultHeightOffsetMeters;
        }

        float halfScaleHeight = enemy.transform.localScale.y * 0.5f;
        Collider enemyCollider = enemy.GetComponent<Collider>();
        float offset = enemyCollider != null && enemyCollider.bounds.extents.y > MinimumColliderExtentMeters
            ? enemyCollider.bounds.extents.y + ColliderHeadroomMeters
            : halfScaleHeight + ScaleHeadroomMeters;
        return offset < MinimumHeightOffsetMeters ? halfScaleHeight + MinimumHeightHeadroomMeters : offset;
    }

    private static Image CreateStretchedImage(string objectName, Transform parent, Color color)
    {
        var imageObject = new GameObject(objectName);
        imageObject.layer = UiLayer;
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = CenterPivot;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return image;
    }

    private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
    {
        if (canvasTransform == null || renderingCamera == null)
        {
            return;
        }

        // Billboard: el frente del canvas apunta a la cámara que está por renderizar.
        Vector3 toCamera = renderingCamera.transform.position - canvasTransform.position;
        if (toCamera.sqrMagnitude > MinimumBillboardDistanceSqr)
        {
            canvasTransform.rotation = Quaternion.LookRotation(toCamera, renderingCamera.transform.up);
        }
    }

    private void HandleDamageReceived(float damageAmount)
    {
        RefreshHealthBar();
    }

    private void RefreshHealthBar()
    {
        if (enemy == null || healthFill == null)
        {
            return;
        }

        UiFill.Set(healthFill, CalculateHealthFraction(enemy.vidaActual, enemy.vidaMaxima));
    }
}
