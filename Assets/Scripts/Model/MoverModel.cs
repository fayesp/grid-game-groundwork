using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure C# model for a movable entity.
/// Wraps GridEntity and holds planning state.
/// </summary>
public class MoverModel
{
    public GridEntity Entity { get; private set; }

    /// <summary>
    /// The planned movement delta for the current turn.
    /// </summary>
    public Vector3 PlannedMove { get; private set; }

    /// <summary>
    /// Whether this mover is currently in a falling state.
    /// </summary>
    public bool IsFalling { get; set; }

    /// <summary>
    /// IDs of attached movers (e.g., magnet links).
    /// </summary>
    public List<string> AttachedMoverIds { get; private set; }

    public MoverModel(GridEntity entity)
    {
        Entity = entity;
        PlannedMove = Vector3.zero;
        IsFalling = false;
        AttachedMoverIds = new List<string>();
    }

    public void SetPlannedMove(Vector3 move)
    {
        PlannedMove = move;
    }

    public void AddToPlannedMove(Vector3 delta)
    {
        PlannedMove += delta;
    }

    public void ClearPlannedMove()
    {
        PlannedMove = Vector3.zero;
    }

    public bool HasPlannedMove()
    {
        return PlannedMove != Vector3Int.zero;
    }

    /// <summary>
    /// Executes the planned move on the underlying entity.
    /// </summary>
    public void ExecutePlannedMove(GameBoard board)
    {
        if (PlannedMove == Vector3Int.zero)
            return;

        board.MoveEntity(Entity.Id, PlannedMove);
        ClearPlannedMove();
    }

    /// <summary>
    /// Checks if this mover should fall (no ground below).
    /// </summary>
    public bool ShouldFall(IGridQuery grid)
    {
        return !grid.GroundBelow(Entity);
    }
}
