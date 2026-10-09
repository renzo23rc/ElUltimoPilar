using System.Collections.Generic;

namespace UltimoPilar.Arena
{

/// <summary>Wires the default phase strategies by hand, keyed by their phase number.</summary>
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
