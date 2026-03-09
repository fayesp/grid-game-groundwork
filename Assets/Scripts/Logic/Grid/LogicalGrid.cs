using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class LogicalGrid
{
    #region variables
    private Dictionary<Vector3, HashSet<GameObject>> Contents;
    #endregion variables
    #region constructor
    public LogicalGrid()
    {
        Contents = new Dictionary<Vector3, HashSet<GameObject>>();
    }
    #endregion constructor
    #region sync contents
    /// <summary>
    /// 同步每个三维坐标包含的物体
    /// </summary>
    /// <param name="tiles"></param>
    public void SyncContents(GameObject[] tiles)
    {
        foreach (KeyValuePair<Vector3, HashSet<GameObject>> kv in Contents)
        {
            kv.Value.Clear();
        }
        foreach (var tile in tiles)
        {
            //占据多格的块,分别加入到不同的整数pos
            Vector3 pos = tile.transform.position;
            List<Vector3Int> occupyPos = GetAllPositions(pos);
            foreach (Vector3Int p in occupyPos)
            {
                if (!Contents.ContainsKey(p))
                    Contents[p] = new HashSet<GameObject>();
                Contents[p].Add(tile);
            }
        }
        foreach (var kv in Contents)
            if (kv.Value.Count < 1)
            { Contents[kv.Key].Clear(); }
    }
    /// <summary>
    /// position占据的所有整数坐标
    /// </summary>
    /// <param name="Position"></param>
    /// <returns></returns>
    public List<Vector3Int> GetAllPositions(Vector3 Position)
    {
        List<Vector3Int> positions = new List<Vector3Int>();
        List<int> xPositions = new List<int>();
        List<int> yPositions = new List<int>();
        List<int> zPositions = new List<int>();
        if (Position.x % 1 != 0)
        {
            xPositions.Add(Mathf.FloorToInt(Position.x));
            xPositions.Add(Mathf.CeilToInt(Position.x));
        }
        else
        {
            xPositions.Add((int)Position.x);
        }
        if (Position.y % 1 != 0)
        {
            yPositions.Add(Mathf.FloorToInt(Position.y));
            yPositions.Add(Mathf.CeilToInt(Position.y));
        }
        else
        {
            yPositions.Add((int)Position.y);
        }
        if (Position.z % 1 != 0)
        {
            zPositions.Add(Mathf.FloorToInt(Position.z));
            zPositions.Add(Mathf.CeilToInt(Position.z));
        }
        else
        {
            zPositions.Add((int)Position.z);
        }
        foreach (int x in xPositions)
        {
            foreach (int y in yPositions)
            {
                foreach (int z in zPositions)
                {
                    positions.Add(new Vector3Int(x, y, z));
                }
            }
        }
        return positions;
    }

    #endregion sync contents
    #region get content at
    /// <summary>
    /// 每个坐标包含的物体
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public HashSet<GameObject> GetContentsAt(Vector3Int pos)
    {
        if (!Contents.ContainsKey(pos))
            Contents[pos] = new HashSet<GameObject>(); // weird side effect...
        return Contents[pos];
    }

    #endregion get content at
}