using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Keyboard shortcuts of the shared match HUD: Enter restarts from pause or a result screen,
/// and R damages the Pilar in the Editor and development builds to test its phases.
/// Pause (Esc / Options) arrives through the Pause action and is resolved by <see cref="GameManager"/>.
/// </summary>
public sealed class MatchShortcuts
{
    private const float DebugPilarDamage = 10f;

    /// <summary>Processes the shortcuts pressed this frame.</summary>
    /// <param name="manager">The match manager.</param>
    /// <param name="pilar">The Pilar damaged by the debug shortcut.</param>
    public void Process(GameManager manager, Pilar pilar)
    {
        ProcessRestart(manager);
        ProcessDebugDamage(manager, pilar);
    }

    private static void ProcessRestart(GameManager manager)
    {
        if (manager == null || Keyboard.current == null)
            return;

        MatchState state = manager.EstadoActual;
        bool canRestart = state == MatchState.Victory || state == MatchState.Defeat || state == MatchState.Paused;
        if (canRestart && Keyboard.current.enterKey.wasPressedThisFrame)
            manager.ReiniciarJuego();
    }

    // Solo existe en el Editor y en Development builds.
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void ProcessDebugDamage(GameManager manager, Pilar pilar)
    {
        if (Keyboard.current == null || !Keyboard.current.rKey.wasPressedThisFrame)
            return;

        if (pilar == null)
        {
            Debug.LogWarning("[Hud] R: Pilar no encontrado (¿generación no completada?)");
            return;
        }

        // Fuerza el inicio para que arena y torretas reaccionen al daño de prueba.
        if (manager != null && !manager.juegoActivo)
            manager.IniciarJuego();
        pilar.RecibirDaño(DebugPilarDamage);
        Debug.Log($"[Hud] R: Pilar dañado -> {pilar.PorcentajeVida:F0}% fase {pilar.faseActual}");
    }
}
