using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Pure C# movement planner that validates and plans moves without Unity dependencies.
/// Extracted from Mover.CanMoveToward, PlanMove, and PlanPushes.
///
/// TODO: Full extraction of multi-tile push logic and magnet attach propagation.
/// </summary>
public class MovePlanner
{
    private readonly IGridQuery gridQuery;
    private readonly bool allowPushMulti;

    public MovePlanner(IGridQuery gridQuery, bool allowPushMulti)
    {
        this.gridQuery = gridQuery;
        this.allowPushMulti = allowPushMulti;
    }

    #region Public API

    /// <summary>
    /// Tries to plan a move for the given mover in the indicated direction.
    /// Returns true if the move is valid, populating the result with all affected entities and their deltas.
    /// </summary>
    public bool TryPlanMove(MoverModel mover, Vector3 moveDelta, Direction dir, out PlannedMoveResult result)
    {
        result = new PlannedMoveResult();

        if (gridQuery == null || mover == null)
            return false;

        // Prevent self-move or zero move
        if (moveDelta == Vector3.zero)
            return false;

        var visited = new HashSet<string>();
        if (!CanMoveToward(mover, ref moveDelta, dir, visited))
            return false;

        visited.Clear();
        PlanMoveRecursive(mover, moveDelta, dir, visited, result);

        return true;
    }

    #endregion

    #region Validation

    /// <summary>
    /// Checks whether the mover can move in the given direction.
    /// Propagates checks to pushed movers.
    /// </summary>
    private bool CanMoveToward(MoverModel mover, ref Vector3 moveDelta, Direction dir, HashSet<string> visited)
    {
        if (visited.Contains(mover.Entity.Id))
            return true;

        visited.Add(mover.Entity.Id);

        foreach (var cell in mover.Entity.OccupiedCells)
        {
            var checkPositions = GetCellsInDirection(cell, dir);

            foreach (var checkPos in checkPositions)
            {
                // Wall collision
                if (gridQuery.WallIsAtPos(checkPos))
                    return false;

                // Mover collision / push check
                var otherMovers = gridQuery.GetMoversAtPos(checkPos);
                foreach (var other in otherMovers)
                {
                    if (other == null || other.Id == mover.Entity.Id)
                        continue;

                    // Only player can push if allowPushMulti is false
                    if (!mover.Entity.IsPlayer && !allowPushMulti)
                        return false;

                    var otherModel = new MoverModel(other); // Temporary wrapper for checking
                    if (!CanMoveToward(otherModel, ref moveDelta, dir, visited))
                        return false;
                }
            }
        }

        return true;
    }

    #endregion

    #region Planning

    /// <summary>
    /// Recursively plans the move for the given mover and all pushed movers.
    /// </summary>
    private void PlanMoveRecursive(MoverModel mover, Vector3 moveDelta, Direction dir, HashSet<string> visited, PlannedMoveResult result)
    {
        if (visited.Contains(mover.Entity.Id))
            return;

        visited.Add(mover.Entity.Id);

        // Avoid redundant planning
        if (result.EntityDeltas.ContainsKey(mover.Entity.Id))
            return;

        result.EntityDeltas[mover.Entity.Id] = moveDelta;

        // Plan pushes to adjacent movers
        var pushedMovers = FindPushedMovers(mover, dir);
        foreach (var pushed in pushedMovers)
        {
            var pushedModel = new MoverModel(pushed);
            PlanMoveRecursive(pushedModel, moveDelta, dir, visited, result);
        }
    }

    /// <summary>
    /// Finds all movers that would be pushed by the given mover moving in the given direction.
    /// </summary>
    private List<GridEntity> FindPushedMovers(MoverModel mover, Direction dir)
    {
        var pushed = new List<GridEntity>();
        var seen = new HashSet<string>();

        foreach (var cell in mover.Entity.OccupiedCells)
        {
            var checkPositions = GetCellsInDirection(cell, dir);
            foreach (var checkPos in checkPositions)
            {
                var others = gridQuery.GetMoversAtPos(checkPos);
                foreach (var other in others)
                {
                    if (other != null && other.Id != mover.Entity.Id && !seen.Contains(other.Id))
                    {
                        seen.Add(other.Id);
                        pushed.Add(other);
                    }
                }
            }
        }

        return pushed;
    }

    #endregion

    #region Direction Helpers

    /// <summary>
    /// Gets the adjacent cell(s) in the given direction from a starting cell.
    /// </summary>
    private List<Vector3Int> GetCellsInDirection(Vector3Int cell, Direction dir)
    {
        var result = new List<Vector3Int>();

        switch (dir)
        {
            case Direction.Right:
                result.Add(new Vector3Int(cell.x + 1, cell.y, cell.z));
                break;
            case Direction.Left:
                result.Add(new Vector3Int(cell.x - 1, cell.y, cell.z));
                break;
            case Direction.Up:
                result.Add(new Vector3Int(cell.x, cell.y + 1, cell.z));
                break;
            case Direction.Down:
                result.Add(new Vector3Int(cell.x, cell.y - 1, cell.z));
                break;
            case Direction.Forward:
                result.Add(new Vector3Int(cell.x, cell.y, cell.z + 1));
                break;
            case Direction.Back:
                result.Add(new Vector3Int(cell.x, cell.y, cell.z - 1));
                break;
        }

        return result;
    }

    #endregion
}

/// <summary>
/// Result of planning a move: mapping from entity ID to movement delta.
/// </summary>
public class PlannedMoveResult
{
    public Dictionary<string, Vector3> EntityDeltas { get; private set; }

    public PlannedMoveResult()
    {
        EntityDeltas = new Dictionary<string, Vector3>();
    }

    public bool IsEmpty => EntityDeltas.Count == 0;
}
