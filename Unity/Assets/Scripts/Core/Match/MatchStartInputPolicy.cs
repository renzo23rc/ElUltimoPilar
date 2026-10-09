using System;

/// <summary>
/// Unity-free rule that decides whether a player command should start a match waiting for input.
/// </summary>
public static class MatchStartInputPolicy
{
    /// <summary>Returns whether the command contains deliberate player input.</summary>
    /// <param name="command">The command snapshot of the primary player.</param>
    /// <param name="lookThreshold">The look magnitude that must be exceeded to count as input.</param>
    /// <returns><see langword="true"/> for movement, a strong look, a jump, or a shot.</returns>
    public static bool IsStartInput(PlayerCommand command, float lookThreshold)
    {
        if (command.MoveX != 0f || command.MoveY != 0f)
            return true;

        double lookMagnitude = Math.Sqrt((command.LookX * command.LookX) + (command.LookY * command.LookY));
        if (lookMagnitude > lookThreshold)
            return true;

        return command.Jump || command.Fire;
    }
}
