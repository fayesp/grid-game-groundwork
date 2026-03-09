using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using TMPro;
using Assets.Scripts;
using System.IO;
using System;
using System.Runtime.Remoting.Messaging;
using System.Linq;

public class Mover : MonoBehaviour
{
    #region variables

    public List<Tile> tiles = new List<Tile>();
    [HideInInspector] public bool isFalling = false;
    [HideInInspector] public bool isNotRound = false;

    public bool isPlayer { get { return CompareTag("Player"); } }

    // During a movement cycle, what's the next move (as a difference
    // from its current position) that this Mover will try to make?

    //planedMove会不会有同时赋值冲突的问题
    private object setValue = new object();
    private Vector3 plannedMove;
    public Vector3 PlannedMove
    {
        get => plannedMove;
        set
        {
            lock (setValue)
            {
                plannedMove = value + plannedMove;
            }
        }
    }

    [HideInInspector]public List<Vector3Int> rightDir = new List<Vector3Int>();
    [HideInInspector]public List<Vector3Int> leftDir = new List<Vector3Int>();
    [HideInInspector]public List<Vector3Int> upDir = new List<Vector3Int>();
    [HideInInspector]public List<Vector3Int> downDir = new List<Vector3Int>();
    [HideInInspector]public List<Vector3Int> forwardDir = new List<Vector3Int>();
    [HideInInspector]public List<Vector3Int> backDir = new List<Vector3Int>();
    [HideInInspector]public List<Transform> AttachBlock = new List<Transform>();


    #endregion variables

    #region initialize

    void Start()
    {
        CreateTiles();
    }

    void CreateTiles()
    {
        tiles.Clear();
        foreach (Transform child in transform)
        {
            if (child.gameObject.CompareTag("Tile"))
            {
                Tile tile = new Tile();
                tile.t = child;
                tiles.Add(tile);
            }
        }
    }

    #endregion initialize

    #region stop
    public void Stop()
    {
        plannedMove = Vector3.zero;
    }

    #endregion stop

    #region planed move

    /// <summary>
    /// Try to plan a move in the indicated direction, if that move is valid.
    /// </summary>
    /// <param name="MoveV3"></param>
    /// <returns></returns>
    public virtual bool TryPlanMove(Vector3 MoveV3, Direction Dir)
    {
        if (!CanMoveToward(ref MoveV3, Dir))
            return false;
        //todo canMoveToward改变了moveV3的值,planmove可以直接使用?
        PlanMove(MoveV3, Dir);
        return true;
    }

    public virtual bool CanMoveToward(ref Vector3 MoveV3, Direction Dir)
    {
        foreach (Tile tile in tiles)
        {
            isNotRound = false;
            if (Utils.IsRound(transform.position, Dir))
            {
                UpdateDirPos(tile.t.position);
            }
            else
            {
                //磁力影响移动跨越两格需要,判断墙是否阻挡一部分移动
                //判断移动后的目标格子里面有没有墙
                Vector3 posCheck = transform.position + MoveV3;
                if (!Utils.IsRound(posCheck, Dir))
                {
                    isNotRound = true;
                    UpdateDirPos(posCheck);
                }
                else
                {
                    //todo 可以直接返回?待验证
                    return true;
                }
            }

            List<Vector3Int> CheckPos = GetDirV3(Dir);
            foreach (var pos in CheckPos)
            {
                if (Utils.WallIsAtPos(pos))
                {
                    if (isNotRound)
                    {
                        //todo 判断moveV3减少多少
                        MoveRound(transform.position + MoveV3, ref MoveV3, Dir);
                    }
                    return false;
                }

                List<Mover> movers = Utils.GetMoverAtPos(pos);
                // Movers don't block themselves.
                if (movers.All(m => m == null || m == this))
                    continue;

                if (!isPlayer && !Game.allowPushMulti)
                    return false;

                //这个是可以推动多个箱子时判断的,
                //修复无限循环,tile可以增加一个checked的属性
                // XXX: could this cause an infinite loop with, say,
                // a U-shaped block and a single block inside, or two
                // interlocking U-blocks? We can fix this by passing
                // in (& ignoring) the set of already checked movers.
                foreach (var m in movers)
                {
                    if (!m.CanMoveToward(ref MoveV3, Dir))
                        return false;
                }
            }

        }
        return true;
    }


    public virtual void PlanMove(Vector3 MoveV3, Direction Dir)
    {
        //防止强迫推动?加的判断,
        //防止自己推动自己,两个U形,u形加单块,死循环
        // Optional optimization - avoid redundant pushes
        // with many multi-tile movers. Slightly fragile.
        //添加canmoveforward的判断,默认是assumes that CanMoveToward() already checked.

        if (PlannedMove == MoveV3)
            return;
        PlannedMove = MoveV3;
        PlanPushes(MoveV3, Dir);
    }

    // If there are other movers in the given direction,
    // push them in the same direction. Does not check whether
    // the move is possible - assumes that CanMoveToward()
    // already checked.
    //不适配多tile的块的push

    protected void PlanPushes(Vector3 MoveV3, Direction Dir)
    {
        foreach (Tile tile in tiles)
        {
            UpdateDirPos(tile.t.position);
            List<Vector3Int> CheckPos = new List<Vector3Int>();

            #region check pos tile
            if (Dir == Direction.Right)
            {
                CheckPos = rightDir;
            }
            else if (Dir == Direction.Left)
            {
                CheckPos = leftDir;
            }
            else if (Dir == Direction.Up)
            {
                CheckPos = upDir;
            }
            else if (Dir == Direction.Down)
            {
                CheckPos = downDir;
            }
            else if (Dir == Direction.Forward)
            {
                CheckPos = forwardDir;
            }
            else if (Dir == Direction.Back)
            {
                CheckPos = backDir;
            }
            #endregion

            List<Mover> moversToPush = new List<Mover>();
            foreach (Vector3Int posToCheck in CheckPos)
            {
                List<Mover> movers = Utils.GetMoverAtPos(posToCheck);
                foreach (var m in movers)
                {
                    List<GameObject> tileList = new List<GameObject>();
                    if (m == null || m == this)
                        continue;
                    if (!moversToPush.Contains(m))
                    { 
                        moversToPush.Add(m);
                    }
                }
            }

            List<List<Mover>> pushLists = GetAttachMoverList(moversToPush);

            foreach (List<Mover> moverList in pushLists)
            {
                if (moverList.Count < 1)
                    continue;
                List<Vector3> moveDirValues = new List<Vector3>();
                float moveValue = 0;
                foreach (var mover in moverList)
                {
                    Vector3 pushPos = transform.position;
                    Vector3 pushedPos = mover.transform.position;
                    switch (Dir)
                    {
                        case Direction.Right:
                            if ((moveValue = pushPos.x + MoveV3.x + 1 - pushedPos.x) > 0)
                            {
                                moveDirValues.Add(new Vector3(moveValue, 0, 0));
                            }
                            break;
                        case Direction.Left:
                            if ((moveValue = pushPos.x + MoveV3.x - pushedPos.x - 1) < 0)
                            {
                                moveDirValues.Add(new Vector3(moveValue, 0, 0));
                            }
                            break;
                        case Direction.Down:
                            if ((moveValue = pushPos.y + MoveV3.y - pushedPos.y - 1) < 0)
                            {
                                moveDirValues.Add(new Vector3(0, moveValue, 0));
                            }
                            break;
                        case Direction.Up:
                            if ((moveValue = pushPos.y + MoveV3.y + 1 - pushedPos.y) > 0)
                            {
                                moveDirValues.Add(new Vector3(0, moveValue, 0));
                            }
                            break;
                        case Direction.Forward:
                            if ((moveValue = pushPos.z + MoveV3.z + 1 - pushedPos.z) > 0)
                            {
                                moveDirValues.Add(new Vector3(0, 0, moveValue));
                            }
                            break;
                        case Direction.Back:
                            if ((moveValue = pushPos.z + MoveV3.z - pushedPos.z - 1) < 0)
                            {
                                moveDirValues.Add(new Vector3(0, 0, moveValue));
                            }
                            break;
                        case Direction.None:
                        default:
                            break;

                    }
                }

                Vector3 newMoveV3 = Vector3.zero;
                if (moveDirValues.Count > 0)
                {
                    foreach (var Value in moveDirValues)
                    {
                        if (Value.magnitude > newMoveV3.magnitude)
                        {
                            newMoveV3 = Value;
                        }
                    }
                    foreach (Mover mover in moverList)
                    {
                        if (newMoveV3.magnitude > 0)
                        {
                            //可以直接planMove,canmoveforward里面已经判断过了
                            mover.PlanMove(newMoveV3, Dir);
                        }
                    }
                }
            }

        }
    }
    #endregion planed move

    #region condition check

    public Vector3 Pos()
    {
        return transform.position;
    }

    public bool HasPlannedMove()
    {
        return PlannedMove != Vector3Int.zero;
    }


    #endregion condition check

    #region excute move


    // Perform the currently planned move (if any).
    public bool ExecuteLogicalMove()
    {
        if (PlannedMove == Vector3Int.zero)
            return false;

        transform.position = Pos() + PlannedMove;
        plannedMove = Vector3.zero;
        return true;
    }

    #endregion excute move

    #region after move effects


    // Handle effects that happen after moving, such as
    // planning to fall.
    public virtual void DoPostMoveEffects()
    {
        //todo 不一定是下落一格,有可能是半格,再落地
        if (ShouldFall())
            PlanMove(Utils.forward, Direction.Forward);
    }

    //添加磁力的判定等

    public virtual bool ShouldFall()
    {
        if (GroundBelow())
        {
            return false;
        }
        return true;
    }

    #endregion move effects

    #region ground below

    public bool GroundBelow()
    {
        foreach (Tile tile in tiles)
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


    /// <summary>
    /// 判断块下面有ground
    /// </summary>
    /// <param name="tile"></param>
    /// <returns></returns>
    bool GroundBelowTile(Tile tile)
    {
        //下面一格是墙
        Vector3Int posToCheck = tile.pos + Utils.forward;
        if (Utils.WallIsAtPos(posToCheck))
        {
            return true;
        }
        //下面有mover
        List<Mover> movers = Utils.GetMoverAtPos(posToCheck);
        if (movers.Any(m => m != null && m != this && !m.isFalling))
        {
            return true;
        }
        return false;
    }

    #endregion ground below

    #region draw cube
    //选中高亮显示
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            CreateTiles();
        }
        Gizmos.color = Color.blue;
        foreach (Tile tile in tiles)
        {
            Gizmos.DrawWireCube(tile.pos, Vector3.one);
        }
    }

    #endregion draw cube


    /// <summary>
    /// position占据的所有整数坐标
    /// </summary>
    /// <param name="Position"></param>
    /// <returns></returns>
    public void UpdateDirPos(Vector3 Position)
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

        rightDir.Clear();
        leftDir.Clear();
        upDir.Clear();
        downDir.Clear();
        forwardDir.Clear();
        backDir.Clear();
        foreach (int y in yPositions)
        {

            foreach (int z in zPositions)
            {
                if (xPositions.Count > 1)
                {

                    rightDir.Add(new Vector3Int(xPositions[1], y, z));
                    leftDir.Add(new Vector3Int(xPositions[0], y, z));
                }
                else
                {
                    rightDir.Add(new Vector3Int(xPositions[0] + 1, y, z));
                    leftDir.Add(new Vector3Int(xPositions[0] - 1, y, z));
                }
            }
        }

        foreach (int x in xPositions)
        {
            foreach (int z in zPositions)
            {
                if (yPositions.Count > 1)
                {
                    upDir.Add(new Vector3Int(x, yPositions[1], z));
                    downDir.Add(new Vector3Int(x, yPositions[0], z));
                }
                else
                {
                    upDir.Add(new Vector3Int(x, yPositions[0] + 1, z));
                    downDir.Add(new Vector3Int(x, yPositions[0] - 1, z));
                }
            }
        }

        foreach (int x in xPositions)
        {
            foreach (int y in yPositions)
            {
                if (zPositions.Count > 1)
                {

                    forwardDir.Add(new Vector3Int(x, y, zPositions[1]));
                    backDir.Add(new Vector3Int(x, y, zPositions[0]));
                }
                else
                {
                    forwardDir.Add(new Vector3Int(x, y, zPositions[0] + 1));
                    backDir.Add(new Vector3Int(x, y, zPositions[0] - 1));
                }
            }
        }
    }


    public List<Vector3Int> GetDirV3(Direction dir)
    {
        switch (dir)
        {
            case Direction.None:
                return new List<Vector3Int>();
            case Direction.Up:
                return upDir;
            case Direction.Down:
                return downDir;
            case Direction.Left:
                return leftDir;
            case Direction.Right:
                return rightDir;
            case Direction.Forward:
                return forwardDir;
            case Direction.Back:
                return backDir;
            default:
                return new List<Vector3Int>();
        }
    }


    /// <summary>
    /// 墙部分阻挡磁力斥力的移动
    /// </summary>
    /// <param name="checkPos"></param>
    /// <param name="moveV3"></param>
    /// <param name="dir"></param>
    /// <returns></returns>
    public Vector3 MoveRound(Vector3 checkPos, ref Vector3 moveV3, Direction dir)
    {
        //todo 有ref不用返回向量?
        float offset = 0f;
        switch (dir)
        {
            case Direction.Up:
            case Direction.Down:
                offset = Math.Abs(checkPos.y % 1);
                if (moveV3.y > 0)
                {
                    moveV3 = new Vector3(0, moveV3.y - offset, 0);
                }
                else if (moveV3.y < 0)
                {
                    moveV3 = new Vector3(0, moveV3.y + offset, 0);
                }
                break;
            case Direction.Left:
            case Direction.Right:
                offset = Math.Abs(checkPos.x % 1);
                if (moveV3.x > 0)
                {
                    moveV3 = new Vector3(moveV3.x - offset, 0, 0);
                }
                else if (moveV3.x < 0)
                {
                    moveV3 = new Vector3(moveV3.x + offset, 0, 0);
                }
                break;
            case Direction.Forward:
            case Direction.Back:
                offset = Math.Abs(checkPos.z % 1);
                if (moveV3.z > 0)
                {
                    moveV3 = new Vector3(0, 0, moveV3.z - offset);
                }
                else if (moveV3.z < 0)
                {
                    moveV3 = new Vector3(0, 0, moveV3.z + offset);
                }
                break;
            case Direction.None:
            default:
                return moveV3;
        }
        return moveV3;
    }

    /// <summary>
    /// 获得attachMoverList
    /// </summary>
    /// <param name="moversToPush"></param>
    /// <returns></returns>
    public List<List<Mover>> GetAttachMoverList(List<Mover> moversToPush)
    {
        bool isContain = false;
        List<List<Mover>> attachMovers = new List<List<Mover>>();
        foreach (Mover m in moversToPush)
        {
            List<Mover> attachMover = new List<Mover>();
            isContain = false;
            foreach (var moverList in attachMovers)
            {
                if (moverList.Contains(m))
                {
                    isContain = true;
                }
            }
            if (isContain)
                continue;
            attachMover = GetAttachMover(moversToPush, ref attachMover, m);
            attachMovers.Add(attachMover);

        }
        return attachMovers;
    }
    /// <summary>
    /// 查找所有attach的mover
    /// </summary>
    /// <param name="moversToPush"></param>
    /// <param name="attachMover"></param>
    /// <param name="mover"></param>
    /// <returns></returns>
    public List<Mover> GetAttachMover(List<Mover> moversToPush, ref List<Mover> attachMover, Mover mover)
    {
        attachMover.Add(mover);
        foreach (Transform t in mover.AttachBlock)
        {
            Mover attachedMover = t.GetComponent<Mover>();
            if (attachedMover != null && moversToPush.Contains(attachedMover) && !attachMover.Contains(attachedMover))
            {
                attachMover.Add(attachedMover);
                attachMover = GetAttachMover(moversToPush, ref attachMover, attachedMover);
            }
        }
        return attachMover;
    }

    public Vector3 CalPushMove(Vector3 moveV3)
    {
        return moveV3;
    }
}

