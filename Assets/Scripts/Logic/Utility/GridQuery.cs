using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Grid query utilities for checking positions and getting objects at specific grid locations.
/// </summary>
public static class GridQuery
{
    #region Tile Empty Check
    public static bool TileIsEmpty(Vector3 pos, bool ignorePlayer)
    {
        return TileIsEmpty(Utils.Vec3ToInt(pos), ignorePlayer);
    }

    public static bool TileIsEmpty(Vector3Int pos, bool ignorePlayer)
    {
        if (WallIsAtPos(pos)) return false;
        List<Mover> movers = GetMoverAtPos(pos);
        if (movers.Any(m => m != null && !m.isPlayer)) return false;
        return true;
    }

    public static bool TileIsEmpty(Vector3 pos)
    {
        return TileIsEmpty(Utils.Vec3ToInt(pos));
    }

    public static bool TileIsEmpty(Vector3Int pos)
    {
        return WallIsAtPos(pos) == false && MoverIsAtPos(pos) == false;
    }
    #endregion

    #region Get Tiles At Position

    public static HashSet<GameObject> GetTilesAt(Vector3Int pos)
    {
        if (Game.instance == null) return new HashSet<GameObject>();
        return Game.instance.Grid.GetContentsAt(pos);
    }
    #endregion

    #region Object At Position

    private static List<T> GetObjAtPos<T>(Vector3Int pos)
    {
        List<T> list = new List<T>();
        foreach (var tile in GetTilesAt(pos))
        {
            var o = tile.GetComponentInParent<T>();
            if (o != null)
            {
                list.Add(o);
            }
        }
        return list;
    }

    public static GameObject GetTaggedObjAtPos(Vector3Int pos, string tag)
    {
        foreach (var tile in GetTilesAt(pos))
        {
            if (tile.transform.parent.CompareTag(tag))
            {
                return tile;
            }
        }
        return null;
    }

    public static bool TaggedObjIsAtPos(Vector3Int pos, string tag)
    {
        return GetTaggedObjAtPos(pos, tag) != null;
    }
    #endregion

    #region Wall At Position

    public static List<Wall> GetWallAtPos(Vector3Int pos)
    {
        return GetObjAtPos<Wall>(pos);
    }

    public static List<Wall> GetWallAtPos(Vector3 pos)
    {
        return GetWallAtPos(Utils.Vec3ToInt(pos));
    }

    public static bool WallIsAtPos(Vector3Int pos)
    {
        return GetWallAtPos(pos).Count > 0;
    }
    #endregion

    #region Mover At Position

    public static List<Mover> GetMoverAtPos(Vector3 pos)
    {
        return GetMoverAtPos(Utils.Vec3ToInt(pos));
    }

    public static List<Mover> GetMoverAtPos(Vector3Int pos)
    {
        return GetObjAtPos<Mover>(pos);
    }

    public static bool MoverIsAtPos(Vector3 pos)
    {
        return MoverIsAtPos(Utils.Vec3ToInt(pos));
    }

    public static bool MoverIsAtPos(Vector3Int pos)
    {
        return GetMoverAtPos(pos).Count > 0;
    }
    #endregion

    #region Player At Position

    public static bool PlayerAtPos(Vector3 v)
    {
        List<Mover> movers = GetMoverAtPos(v);
        if (movers.Count == 0) return false;
        if (movers.Any(m => m.isPlayer))
        {
            return true;
        }
        return false;
    }
    #endregion

    #region Above/Below Detection

    private static List<Mover> moversCache = new List<Mover>();

    public static List<Mover> MoversAbove(Mover m, bool clear = true)
    {
        if (clear)
        {
            moversCache.Clear();
        }
        foreach (Tile t in m.tiles)
        {
            List<Mover> ms = GetMoverAtPos(t.pos + Vector3.back);
            foreach (var mover in ms)
            {
                if (mover != null && !moversCache.Contains(mover))
                {
                    moversCache.Add(mover);
                    MoversAbove(mover, false);
                }
            }
        }
        return moversCache.Distinct().ToList();
    }

    public static bool AIsHigherThanB(Transform a, Transform b)
    {
        return a.position.z < b.position.z;
    }

    public static bool GroundBelowPosition(Vector3Int v, Mover source = null)
    {
        Vector3Int posToCheck = v + Utils.forward;
        if (WallIsAtPos(posToCheck))
        {
            return true;
        }
        List<Mover> movers = GetMoverAtPos(posToCheck);
        if (movers.Any(m => m != null && m != source && !m.isFalling))
        {
            return true;
        }
        return false;
    }

    public static bool GroundBelowTile(Tile tile)
    {
        return GroundBelowPosition(tile.pos);
    }

    public static bool GroundBelowPlayer()
    {
        return GroundBelow(Player.instance);
    }

    public static bool GroundBelow(Mover m)
    {
        foreach (Tile tile in m.tiles)
        {
            if (tile.pos.z == 0)
                return true;
            if (GroundBelowTile(tile))
            {
                return true;
            }
        }
        return false;
    }
    #endregion

    #region Avoid Intersect

    public static void AvoidIntersect(Transform root)
    {
        bool intersecting = true;
        while (intersecting)
        {
            intersecting = false;
            foreach (Transform tile in root)
            {
                if (tile.gameObject.CompareTag("Tile"))
                {
                    List<Mover> movers = GetMoverAtPos(tile.position);
                    if (movers.Any(m => m != null && m.transform != root))
                    {
                        root.position += Vector3.back;
                        intersecting = true;
                    }
                    else
                    {
                        List<Wall> walls = GetWallAtPos(tile.position);
                        if (walls.Any(wall => wall != null && wall.transform != root))
                        {
                            root.position += Vector3.back;
                            intersecting = true;
                        }
                    }
                }
            }
        }
    }

    public static Vector3 AvoidIntersect(Vector3 v)
    {
        bool intersecting = true;
        while (intersecting)
        {
            intersecting = false;
            if (!TileIsEmpty(v))
            {
                v += Vector3.back;
                intersecting = true;
            }
        }
        return v;
    }
    #endregion
}
