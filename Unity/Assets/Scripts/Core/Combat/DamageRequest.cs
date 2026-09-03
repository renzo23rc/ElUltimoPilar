/// <summary>
/// Carries the amount of damage to apply to a damageable target.
/// </summary>
public readonly struct DamageRequest
{
    /// <summary>
    /// Creates a damage request.
    /// </summary>
    /// <param name="amount">The damage amount.</param>
    public DamageRequest(float amount)
    {
        Amount = amount;
    }

    /// <summary>
    /// Gets the requested damage amount.
    /// </summary>
    public float Amount { get; }
}
