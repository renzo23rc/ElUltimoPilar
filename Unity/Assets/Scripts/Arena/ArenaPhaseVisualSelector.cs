namespace UltimoPilar.Arena
{
    /// <summary>
    /// Picks which deteriorated monument variant to show for a Pilar phase.
    /// Phase 1 keeps the original map pieces; later phases use the matching variant.
    /// </summary>
    public static class ArenaPhaseVisualSelector
    {
        private const int FirstDeterioratedPhase = 2;

        /// <summary>
        /// Returns the index into the fase 2+ roots, or -1 to keep the original map visuals.
        /// </summary>
        public static int DeterioratedVariantIndex(int phase, int variantCount)
        {
            if (variantCount <= 0 || phase < FirstDeterioratedPhase)
            {
                return -1;
            }

            int index = phase - FirstDeterioratedPhase;
            if (index >= variantCount)
            {
                return variantCount - 1;
            }

            return index;
        }
    }
}
