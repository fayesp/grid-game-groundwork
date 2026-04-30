using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Pure C# data representation of a game entity (Mover, Wall, etc.).
/// No Unity dependencies except Vector3 value types.
/// </summary>
public class GridEntity
{
    public string Id;
    public Vector3 Position;
    public Vector3 Rotation;
    public List<Vector3Int> TileOffsets;
    public bool IsPlayer;
    public bool IsStatic;
    public string EntityType;

    public GridEntity(string id, string entityType)
    {
        Id = id;
        EntityType = entityType;
        Position = Vector3.zero;
        Rotation = Vector3.zero;
        TileOffsets = new List<Vector3Int> { Vector3Int.zero };
        IsPlayer = false;
        IsStatic = false;
    }

    /// <summary>
    /// All integer grid cells occupied by this entity.
    /// </summary>
    public IEnumerable<Vector3Int> OccupiedCells
    {
        get
        {
            if (TileOffsets == null || TileOffsets.Count == 0)
            {
                yield return Vector3Int.RoundToInt(Position);
                yield break;
            }

            foreach (var offset in TileOffsets)
            {
                yield return Vector3Int.RoundToInt(Position + offset);
            }
        }
    }

    /// <summary>
    /// All integer grid cells occupied by this entity after applying a delta.
    /// </summary>
    public IEnumerable<Vector3Int> GetOccupiedCellsAfterMove(Vector3 delta)
    {
        Vector3 newPos = Position + delta;
        if (TileOffsets == null || TileOffsets.Count == 0)
        {
            yield return Vector3Int.RoundToInt(newPos);
            yield break;
        }

        foreach (var offset in TileOffsets)
        {
            yield return Vector3Int.RoundToInt(newPos + offset);
        }
    }

    public void MoveBy(Vector3 delta)
    {
        Position += delta;
    }

    public void SetPosition(Vector3 position)
    {
        Position = position;
    }

    public void SetRotation(Vector3 rotation)
    {
        Rotation = rotation;
    }
}
