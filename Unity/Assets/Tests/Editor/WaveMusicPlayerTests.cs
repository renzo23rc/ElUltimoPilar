using NUnit.Framework;

public class WaveMusicPlayerTests
{
    [Test]
    public void ResourcePathForSceneMapsEachPlayableMap()
    {
        Assert.That(WaveMusicPlayer.ResourcePathForScene("EstatuaDeLaLibertad"), Is.EqualTo("Audio/Musica/wave_eeuu"));
        Assert.That(WaveMusicPlayer.ResourcePathForScene("Obelisco"), Is.EqualTo("Audio/Musica/wave_bsas"));
        Assert.That(WaveMusicPlayer.ResourcePathForScene("CristoRedentor"), Is.EqualTo("Audio/Musica/wave_brasil"));
    }

    [Test]
    public void ResourcePathForSceneIgnoresUnknownScenes()
    {
        Assert.That(WaveMusicPlayer.ResourcePathForScene("MainMenu"), Is.Null);
        Assert.That(WaveMusicPlayer.ResourcePathForScene(null), Is.Null);
    }
}
