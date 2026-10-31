using UnityEngine;

/// <summary>Plays type-indexed hurt and death clips from Resources at the enemy position.</summary>
public static class EnemyVoiceSfx
{
    private const string HurtResourcePrefix = "Audio/Enemigos/hurt";
    private const string DeathResourcePrefix = "Audio/Enemigos/death";
    private const float Volume = 0.7f;

    /// <summary>Plays the hurt or death clip that matches the enemy type.</summary>
    public static void Play(Enemy enemy, bool killed)
    {
        if (enemy == null)
        {
            return;
        }

        int voiceIndex = EnemyGrowl.VoiceIndex(enemy);
        string prefix = killed ? DeathResourcePrefix : HurtResourcePrefix;
        AudioClip clip = Resources.Load<AudioClip>($"{prefix}{voiceIndex}");
        if (clip == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(
            clip,
            enemy.transform.position,
            Volume * MenuPreferences.EffectsVolume);
    }
}
