using System.Collections;

namespace UltimoPilar.Arena;

/// <summary>Handles the central pit phase transition.</summary>
public sealed class PitPhaseHandler : IArenaPhaseHandler
{
    private const int PitPhaseNumber = 2;

    private readonly ArenaWarningPresenter warningPresenter;
    private readonly ArenaPhaseEffects phaseEffects;

    /// <summary>Initializes a central pit phase handler.</summary>
    /// <param name="warningPresenter">The warning presenter for this transition.</param>
    /// <param name="phaseEffects">The effect service for this transition.</param>
    public PitPhaseHandler(ArenaWarningPresenter warningPresenter, ArenaPhaseEffects phaseEffects)
    {
        this.warningPresenter = warningPresenter;
        this.phaseEffects = phaseEffects;
    }

    /// <inheritdoc />
    public int Phase => PitPhaseNumber;

    /// <inheritdoc />
    public IEnumerator Warn(float durationSeconds)
    {
        if (warningPresenter == null)
        {
            yield break;
        }

        yield return warningPresenter.PresentPitWarning(durationSeconds);
    }

    /// <inheritdoc />
    public IEnumerator Activate()
    {
        if (phaseEffects == null)
        {
            yield break;
        }

        yield return phaseEffects.ActivatePit();
    }
}
