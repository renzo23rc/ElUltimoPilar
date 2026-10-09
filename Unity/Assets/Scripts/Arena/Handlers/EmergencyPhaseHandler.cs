using System.Collections;

namespace UltimoPilar.Arena
{
    /// <summary>Handles the emergency phase transition.</summary>
    public sealed class EmergencyPhaseHandler : ArenaPhaseHandler
    {
        private const int EmergencyPhaseNumber = 4;

        /// <summary>Initializes the emergency phase handler.</summary>
        /// <param name="warningPresenter">The warning presenter for this transition.</param>
        /// <param name="phaseEffects">The effect service for this transition.</param>
        public EmergencyPhaseHandler(ArenaWarningPresenter warningPresenter, ArenaPhaseEffects phaseEffects)
            : base(warningPresenter, phaseEffects)
        {
        }

        /// <inheritdoc />
        public override int Phase => EmergencyPhaseNumber;

        /// <inheritdoc />
        protected override IEnumerator PresentWarning(ArenaWarningPresenter presenter, float durationSeconds)
        {
            return presenter.PresentEmergencyWarning(durationSeconds);
        }

        /// <inheritdoc />
        protected override IEnumerator ActivateEffects(ArenaPhaseEffects effects)
        {
            return effects.ActivateEmergency();
        }
    }
}
