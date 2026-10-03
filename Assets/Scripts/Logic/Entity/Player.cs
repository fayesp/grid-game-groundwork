using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using DG.Tweening;
/// <summary>
/// 玩家控制类，继承自 Mover
/// </summary>
public class Player : Mover
{
    #region 变量定义
    // 一个场景只能有一个玩家实例
    public static Player instance { get; private set; }
    // 玩家当前的移动方向
    Vector3Int MoveV3 = Vector3Int.zero;
    // 上一次的水平输入值
    float prevHorInput = 0;
    // 上一次的垂直输入值
    float prevVerInput = 0;
    // 输入缓冲区，用于存储玩家的输入方向
    public List<Vector3Int> InputBuffer = new List<Vector3Int>();
    GameObject pivot;
    // 翻滚前的原始父物体，动画完成后恢复
    Transform originalParent;
    Vector3 rotationAxis = Vector3.zero;
    #endregion 变量定义

    #region 基础功能
    protected override void Start()
    {
        base.Start(); // Mover.Start: 收集 Tile 子物体，供 GroundBelow 等查询使用
        pivot = new GameObject("RollPivot");
        originalParent = transform.parent;
    }
    /// <summary>
    /// 初始化玩家实例
    /// </summary>
    void Awake()
    {
        instance = this;
    }
    /// <summary>
    /// 每帧更新，检测按键输入
    /// </summary>
    void Update()
    {
        // 如果没有按住撤销键，则缓冲输入
        if (!Game.instance.holdingUndo)
        {
            BufferInput();
        }
        // 如果可以输入，则检查缓冲区中的输入
        if (CanInput())
        {
            CheckBufferedInput();
        }
    }
    #endregion 基础功能

    #region 输入缓冲区
    /// <summary>
    /// 检查是否可以输入（没有按住撤销键且没有正在移动）
    /// </summary>
    /// <returns>是否可以输入</returns>
    public bool CanInput()
    {
        return !Game.instance.isMoving && !Game.instance.holdingUndo;
    }
    /// <summary>
    /// 清空输入缓冲区
    /// </summary>
    public void ClearInputBuffer()
    {
        InputBuffer.Clear();
        prevHorInput = 0;
        prevVerInput = 0;
        MoveV3 = Vector3Int.zero;
    }
    /// <summary>
    /// 缓冲玩家的输入
    /// </summary>
    public void BufferInput()
    {
        // 获取当前的水平和垂直输入值
        float newHor = Input.GetAxisRaw("Horizontal");
        float newVer = Input.GetAxisRaw("Vertical");
        // 判断是否需要缓冲输入
        bool shouldBufferInput =
            (newHor != prevHorInput || newVer != prevVerInput) && // 输入与上次不同
            !((newHor == 0 && newVer != prevVerInput) ||
            (newVer == 0 && newHor != prevHorInput)); // 输入变化不是因为松开按键
        Vector3Int dir = Vector3Int.zero;
        // 如果缓冲区为空，优先处理转向操作
        if (InputBuffer.Count == 0)
        {
            if (shouldBufferInput || CanInput())
            {
                dir = CalculateNewDirFromInput(MoveV3);
            }
        }
        else
        {
            // 如果缓冲区不为空，基于最后一个输入方向计算新方向
            if (shouldBufferInput)
            {
                dir = CalculateNewDirFromInput(InputBuffer.Last());
            }
        }
        // 如果计算出的方向有效，则添加到缓冲区
        if (dir != Vector3Int.zero)
        {
            InputBuffer.Add(dir);
        }
        // 更新上一次的输入值
        prevHorInput = newHor;
        prevVerInput = newVer;
    }
    /// <summary>
    /// 根据当前输入计算新的方向
    /// </summary>
    /// <param name="currentDir">当前方向</param>
    /// <returns>新的方向</returns>
    public Vector3Int CalculateNewDirFromInput(Vector3Int currentDir)
    {
        // 获取当前的水平和垂直输入值
        float hor = Input.GetAxisRaw("Horizontal");
        float ver = Input.GetAxisRaw("Vertical");
        // 如果没有输入，返回零向量
        if (hor == 0 && ver == 0)
        {
            return Vector3Int.zero;
        }
        // 如果同时有水平和垂直输入，优先保持当前方向
        if (hor != 0 && ver != 0)
        {
            if (currentDir == Vector3Int.right || currentDir == Vector3Int.left)
            {
                hor = 0;
            }
            else
            {
                ver = 0;
            }
        }
        // 根据输入值返回对应的方向
        if (hor == 1)
        {
            return Vector3Int.right;
        }
        else if (hor == -1)
        {
            return Vector3Int.left;
        }
        else if (ver == -1)
        {
            return Vector3Int.down;
        }
        else if (ver == 1)
        {
            return Vector3Int.up;
        }
        return Vector3Int.zero;
    }
    public void CalRollPivot(Vector3 Dir)
    {
        //若上次翻滚动画被中途打断（如撤销触发的 KillAll），玩家可能仍挂在 pivot 下，先脱离再计算
        if (transform.parent == pivot.transform)
        {
            transform.SetParent(originalParent);
        }
        //计算锚点:行进前缘的底边中点（游戏平面为 XY，Z 为深度）
        Vector3 side = (Dir.x != 0 || Dir.z != 0) ? Vector3.down : Vector3.left;
        pivot.transform.position = transform.position + Dir * 0.5f + side * 0.5f;
        //重置 pivot 旋转，避免 LocalAxisAdd 的局部轴被上次翻滚的累加旋转污染
        pivot.transform.rotation = Quaternion.identity;
        //计算旋转轴：水平/竖直移动绕 +Z（右/下 -90°，左/上 +90°），前后移动绕 X
        if (Dir.z != 0)
        {
            rotationAxis = Vector3.right * Dir.z;
        }
        else
        {
            rotationAxis = Vector3.forward * (Dir.y - Dir.x);
        }
    }

    public void OnRollPlayer(float rollDuration,Ease rotateEase)
    {
        transform.SetParent(pivot.transform);
        // 父级恢复必须等动画完成：立即恢复会让玩家脱离 pivot，翻滚动画彻底失效
        pivot.transform.DORotate(rotationAxis * 90f,rollDuration,RotateMode.LocalAxisAdd).SetEase(rotateEase).OnComplete(() =>
        {
            transform.SetParent(originalParent);
            Game.instance.MoveEnd();
        });
    }

    #endregion 输入缓冲区

    #region 执行移动
    /// <summary>
    /// 检查输入缓冲区并执行移动
    /// </summary>
    public void CheckBufferedInput()
    {
        // 如果缓冲区为空，直接返回
        if (InputBuffer.Count == 0)
        {
            return;
        }
        // 获取缓冲区中的第一个移动并移除
        MoveV3 = InputBuffer.First();
        InputBuffer.RemoveAt(0);
        Direction Dir = Utils.CheckDirection(MoveV3);
        // 尝试计划移动，如果成功则开始移动
        if (TryPlanMove(MoveV3, Dir))
        {
            Game.instance.MoveStart();
        }
    }
    #endregion 执行移动
}