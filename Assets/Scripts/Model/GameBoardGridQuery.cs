using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Adapter that exposes GameBoard queries through the IGridQuery interface.
/// </summary>
public class GameBoardGridQuery : IGridQuery
{
    private readonly GameBoard board;

    public GameBoardGridQuery(GameBoard board)
    {
        this.board = board;
    }

    public bool WallIsAtPos(Vector3Int pos)
    {
        return board?.WallIsAtPos(pos) ?? false;
    }

    public List<GridEntity> GetMoversAtPos(Vector3Int pos)
    {
        return board?.GetMoversAtPos(pos) ?? new List<GridEntity>();
    }

    public bool TileIsEmpty(Vector3Int pos)
    {
        return board?.TileIsEmpty(pos) ?? true;
    }

    public bool TileIsEmpty(Vector3Int pos, bool ignorePlayer)
    {
        if (board == null)
            return true;

        var entities = board.GetEntitiesAt(pos);
        if (ignorePlayer)
        {
            return !entities.Any(e => !e.IsPlayer);
        }

        return !entities.Any();
    }

    public bool GroundBelow(GridEntity entity)
    {
        return board?.GroundBelow(entity) ?? false;
    }

    public bool GroundBelowPosition(Vector3Int pos, GridEntity source = null)
    {
        if (board == null)
            return false;

        if (pos.z == 0)
            return true;

        Vector3Int below = pos + new Vector3Int(0, 0, 1);
        if (board.WallIsAtPos(below))
            return true;

        var movers = board.GetMoversAtPos(below);
        return movers.Any(m => source == null || m.Id != source.Id);
    }
}
