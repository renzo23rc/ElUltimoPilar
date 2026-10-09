/// <summary>
/// Lifecycle and snapshot contract for one player's input source.
/// </summary>
public interface IInputAdapter
{
    /// <summary>Gets whether input reading is enabled.</summary>
    bool IsEnabled { get; }
    /// <summary>Gets the current immutable command snapshot.</summary>
    PlayerCommand CurrentCommand { get; }

    /// <summary>Enables input reading.</summary>
    void Enable();
    /// <summary>Disables input reading.</summary>
    void Disable();
}
