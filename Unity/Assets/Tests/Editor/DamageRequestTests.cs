using System;
using NUnit.Framework;

public class DamageRequestTests
{
    [Test]
    public void ConstructorPreservesEveryRawAmountExactly()
    {
        var amounts = new[]
        {
            0f,
            -0f,
            -1.25f,
            float.MinValue,
            float.MaxValue,
            float.PositiveInfinity,
            float.NegativeInfinity,
            float.NaN
        };

        foreach (var amount in amounts)
        {
            var request = new DamageRequest(amount);

            Assert.That(FloatBits(request.Amount), Is.EqualTo(FloatBits(amount)));
        }
    }

    [Test]
    public void AmountIsTheOnlyGetterOnlyPropertyOfTheValueType()
    {
        var requestType = typeof(DamageRequest);
        var amountProperty = requestType.GetProperty(nameof(DamageRequest.Amount));

        Assert.That(requestType.IsValueType, Is.True);
        Assert.That(requestType.GetProperties(), Has.Length.EqualTo(1));
        Assert.That(amountProperty, Is.Not.Null);
        Assert.That(amountProperty.PropertyType, Is.EqualTo(typeof(float)));
        Assert.That(amountProperty.CanWrite, Is.False);
    }

    [Test]
    public void FakeDamageableReceivesOneUnchangedRequest()
    {
        const float amount = -0f;
        var request = new DamageRequest(amount);
        var receiver = new FakeDamageable();

        receiver.ReceiveDamage(request);

        Assert.That(receiver.ReceiveCount, Is.EqualTo(1));
        Assert.That(FloatBits(receiver.LastRequest.Amount), Is.EqualTo(FloatBits(amount)));
    }

    private static int FloatBits(float value)
    {
        return BitConverter.SingleToInt32Bits(value);
    }

    private sealed class FakeDamageable : IDamageable
    {
        public int ReceiveCount { get; private set; }
        public DamageRequest LastRequest { get; private set; }

        public void ReceiveDamage(DamageRequest request)
        {
            ReceiveCount++;
            LastRequest = request;
        }
    }
}
