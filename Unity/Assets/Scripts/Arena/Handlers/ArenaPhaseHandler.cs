using System.Collections;

namespace UltimoPilar.Arena
{
    /// <summary>
    /// Base strategy for one arena phase: it guards missing collaborators and lets each phase
    /// choose only which warning to present and which effects to activate.
    /// </summary>
    public abstract class ArenaPhaseHandler : IArenaPhaseHandler
    {
        private readonly ArenaWarningPresenter warningPresenter;
        private readonly ArenaPhaseEffects phaseEffects;

        /// <summary>Initializes the handler with its presentation and effect collaborators.</summary>
        /// <param name="warningPresenter">The warning presenter for this transition.</param>
        /// <param name="phaseEffects">The effect service for this transition.</param>
        protected ArenaPhaseHandler(ArenaWarningPresenter warningPresenter, ArenaPhaseEffects phaseEffects)
        {
            this.warningPresenter = warningPresenter;
            this.phaseEffects = phaseEffects;
        }

        /// <inheritdoc />
        public abstract int Phase { get; }

        /// <inheritdoc />
        public IEnumerator Warn(float durationSeconds)
        {
            if (warningPresenter == null)
            {
                yield break;
            }

            yield return PresentWarning(warningPresenter, durationSeconds);
        }

        /// <inheritdoc />
        public IEnumerator Activate()
        {
            if (phaseEffects == null)
            {
                yield break;
            }

            yield return ActivateEffects(phaseEffects);
        }

        /// <summary>Presents the warning of this phase.</summary>
        protected abstract IEnumerator PresentWarning(ArenaWarningPresenter presenter, float durationSeconds);

        /// <summary>Activates the effects of this phase.</summary>
        protected abstract IEnumerator ActivateEffects(ArenaPhaseEffects effects);
    }
}
