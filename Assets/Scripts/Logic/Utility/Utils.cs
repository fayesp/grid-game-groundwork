using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Globalization;
using System;
using Assets.Scripts;
public class Utils
{
    #region variables
    public static List<Mover> movers = new List<Mover>();
    // These are built into newer versions of Unity.
    //forward是向下,back是向上
    #region move unit vector
    public static Vector3Int forward { get { return new Vector3Int(0, 0, 1); } }
    public static Vector3Int back { get { return new Vector3Int(0, 0, -1); } }
    #endregion move unit
    #endregion variables
    #region levels relation
    //level是使用level name的json文件存储
    private static List<string> allLevelsRef;
    public static List<string> allLevels
    {
        get
        {
            if (allLevelsRef == null)
            {
                allLevelsRef = new List<string>();
                UnityEngine.Object[] levels = Resources.LoadAll("Levels");
                foreach (UnityEngine.Object t in levels)
                {
                    allLevelsRef.Add(t.name);
                }
            }
            return allLevelsRef;
        }
    }
    public static void RefreshLevels()
    {
        allLevelsRef = null;
    }
    public static IEnumerator LoadScene(string scene)
    {
        yield return WaitFor.EndOfFrame;
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }
    #endregion levels
    #region scene relation
    public static string sceneName => SceneManager.GetActiveScene().name;
    //todo ?0_表示元场景?
    public static bool isMetaScene => LevelManager.currentLevelName.Contains("0_");
    #endregion scene relation
    #region round,roughly function
    public static bool Roughly(float one, float two, float tolerance = 0.5f)
    {
        return Mathf.Abs(one - two) < tolerance;
    }
    public static bool VectorRoughly(Vector3 one, Vector3 two, float t = 0.5f)
    {
        return Roughly(one.x, two.x, t) && Roughly(one.y, two.y, t) && Roughly(one.z, two.z, t);
    }
    public static bool VectorRoughly2D(Vector3 one, Vector3 two, float t = 0.5f)
    {
        return Roughly(one.x, two.x, t) && Roughly(one.y, two.y, t);
    }
    public static Vector3Int Vec3ToInt(Vector3 v)
    {
        return Vector3Int.RoundToInt(v);
    }
    public static void RoundPosition(Transform t)
    {
        Vector3 p = t.position;
        t.position = Vec3ToInt(p);
    }
    public static void RoundRotation(Transform t)
    {
        Vector3 r = t.eulerAngles;
        r = StandardizeRotation(r);
        t.eulerAngles = Vec3ToInt(r);
    }
    //todo 角度的范围是-5到355?
    public static Vector3 StandardizeRotation(Vector3 v)
    {
        if (v.z < -5)
        {
            float z = v.z;
            while (z < 0)
            {
                z += 360;
            }
            return new Vector3(v.x, v.y, z);
        }
        else if (v.z > 355)
        {
            float z = v.z;
            while (z > 355)
            {
                z -= 360;
            }
            return new Vector3(v.x, v.y, z);
        }
        else
        {
            return v;
        }
    }
    #endregion 向量处理
    #region avoid intersect 避免相交
    //todo 这里加了一个back,z的负方向是上方?
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
    /// <summary>
    /// 避免相交传模,back是往上面绘制prefabs
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
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
    #endregion avoid intersect 避免相交
    #region tile is empty check
    public static bool TileIsEmpty(Vector3 pos, bool ignorePlayer)
    {
        return TileIsEmpty(Vec3ToInt(pos), ignorePlayer);
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
        return TileIsEmpty(Vec3ToInt(pos));
    }
    public static bool TileIsEmpty(Vector3Int pos)
    {
        return WallIsAtPos(pos) == false && MoverIsAtPos(pos) == false;
    }
    #endregion tile is empty check
    #region At pos function
    public static HashSet<GameObject> GetTilesAt(Vector3Int pos)
    {
        if (Game.instance == null) return new HashSet<GameObject>();
        return Game.instance.Grid.GetContentsAt(pos);
    }
    //todo 这个函数的作用?GetComponentInParent
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
    #region wall at
    public static List<Wall> GetWallAtPos(Vector3Int pos)
    {
        return GetObjAtPos<Wall>(pos);
    }
    public static List<Wall> GetWallAtPos(Vector3 pos)
    {
        return GetWallAtPos(Vec3ToInt(pos));
    }
    public static bool WallIsAtPos(Vector3Int pos)
    {
        return GetWallAtPos(pos).Count > 0;
    }
    #endregion wall at
    #region mover at
    public static List<Mover> GetMoverAtPos(Vector3 pos)
    {
        return GetMoverAtPos(Vec3ToInt(pos));
    }
    //todo 修改判定接触这个方格的mover
    public static List<Mover> GetMoverAtPos(Vector3Int pos)
    {
        return GetObjAtPos<Mover>(pos);
    }
    public static bool MoverIsAtPos(Vector3 pos)
    {
        return MoverIsAtPos(Vec3ToInt(pos));
    }
    public static bool MoverIsAtPos(Vector3Int pos)
    {
        return GetMoverAtPos(pos).Count > 0;
    }
    #endregion mover at
    #region player at
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
    #endregion player at
    #endregion At pos function
    #region above detect
    public static List<Mover> MoversAbove(Mover m, bool clear = true)
    {
        if (clear)
        {
            movers.Clear();
        }
        foreach (Tile t in m.tiles)
        {
            List<Mover> ms = GetMoverAtPos(t.pos + Vector3.back);
            foreach (var mover in ms)
            {
                if (mover != null && !movers.Contains(mover))
                {
                    movers.Add(mover);
                    movers.AddRange(MoversAbove(mover, false));
                }
            }
        }
        return movers.Distinct().ToList();
    }
    public static bool AIsHigherThanB(Transform a, Transform b)
    {
        return a.position.z < b.position.z;
    }
    #endregion above detect
    #region below detect
    public static bool GroundBelowPosition(Vector3Int v, Mover source = null)
    {
        Vector3Int posToCheck = v + forward;
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
            if (Utils.GroundBelowTile(tile))
            {
                return true;
            }
        }
        return false;
    }
    #endregion below detect
    #region texture
    public static Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
    #endregion texture
    #region type change fuction
    public static int StringToInt(string intString)
    {
        int i = 0;
        if (!System.Int32.TryParse(intString, out i))
        {
            i = 0;
        }
        return i;
    }
    #endregion helper fuction
    #region 新增
    /// <summary>
    /// 依据Vector3的值判断方向
    /// </summary>
    /// <param name="dir"></param>
    /// <returns></returns>
    public static Direction CheckDirection(Vector3 dir)
    {
        if (dir.x > 0 && dir.y == 0 && dir.z == 0)
        {
            return Direction.Right;
        }
        else if (dir.x < 0 && dir.y == 0 && dir.z == 0)
        {
            return Direction.Left;
        }
        else if (dir.x == 0 && dir.y > 0 && dir.z == 0)
        {
            return Direction.Up;
        }
        else if (dir.x == 0 && dir.y < 0 && dir.z == 0)
        {
            return Direction.Down;
        }
        else if (dir.x == 0 && dir.y == 0 && dir.z > 0)
        {
            return Direction.Forward;
        }
        else if (dir.x == 0 && dir.y == 0 && dir.z < 0)
        {
            return Direction.Back;
        }
        else
        {
            return Direction.None;
        }
    }
    /// <summary>
    /// 检查指定方向的Vector3是否是一个整数,小数则不用检查是否有墙
    /// </summary>
    /// <param name="vector3"></param>
    /// <param name="dir"></param>
    /// <returns></returns>
    public static bool IsRound(Vector3 vector3, Direction dir)
    {
        switch (dir)
        {
            case Direction.None:
                return false;
            case Direction.Up:
            case Direction.Down:
                return vector3.y % 1 == 0;
            case Direction.Left:
            case Direction.Right:
                return vector3.x % 1 == 0;
            case Direction.Forward:
            case Direction.Back:
                return vector3.z % 1 == 0;
            default:
                return false;
        }
    }
    #endregion 新增
}