using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using Assets.Scripts;

/// <summary>
/// Main utility class that provides backward-compatible access to grid queries,
/// direction utilities, and level management functions.
///
/// For new code, prefer using the specialized classes directly:
/// - GridQuery: Position queries (GetMoverAtPos, WallIsAtPos, TileIsEmpty, etc.)
/// - DirectionUtils: Direction calculations (CheckDirection, IsRound, etc.)
/// </summary>
public class Utils
{
    #region Move Unit Vectors (delegates to DirectionUtils)
    public static Vector3Int forward { get { return DirectionUtils.forward; } }
    public static Vector3Int back { get { return DirectionUtils.back; } }
    #endregion

    #region Level Management

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
    #endregion

    #region Scene Helpers

    public static string sceneName => SceneManager.GetActiveScene().name;
    public static bool isMetaScene => LevelManager.currentLevelName.Contains("0_");
    #endregion

    #region Vector Utilities (delegates to DirectionUtils)

    public static bool Roughly(float one, float two, float tolerance = 0.5f)
    {
        return DirectionUtils.Roughly(one, two, tolerance);
    }

    public static bool VectorRoughly(Vector3 one, Vector3 two, float t = 0.5f)
    {
        return DirectionUtils.VectorRoughly(one, two, t);
    }

    public static bool VectorRoughly2D(Vector3 one, Vector3 two, float t = 0.5f)
    {
        return DirectionUtils.VectorRoughly2D(one, two, t);
    }

    public static Vector3Int Vec3ToInt(Vector3 v)
    {
        return DirectionUtils.Vec3ToInt(v);
    }

    public static void RoundPosition(Transform t)
    {
        DirectionUtils.RoundPosition(t);
    }

    public static void RoundRotation(Transform t)
    {
        DirectionUtils.RoundRotation(t);
    }

    public static Vector3 StandardizeRotation(Vector3 v)
    {
        return DirectionUtils.StandardizeRotation(v);
    }

    public static Direction CheckDirection(Vector3 dir)
    {
        return DirectionUtils.CheckDirection(dir);
    }

    public static bool IsRound(Vector3 vector3, Direction dir)
    {
        return DirectionUtils.IsRound(vector3, dir);
    }
    #endregion

    #region Grid Queries (delegates to GridQuery)

    public static void AvoidIntersect(Transform root)
    {
        GridQuery.AvoidIntersect(root);
    }

    public static Vector3 AvoidIntersect(Vector3 v)
    {
        return GridQuery.AvoidIntersect(v);
    }

    public static bool TileIsEmpty(Vector3 pos, bool ignorePlayer)
    {
        return GridQuery.TileIsEmpty(pos, ignorePlayer);
    }

    public static bool TileIsEmpty(Vector3Int pos, bool ignorePlayer)
    {
        return GridQuery.TileIsEmpty(pos, ignorePlayer);
    }

    public static bool TileIsEmpty(Vector3 pos)
    {
        return GridQuery.TileIsEmpty(pos);
    }

    public static bool TileIsEmpty(Vector3Int pos)
    {
        return GridQuery.TileIsEmpty(pos);
    }

    public static HashSet<GameObject> GetTilesAt(Vector3Int pos)
    {
        return GridQuery.GetTilesAt(pos);
    }

    public static GameObject GetTaggedObjAtPos(Vector3Int pos, string tag)
    {
        return GridQuery.GetTaggedObjAtPos(pos, tag);
    }

    public static bool TaggedObjIsAtPos(Vector3Int pos, string tag)
    {
        return GridQuery.TaggedObjIsAtPos(pos, tag);
    }

    public static List<Wall> GetWallAtPos(Vector3Int pos)
    {
        return GridQuery.GetWallAtPos(pos);
    }

    public static List<Wall> GetWallAtPos(Vector3 pos)
    {
        return GridQuery.GetWallAtPos(pos);
    }

    public static bool WallIsAtPos(Vector3Int pos)
    {
        return GridQuery.WallIsAtPos(pos);
    }

    public static List<Mover> GetMoverAtPos(Vector3 pos)
    {
        return GridQuery.GetMoverAtPos(pos);
    }

    public static List<Mover> GetMoverAtPos(Vector3Int pos)
    {
        return GridQuery.GetMoverAtPos(pos);
    }

    public static bool MoverIsAtPos(Vector3 pos)
    {
        return GridQuery.MoverIsAtPos(pos);
    }

    public static bool MoverIsAtPos(Vector3Int pos)
    {
        return GridQuery.MoverIsAtPos(pos);
    }

    public static bool PlayerAtPos(Vector3 v)
    {
        return GridQuery.PlayerAtPos(v);
    }

    public static List<Mover> MoversAbove(Mover m, bool clear = true)
    {
        return GridQuery.MoversAbove(m, clear);
    }

    public static bool AIsHigherThanB(Transform a, Transform b)
    {
        return GridQuery.AIsHigherThanB(a, b);
    }

    public static bool GroundBelowPosition(Vector3Int v, Mover source = null)
    {
        return GridQuery.GroundBelowPosition(v, source);
    }

    public static bool GroundBelowTile(Tile tile)
    {
        return GridQuery.GroundBelowTile(tile);
    }

    public static bool GroundBelowPlayer()
    {
        return GridQuery.GroundBelowPlayer();
    }

    public static bool GroundBelow(Mover m)
    {
        return GridQuery.GroundBelow(m);
    }
    #endregion

    #region Texture Utilities

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
    #endregion

    #region Type Conversion

    public static int StringToInt(string intString)
    {
        int i = 0;
        if (!System.Int32.TryParse(intString, out i))
        {
            i = 0;
        }
        return i;
    }
    #endregion
}
