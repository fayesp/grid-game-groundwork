using UnityEngine;

/// <summary>
/// Direction and vector utilities for movement calculations.
/// </summary>
public static class DirectionUtils
{
    #region Move Unit Vectors
    // forward is down (positive Z), back is up (negative Z)
    public static Vector3Int forward { get { return new Vector3Int(0, 0, 1); } }
    public static Vector3Int back { get { return new Vector3Int(0, 0, -1); } }
    #endregion

    #region Direction Detection

    /// <summary>
    /// Determine direction from a Vector3 movement vector.
    /// </summary>
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
    /// Check if the position is at an integer coordinate in the specified direction.
    /// Non-integer positions don't need wall collision checks.
    /// </summary>
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
    #endregion

    #region Vector Rounding

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
    #endregion
}
