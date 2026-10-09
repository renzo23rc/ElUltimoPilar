using System;

/// <summary>
/// Unity-free generation of mono sample buffers for the procedural sound effects.
/// Every buffer is enveloped so it starts and ends in silence and stays within [-amplitude, amplitude].
/// </summary>
public static class ProceduralSfxSynthesizer
{
    private const double FullCycleRadians = 2d * Math.PI;
    private const double NoiseRange = 2d;
    private const double NoiseOffset = 1d;
    private const int MinimumSamples = 1;

    /// <summary>Gets the number of samples needed for a duration.</summary>
    public static int SampleCount(float durationSeconds, int sampleRateHz)
    {
        return Math.Max(MinimumSamples, (int)Math.Round(sampleRateHz * (double)durationSeconds));
    }

    /// <summary>Creates a frequency sweep with a sine envelope.</summary>
    /// <param name="startFrequencyHz">The initial frequency.</param>
    /// <param name="endFrequencyHz">The final frequency.</param>
    /// <param name="durationSeconds">The duration.</param>
    /// <param name="amplitude">The peak amplitude.</param>
    /// <param name="square">Whether to use a square wave instead of a sine wave.</param>
    /// <param name="sampleRateHz">The sample rate.</param>
    /// <returns>The sample buffer.</returns>
    public static float[] Tone(
        float startFrequencyHz,
        float endFrequencyHz,
        float durationSeconds,
        float amplitude,
        bool square,
        int sampleRateHz)
    {
        int samples = SampleCount(durationSeconds, sampleRateHz);
        var data = new float[samples];
        double phase = 0d;
        for (int i = 0; i < samples; i++)
        {
            double progress = (double)i / samples;
            double frequency = startFrequencyHz + ((endFrequencyHz - startFrequencyHz) * progress);
            phase += FullCycleRadians * frequency / sampleRateHz;
            double wave = square ? SquareSign(Math.Sin(phase)) : Math.Sin(phase);
            data[i] = (float)(wave * Envelope(progress) * amplitude);
        }

        return data;
    }

    /// <summary>Creates deterministic white noise that fades out linearly.</summary>
    /// <param name="durationSeconds">The duration.</param>
    /// <param name="amplitude">The peak amplitude.</param>
    /// <param name="seed">The random seed, so the same effect always sounds the same.</param>
    /// <param name="sampleRateHz">The sample rate.</param>
    /// <returns>The sample buffer.</returns>
    public static float[] Noise(float durationSeconds, float amplitude, int seed, int sampleRateHz)
    {
        int samples = SampleCount(durationSeconds, sampleRateHz);
        var data = new float[samples];
        var random = new Random(seed);
        for (int i = 0; i < samples; i++)
        {
            double fade = 1d - ((double)i / samples);
            data[i] = (float)(((random.NextDouble() * NoiseRange) - NoiseOffset) * fade * amplitude);
        }

        return data;
    }

    /// <summary>Creates consecutive enveloped sine notes.</summary>
    /// <param name="frequenciesHz">The note frequencies in order.</param>
    /// <param name="noteDurationSeconds">The duration of each note.</param>
    /// <param name="amplitude">The peak amplitude.</param>
    /// <param name="sampleRateHz">The sample rate.</param>
    /// <returns>The sample buffer.</returns>
    public static float[] Arpeggio(float[] frequenciesHz, float noteDurationSeconds, float amplitude, int sampleRateHz)
    {
        if (frequenciesHz == null)
            throw new ArgumentNullException(nameof(frequenciesHz));

        int samplesPerNote = SampleCount(noteDurationSeconds, sampleRateHz);
        var data = new float[samplesPerNote * frequenciesHz.Length];
        for (int note = 0; note < frequenciesHz.Length; note++)
        {
            double phase = 0d;
            for (int i = 0; i < samplesPerNote; i++)
            {
                double progress = (double)i / samplesPerNote;
                phase += FullCycleRadians * frequenciesHz[note] / sampleRateHz;
                data[(note * samplesPerNote) + i] = (float)(Math.Sin(phase) * Envelope(progress) * amplitude);
            }
        }

        return data;
    }

    private static double Envelope(double progress)
    {
        return Math.Sin(Math.PI * progress);
    }

    // Igual que Mathf.Sign: el cero cuenta como positivo.
    private static double SquareSign(double value)
    {
        return value >= 0d ? 1d : -1d;
    }
}
