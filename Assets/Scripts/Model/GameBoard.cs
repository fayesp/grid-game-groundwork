using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Pure C# game board that owns all entity models and the spatial grid.
/// Central registry for game state. Fully testable without Unity.
/// </summary>
public class GameBoard
{
    private Dictionary<string, GridEntity> entities;
    private SpatialGrid spatialGrid;

    public GameBoard()
    {
        entities = new Dictionary<string, GridEntity>();
        spatialGrid = new SpatialGrid();
    }

    #region Entity Registration

    public void RegisterEntity(GridEntity entity)
    {
        entities[entity.Id] = entity;
        spatialGrid.RegisterEntity(entity);
    }

    public void UnregisterEntity(string entityId)
    {
        if (entities.TryGetValue(entityId, out var entity))
        {
            spatialGrid.UnregisterEntity(entity);
            entities.Remove(entityId);
        }
    }

    public GridEntity GetEntity(string entityId)
    {
        if (entities.TryGetValue(entityId, out var entity))
            return entity;

        return null;
    }

    public IEnumerable<GridEntity> GetAllEntities()
    {
        return entities.Values;
    }

    public IEnumerable<GridEntity> GetMovers()
    {
        return entities.Values.Where(e => !e.IsStatic);
    }

    public IEnumerable<GridEntity> GetWalls()
    {
        return entities.Values.Where(e => e.IsStatic);
    }

    public GridEntity GetPlayer()
    {
        return entities.Values.FirstOrDefault(e => e.IsPlayer);
    }

    #endregion

    #region Position Queries

    public IEnumerable<GridEntity> GetEntitiesAt(Vector3Int cell)
    {
        var ids = spatialGrid.GetEntityIdsAt(cell);
        foreach (var id in ids)
        {
            if (entities.TryGetValue(id, out var entity))
                yield return entity;
        }
    }

    public bool HasEntityAt(Vector3Int cell)
    {
        return spatialGrid.HasAnyEntityAt(cell);
    }

    public bool WallIsAtPos(Vector3Int cell)
    {
        return GetEntitiesAt(cell).Any(e => e.IsStatic);
    }

    public List<GridEntity> GetMoversAtPos(Vector3Int cell)
    {
        return GetEntitiesAt(cell).Where(e => !e.IsStatic).ToList();
    }

    public bool TileIsEmpty(Vector3Int cell)
    {
        return !spatialGrid.HasAnyEntityAt(cell);
    }

    public bool GroundBelow(GridEntity entity)
    {
        foreach (var cell in entity.OccupiedCells)
        {
            if (cell.z == 0)
                return true;

            Vector3Int below = cell + new Vector3Int(0, 0, 1); // forward is +Z
            if (WallIsAtPos(below))
                return true;

            var moversBelow = GetMoversAtPos(below);
            if (moversBelow.Any(m => m.Id != entity.Id))
                return true;
        }

        return false;
    }

    #endregion

    #region Entity Movement

    public void MoveEntity(string entityId, Vector3 delta)
    {
        if (!entities.TryGetValue(entityId, out var entity))
            return;

        Vector3 oldPosition = entity.Position;
        entity.MoveBy(delta);
        spatialGrid.UpdateEntityPosition(entity, oldPosition);
    }

    public void SetEntityPosition(string entityId, Vector3 position)
    {
        if (!entities.TryGetValue(entityId, out var entity))
            return;

        Vector3 oldPosition = entity.Position;
        entity.SetPosition(position);
        spatialGrid.UpdateEntityPosition(entity, oldPosition);
    }

    public void SetEntityRotation(string entityId, Vector3 rotation)
    {
        if (!entities.TryGetValue(entityId, out var entity))
            return;

        entity.SetRotation(rotation);
    }

    #endregion

    #region Snapshot / State

    /// <summary>
    /// Creates a snapshot of all entity positions and rotations.
    /// Useful for undo/redo.
    /// </summary>
    public Dictionary<string, EntityTransformSnapshot> CreateSnapshot()
    {
        var snapshot = new Dictionary<string, EntityTransformSnapshot>();
        foreach (var entity in entities.Values)
        {
            snapshot[entity.Id] = new EntityTransformSnapshot
            {
                Position = entity.Position,
                Rotation = entity.Rotation
            };
        }

        return snapshot;
    }

    /// <summary>
    /// Restores entity positions and rotations from a snapshot.
    /// </summary>
    public void RestoreSnapshot(Dictionary<string, EntityTransformSnapshot> snapshot)
    {
        foreach (var pair in snapshot)
        {
            if (entities.TryGetValue(pair.Key, out var entity))
            {
                SetEntityPosition(pair.Key, pair.Value.Position);
                SetEntityRotation(pair.Key, pair.Value.Rotation);
            }
        }
    }

    #endregion

    #region Utility

    public void Clear()
    {
        entities.Clear();
        spatialGrid.Clear();
    }

    public int EntityCount => entities.Count;

    #endregion
}

/// <summary>
/// Immutable snapshot of an entity's transform state.
/// </summary>
public struct EntityTransformSnapshot
{
    public Vector3 Position;
    public Vector3 Rotation;
}
