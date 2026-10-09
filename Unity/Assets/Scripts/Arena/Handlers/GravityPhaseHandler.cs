using System.Collections;

namespace UltimoPilar.Arena
{
    /// <summary>Handles the altered gravity phase transition.</summary>
    public sealed class GravityPhaseHandler : ArenaPhaseHandler
    {
        private const int GravityPhaseNumber = 3;

        /// <summary>Initializes the altered gravity phase handler.</summary>
        /// <param name="warningPresenter">The warning presenter for this transition.</param>
        /// <param name="phaseEffects">The effect service for this transition.</param>
        public GravityPhaseHandler(ArenaWarningPresenter warningPresenter, ArenaPhaseEffects phaseEffects)
            : base(warningPresenter, phaseEffects)
        {
        }

        /// <inheritdoc />
        public override int Phase => GravityPhaseNumber;

        /// <inheritdoc />
        protected override IEnumerator PresentWarning(ArenaWarningPresenter presenter, float durationSeconds)
        {
            return presenter.PresentGravityWarning(durationSeconds);
        }

        /// <inheritdoc />
        protected override IEnumerator ActivateEffects(ArenaPhaseEffects effects)
        {
            return effects.ActivateGravity();
        }
    }
}
