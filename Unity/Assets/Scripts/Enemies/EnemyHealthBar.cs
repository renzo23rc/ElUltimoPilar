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
    [Header("References")]
    [SerializeField] private Enemy enemy;
    [SerializeField] private Image healthFill;

    public static float CalculateHealthFraction(float currentHealth, float maximumHealth)
    {
        return EnemyHealthFraction.Calculate(currentHealth, maximumHealth);
    }

    private void Awake()
    {
        ResolveEnemy();
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
