using System.Collections;

namespace UltimoPilar.Arena
{
    /// <summary>Handles the central pit phase transition.</summary>
    public sealed class PitPhaseHandler : ArenaPhaseHandler
    {
        private const int PitPhaseNumber = 2;

        /// <summary>Initializes the central pit phase handler.</summary>
        /// <param name="warningPresenter">The warning presenter for this transition.</param>
        /// <param name="phaseEffects">The effect service for this transition.</param>
        public PitPhaseHandler(ArenaWarningPresenter warningPresenter, ArenaPhaseEffects phaseEffects)
            : base(warningPresenter, phaseEffects)
        {
        }

        /// <inheritdoc />
        public override int Phase => PitPhaseNumber;

        /// <inheritdoc />
        protected override IEnumerator PresentWarning(ArenaWarningPresenter presenter, float durationSeconds)
        {
            return presenter.PresentPitWarning(durationSeconds);
        }

        /// <inheritdoc />
        protected override IEnumerator ActivateEffects(ArenaPhaseEffects effects)
        {
            return effects.ActivatePit();
        }
    }
}
