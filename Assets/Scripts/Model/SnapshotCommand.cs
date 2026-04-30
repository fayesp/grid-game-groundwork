using System.Collections.Generic;

/// <summary>
/// Captures the current state of the GameBoard as a snapshot.
/// Execute stores the snapshot; Undo restores it.
/// Useful for recording game state after each move for undo support.
/// </summary>
public class SnapshotCommand : ICommand
{
    private readonly GameBoard board;
    private Dictionary<string, EntityTransformSnapshot> snapshot;

    public SnapshotCommand(GameBoard board)
    {
        this.board = board;
    }

    public void Execute()
    {
        if (board != null)
            snapshot = board.CreateSnapshot();
    }

    public void Undo()
    {
        if (board != null && snapshot != null)
            board.RestoreSnapshot(snapshot);
    }
}
