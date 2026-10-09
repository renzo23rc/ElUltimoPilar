using System.Collections;

namespace UltimoPilar.Arena
{

/// <summary>Defines the warning and activation operations for one arena phase.</summary>
public interface IArenaPhaseHandler
{
    /// <summary>Gets the phase number handled by this strategy.</summary>
    int Phase { get; }

    /// <summary>Presents the phase warning for the requested duration.</summary>
    /// <param name="durationSeconds">The warning duration in seconds.</param>
    /// <returns>The coroutine that presents the warning.</returns>
    IEnumerator Warn(float durationSeconds);

    /// <summary>Activates the phase effects.</summary>
    /// <returns>The coroutine that activates the phase.</returns>
    IEnumerator Activate();
}
}
