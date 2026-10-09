using System;
using NUnit.Framework;

public class ProceduralSfxSynthesizerTests
{
    private const int SampleRateHz = 44100;
    private const float DurationSeconds = 0.1f;
    private const float Amplitude = 0.6f;
    private const int Seed = 1234;
    private const float Tolerance = 0.0001f;

    [Test]
    public void SampleCountFollowsDurationAndNeverDropsBelowOne()
    {
        Assert.That(ProceduralSfxSynthesizer.SampleCount(DurationSeconds, SampleRateHz), Is.EqualTo(4410));
        Assert.That(ProceduralSfxSynthesizer.SampleCount(0f, SampleRateHz), Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ToneStaysWithinAmplitudeAndStartsSilent(bool square)
    {
        float[] samples = ProceduralSfxSynthesizer.Tone(440f, 880f, DurationSeconds, Amplitude, square, SampleRateHz);

        Assert.That(samples.Length, Is.EqualTo(4410));
        Assert.That(samples[0], Is.EqualTo(0f).Within(Tolerance));
        foreach (float sample in samples)
            Assert.That(Math.Abs(sample), Is.LessThanOrEqualTo(Amplitude + Tolerance));
    }

    [Test]
    public void NoiseIsDeterministicForTheSameSeed()
    {
        float[] first = ProceduralSfxSynthesizer.Noise(DurationSeconds, Amplitude, Seed, SampleRateHz);
        float[] second = ProceduralSfxSynthesizer.Noise(DurationSeconds, Amplitude, Seed, SampleRateHz);

        Assert.That(first, Is.EqualTo(second));
        foreach (float sample in first)
            Assert.That(Math.Abs(sample), Is.LessThanOrEqualTo(Amplitude + Tolerance));
    }

    [Test]
    public void ArpeggioPlaysOneEnvelopedNotePerFrequency()
    {
        float[] frequencies = { 523f, 659f, 784f };

        float[] samples = ProceduralSfxSynthesizer.Arpeggio(frequencies, DurationSeconds, Amplitude, SampleRateHz);

        int perNote = ProceduralSfxSynthesizer.SampleCount(DurationSeconds, SampleRateHz);
        Assert.That(samples.Length, Is.EqualTo(perNote * frequencies.Length));
        Assert.That(samples[perNote], Is.EqualTo(0f).Within(Tolerance));
    }

    [Test]
    public void ArpeggioRejectsMissingFrequencies()
    {
        Assert.Throws<ArgumentNullException>(() => ProceduralSfxSynthesizer.Arpeggio(null, DurationSeconds, Amplitude, SampleRateHz));
    }
}
