using System.Collections;
using System.Collections.Generic;

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

internal static class ArenaPhaseHandlerCatalog
{
    /// <summary>Creates the manually wired phase strategy map.</summary>
    /// <param name="warningPresenter">The warning presenter used by each strategy.</param>
    /// <param name="phaseEffects">The effect service used by each strategy.</param>
    /// <returns>The phase strategies keyed by their phase number.</returns>
    public static IReadOnlyDictionary<int, IArenaPhaseHandler> CreateDefault(
        ArenaWarningPresenter warningPresenter,
        ArenaPhaseEffects phaseEffects)
    {
        IArenaPhaseHandler pitHandler = new PitPhaseHandler(warningPresenter, phaseEffects);
        IArenaPhaseHandler gravityHandler = new GravityPhaseHandler(warningPresenter, phaseEffects);
        IArenaPhaseHandler emergencyHandler = new EmergencyPhaseHandler(warningPresenter, phaseEffects);

        var handlers = new Dictionary<int, IArenaPhaseHandler>
        {
            [pitHandler.Phase] = pitHandler,
            [gravityHandler.Phase] = gravityHandler,
            [emergencyHandler.Phase] = emergencyHandler
        };

        return handlers;
    }
}
}
