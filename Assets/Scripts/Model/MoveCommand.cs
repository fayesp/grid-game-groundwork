using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Encapsulates a single game move (including all pushed movers and post-move effects like falling).
/// Execute applies the move to the GameBoard; Undo restores the previous state from a snapshot.
/// </summary>
public class MoveCommand : ICommand
{
    private readonly GameBoard board;
    private readonly Dictionary<string, Vector3> entityDeltas;
    private Dictionary<string, EntityTransformSnapshot> preMoveSnapshot;

    /// <summary>
    /// Creates a move command that will apply the given deltas to entities on the board.
    /// </summary>
    public MoveCommand(GameBoard board, Dictionary<string, Vector3> entityDeltas)
    {
        this.board = board;
        this.entityDeltas = entityDeltas ?? new Dictionary<string, Vector3>();
    }

    /// <summary>
    /// Creates a move command from a PlannedMoveResult.
    /// </summary>
    public MoveCommand(GameBoard board, PlannedMoveResult plannedMove)
        : this(board, plannedMove?.EntityDeltas)
    {
    }

    public void Execute()
    {
        if (board == null || entityDeltas.Count == 0)
            return;

        // Take snapshot before applying move
        preMoveSnapshot = board.CreateSnapshot();

        // Apply deltas to all affected entities
        foreach (var pair in entityDeltas)
        {
            board.MoveEntity(pair.Key, pair.Value);
        }
    }

    public void Undo()
    {
        if (board == null || preMoveSnapshot == null)
            return;

        board.RestoreSnapshot(preMoveSnapshot);
    }

    /// <summary>
    /// Returns the list of entity IDs affected by this command.
    /// </summary>
    public IEnumerable<string> AffectedEntityIds => entityDeltas.Keys;
}
