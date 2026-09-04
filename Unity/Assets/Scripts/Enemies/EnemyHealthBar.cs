using UnityEngine;
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

        return Clamp(currentHealth / maximumHealth);
    }

    private static bool IsValidMaximum(float maximumHealth)
    {
        return !float.IsNaN(maximumHealth)
            && !float.IsInfinity(maximumHealth)
            && maximumHealth > EmptyFraction;
    }

    private static float Clamp(float fraction)
    {
        if (fraction <= EmptyFraction)
        {
            return EmptyFraction;
        }

        if (fraction >= FullFraction)
        {
            return FullFraction;
        }

        return fraction;
    }
}

public class EnemyHealthBar : MonoBehaviour
{
    private const float BarWidth = 2.2f;
    private const float BarHeight = 0.28f;
    private const float BarHeightOffset = 0.4f;
    private const float CanvasScale = 0.016f;
    private const float BackgroundAlpha = 0.9f;

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
        if (enemy == null)
        {
            return;
        }

        enemy.OnDañoRecibido -= HandleDamageReceived;
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
        if (healthFill != null)
        {
            Canvas existingCanvas = healthFill.GetComponentInParent<Canvas>();
            if (existingCanvas != null) canvasTransform = existingCanvas.transform;
            if (canvasTransform != null && existingCanvas != null)
            {
                // Fix old invisible bars (small scale, wrong alpha, wrong height, billboard invertido, layer)
                canvasTransform.localScale = Vector3.one * CanvasScale;
                RectTransform cr = existingCanvas.GetComponent<RectTransform>();
                if (cr != null) cr.sizeDelta = new Vector2(BarWidth * 100f, BarHeight * 100f);
                existingCanvas.sortingOrder = 100;
                existingCanvas.gameObject.layer = 5;
                if (existingCanvas.worldCamera == null) existingCanvas.worldCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
                float existingHeightOffset = BarHeightOffset;
                if (enemy != null)
                {
                    Collider col = enemy.GetComponent<Collider>();
                    if (col != null && col.bounds.extents.y > 0.01f) existingHeightOffset = col.bounds.extents.y + 0.6f;
                    else existingHeightOffset = enemy.transform.localScale.y * 0.5f + 0.7f;
                    if (existingHeightOffset < 1.1f) existingHeightOffset = enemy.transform.localScale.y * 0.5f + 0.9f;
                }
                else existingHeightOffset = 1.6f;
                canvasTransform.localPosition = new Vector3(0f, existingHeightOffset, 0f);
                Image[] imgs = existingCanvas.GetComponentsInChildren<Image>(true);
                if (imgs.Length > 0) imgs[0].color = new Color(0.08f, 0.08f, 0.08f, BackgroundAlpha);
                if (imgs.Length > 1)
                {
                    imgs[1].color = new Color(0.15f, 1f, 0.15f, 1f);
                    healthFill = imgs[1];
                }
                foreach (var img in imgs) img.gameObject.layer = 5;
            }
            if (healthFill != null && canvasTransform != null) return;
        }

        // Create WorldSpace canvas above enemy head.
        GameObject canvasGO = new GameObject("HealthBarCanvas");
        canvasTransform = canvasGO.transform;
        canvasTransform.SetParent(transform, false);

        // Position above head based on collider or scale.
        float heightOffset = BarHeightOffset;
        if (enemy != null)
        {
            Collider col = enemy.GetComponent<Collider>();
            if (col != null && col.bounds.extents.y > 0.01f) heightOffset = col.bounds.extents.y + 0.6f;
            else heightOffset = enemy.transform.localScale.y * 0.5f + 0.7f;
            if (heightOffset < 1.1f) heightOffset = enemy.transform.localScale.y * 0.5f + 0.9f;
        }
        else
        {
            heightOffset = 1.6f;
        }

        canvasTransform.localPosition = new Vector3(0f, heightOffset, 0f);
        canvasTransform.localRotation = Quaternion.identity;
        canvasTransform.localScale = Vector3.one * CanvasScale;

        canvasGO.layer = 5;
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        canvas.sortingOrder = 100;
        // CanvasScaler/GraphicRaycaster not needed for WorldSpace but keep for safety.
        if (canvasGO.GetComponent<CanvasScaler>() == null) canvasGO.AddComponent<CanvasScaler>();
        if (canvasGO.GetComponent<GraphicRaycaster>() == null) canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(BarWidth * 100f, BarHeight * 100f);

        // Background - more opaque and with outline for visibility
        GameObject bgGO = new GameObject("Background");
        bgGO.layer = 5;
        bgGO.transform.SetParent(canvasTransform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.08f, BackgroundAlpha);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = Vector2.zero;

        // Fill - bright green, fully opaque
        GameObject fillGO = new GameObject("Fill");
        fillGO.layer = 5;
        fillGO.transform.SetParent(bgGO.transform, false);
        healthFill = fillGO.AddComponent<Image>();
        healthFill.color = new Color(0.15f, 1f, 0.15f, 1f);
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillOrigin = 0;
        healthFill.fillAmount = 1f;
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;

        // Ensure initial fill.
        if (enemy != null) healthFill.fillAmount = CalculateHealthFraction(enemy.vidaActual, enemy.vidaMaxima);
    }

    private void LateUpdate()
    {
        if (canvasTransform == null) return;
        Camera cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (cam == null) return;
        Canvas canvas = canvasTransform.GetComponent<Canvas>();
        if (canvas != null && canvas.worldCamera == null) canvas.worldCamera = cam;
        // Billboard: face camera (canvas forward must point to camera).
        Vector3 dir = cam.transform.position - canvasTransform.position;
        if (dir.sqrMagnitude > 0.001f) canvasTransform.rotation = Quaternion.LookRotation(dir, cam.transform.up);
    }

    public void AssignReferences(Enemy targetEnemy, Image fill, Transform canvas)
    {
        enemy = targetEnemy;
        healthFill = fill;
        canvasTransform = canvas;
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

        healthFill.fillAmount = CalculateHealthFraction(enemy.vidaActual, enemy.vidaMaxima);
    }
}
