/// <summary>
/// Lifecycle and snapshot contract for one player's input source.
/// </summary>
public interface IInputAdapter
{
    bool IsEnabled { get; }
    PlayerCommand CurrentCommand { get; }

    void Enable();
    void Disable();
}
