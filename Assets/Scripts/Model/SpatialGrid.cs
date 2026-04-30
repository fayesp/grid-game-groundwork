using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure C# spatial hash grid for tracking entity occupancy.
/// Replaces LogicalGrid with a testable, GameObject-free implementation.
/// </summary>
public class SpatialGrid
{
    private Dictionary<Vector3Int, HashSet<string>> cells;

    public SpatialGrid()
    {
        cells = new Dictionary<Vector3Int, HashSet<string>>();
    }

    public void Clear()
    {
        cells.Clear();
    }

    /// <summary>
    /// Registers an entity's occupied cells in the grid.
    /// </summary>
    public void RegisterEntity(GridEntity entity)
    {
        foreach (var cell in entity.OccupiedCells)
        {
            if (!cells.ContainsKey(cell))
                cells[cell] = new HashSet<string>();

            cells[cell].Add(entity.Id);
        }
    }

    /// <summary>
    /// Removes an entity from all occupied cells.
    /// </summary>
    public void UnregisterEntity(GridEntity entity)
    {
        foreach (var cell in entity.OccupiedCells)
        {
            if (cells.ContainsKey(cell))
                cells[cell].Remove(entity.Id);
        }
    }

    /// <summary>
    /// Updates an entity's position in the grid. Efficiently unregisters old cells and registers new ones.
    /// </summary>
    public void UpdateEntityPosition(GridEntity entity, Vector3 oldPosition)
    {
        var oldCells = GetCellsForPosition(oldPosition, entity.TileOffsets);
        var newCells = new HashSet<Vector3Int>(entity.OccupiedCells);

        foreach (var cell in oldCells)
        {
            if (!newCells.Contains(cell) && cells.ContainsKey(cell))
                cells[cell].Remove(entity.Id);
        }

        foreach (var cell in newCells)
        {
            if (!cells.ContainsKey(cell))
                cells[cell] = new HashSet<string>();

            cells[cell].Add(entity.Id);
        }
    }

    /// <summary>
    /// Gets all entity IDs at a given grid cell.
    /// </summary>
    public HashSet<string> GetEntityIdsAt(Vector3Int cell)
    {
        if (cells.ContainsKey(cell))
            return new HashSet<string>(cells[cell]);

        return new HashSet<string>();
    }

    /// <summary>
    /// Checks if any entity occupies the given cell.
    /// </summary>
    public bool HasAnyEntityAt(Vector3Int cell)
    {
        return cells.ContainsKey(cell) && cells[cell].Count > 0;
    }

    private HashSet<Vector3Int> GetCellsForPosition(Vector3 position, List<Vector3Int> tileOffsets)
    {
        var result = new HashSet<Vector3Int>();
        if (tileOffsets == null || tileOffsets.Count == 0)
        {
            result.Add(Vector3Int.RoundToInt(position));
            return result;
        }

        foreach (var offset in tileOffsets)
        {
            result.Add(Vector3Int.RoundToInt(position + offset));
        }

        return result;
    }
}
