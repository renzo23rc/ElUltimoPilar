/// <summary>
/// Defines an object that can receive a damage request.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Applies the supplied damage request to this object.
    /// </summary>
    /// <param name="request">The damage request to apply.</param>
    void ReceiveDamage(DamageRequest request);
}
