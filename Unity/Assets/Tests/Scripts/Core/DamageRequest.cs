public readonly struct DamageRequest
{
    public DamageRequest(float amount)
    {
        Amount = amount;
    }

    public float Amount { get; }
}
