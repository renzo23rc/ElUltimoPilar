using System.Collections;

namespace UltimoPilar.Arena;

/// <summary>Handles the emergency arena phase transition.</summary>
public sealed class EmergencyPhaseHandler : IArenaPhaseHandler
{
    private const int EmergencyPhaseNumber = 4;

    private readonly ArenaWarningPresenter warningPresenter;
    private readonly ArenaPhaseEffects phaseEffects;

    /// <summary>Initializes an emergency phase handler.</summary>
    /// <param name="warningPresenter">The warning presenter for this transition.</param>
    /// <param name="phaseEffects">The effect service for this transition.</param>
    public EmergencyPhaseHandler(ArenaWarningPresenter warningPresenter, ArenaPhaseEffects phaseEffects)
    {
        this.warningPresenter = warningPresenter;
        this.phaseEffects = phaseEffects;
    }

    /// <inheritdoc />
    public int Phase => EmergencyPhaseNumber;

    /// <inheritdoc />
    public IEnumerator Warn(float durationSeconds)
    {
        if (warningPresenter == null)
        {
            yield break;
        }

        yield return warningPresenter.PresentEmergencyWarning(durationSeconds);
    }

    /// <inheritdoc />
    public IEnumerator Activate()
    {
        if (phaseEffects == null)
        {
            yield break;
        }

        yield return phaseEffects.ActivateEmergency();
    }
}
