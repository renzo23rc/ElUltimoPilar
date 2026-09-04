using System.Collections;

namespace UltimoPilar.Arena
{

/// <summary>Handles the altered gravity phase transition.</summary>
public sealed class GravityPhaseHandler : IArenaPhaseHandler
{
    private const int GravityPhaseNumber = 3;

    private readonly ArenaWarningPresenter warningPresenter;
    private readonly ArenaPhaseEffects phaseEffects;

    /// <summary>Initializes an altered gravity phase handler.</summary>
    /// <param name="warningPresenter">The warning presenter for this transition.</param>
    /// <param name="phaseEffects">The effect service for this transition.</param>
    public GravityPhaseHandler(ArenaWarningPresenter warningPresenter, ArenaPhaseEffects phaseEffects)
    {
        this.warningPresenter = warningPresenter;
        this.phaseEffects = phaseEffects;
    }

    /// <inheritdoc />
    public int Phase => GravityPhaseNumber;

    /// <inheritdoc />
    public IEnumerator Warn(float durationSeconds)
    {
        if (warningPresenter == null)
        {
            yield break;
        }

        yield return warningPresenter.PresentGravityWarning(durationSeconds);
    }

    /// <inheritdoc />
    public IEnumerator Activate()
    {
        if (phaseEffects == null)
        {
            yield break;
        }

        yield return phaseEffects.ActivateGravity();
    }
}
}
