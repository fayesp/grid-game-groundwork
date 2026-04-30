using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for grid-based position queries.
/// Decouples movement planning from concrete grid implementations,
/// enabling unit tests with mock grids.
/// </summary>
public interface IGridQuery
{
    bool WallIsAtPos(Vector3Int pos);

    List<GridEntity> GetMoversAtPos(Vector3Int pos);

    bool TileIsEmpty(Vector3Int pos);

    bool TileIsEmpty(Vector3Int pos, bool ignorePlayer);

    bool GroundBelow(GridEntity entity);

    bool GroundBelowPosition(Vector3Int pos, GridEntity source = null);
}
