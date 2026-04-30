/// <summary>
/// Interface for commands that can be executed and undone.
/// Enables undo/redo functionality and makes state changes explicit and testable.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Executes the command, modifying game state.
    /// </summary>
    void Execute();

    /// <summary>
    /// Reverses the command, restoring previous game state.
    /// </summary>
    void Undo();
}
