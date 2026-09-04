using System;
using NUnit.Framework;

public class AudioAdapterTests
{
    [Test]
    public void ProceduralSfxIncludesDamageFeedback()
    {
        Assert.That(Enum.IsDefined(typeof(AudioAdapter.Sfx), nameof(AudioAdapter.Sfx.Damage)), Is.True);
    }

    [Test]
    public void TransformationStingerAndLayeredMusicRemainDeferred()
    {
        Assert.That(Enum.IsDefined(typeof(AudioAdapter.Sfx), "Transformation"), Is.False);
        Assert.That(Enum.IsDefined(typeof(AudioAdapter.Sfx), "LayeredMusic"), Is.False);
    }
}
