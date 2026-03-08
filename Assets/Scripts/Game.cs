using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using Assets.Scripts;
/// <summary>
/// 管理游戏对象、同步网格、关卡管理器
/// </summary>
public class Game : MonoBehaviour
{
    #region 单例模式
    private static Game instanceRef;
    /// <summary>
    /// 单例访问器，确保场景中只有一个 Game 实例
    /// </summary>
    public static Game instance
    {
        get
        {
            if (instanceRef == null)
            {
                instanceRef = FindObjectOfType<Game>();
            }
            return instanceRef;
        }
    }
    #endregion 单例模式
    #region 公共变量
    public LogicalGrid Grid = new LogicalGrid(); // 游戏逻辑网格
    public static List<Mover> movers = new List<Mover>(); // 所有可移动对象
    public static List<Wall> walls = new List<Wall>(); // 所有墙体对象
    public float moveTime = 0.18f; // 移动 1 单位所需时间
    public float rotateTime = 0.18f; //翻滚90度时间
    public float fallTime = 0.1f; // 下落 1 单位所需时间
    public float moveBufferSpeedupFactor = 0.5f; // 连续输入加速因子
    public Ease moveEase = Ease.Linear; // 动画效果，默认线性匀速运动
    public Ease rotateEase = Ease.OutCubic; // 动画效果,翻滚回弹?
    private int movingCount = 0; // 当前正在移动的对象计数
    private List<List<MoverPos>> PlannedMoves = new List<List<MoverPos>>(); // 计划的移动列表
    public bool holdingUndo { get; private set; } = false; // 是否正在长按撤销键
    public static bool allowPushMulti = true; // 是否可以推动多个箱子
    public bool blockInput = false; // 是否阻止输入
    public bool isMoving { get { return movingCount > 0; } } // 是否有对象正在移动
    #endregion 公共变量
    #region Unity 生命周期方法
    /// <summary>
    /// 初始化游戏实例，设置帧率，加载存档
    /// </summary>
    void Awake()
    {
        if (instanceRef == null || instanceRef == this)
        {
            instanceRef = this;
            Application.targetFrameRate = 60; // 设置目标帧率为 60
            if (Application.isEditor && !SaveData.initialized) // 如果在编辑器中且存档未初始化
            {
                SaveData.LoadGame(1); // 加载存档
                SyncGrid(); // 同步网格
            }
        }
        else
        {
            MyLogger.Instance.WriteError("场景中存在多个 Game 实例"); // 报错提示多个实例
        }
    }
    /// <summary>
    /// 启动时初始化引用和状态
    /// </summary>
    void Start()
    {
        StartCoroutine(InitAfterFrame()); // 延迟一帧后初始化
        //MyLogger.Instance.WriteError("场景中存在多个 Game 实例，请确保只有一个 Game 对象。");
    }
    /// <summary>
    /// 延迟一帧后初始化
    /// </summary>
    IEnumerator InitAfterFrame()
    {
        blockInput = true; // 阻止输入
        yield return WaitFor.EndOfFrame; // 等待一帧
        SetReferences(); // 设置对象引用
        State.Init(); // 初始化游戏状态
        foreach (Mover mover in movers)
        {
            State.AddMover(mover); // 将所有 Mover 添加到状态管理
        }
        State.AddToUndoStack(); // 添加初始状态到撤销栈
        blockInput = false; // 解除输入阻止
    }
    /// <summary>
    /// 设置对象引用
    /// </summary>
    void SetReferences()
    {
        movers.Clear(); // 清空 Mover 列表
        walls.Clear(); // 清空 Wall 列表
        movers = FindObjectsOfType<Mover>().ToList(); // 查找所有 Mover 对象
        walls = FindObjectsOfType<Wall>().ToList(); // 查找所有 Wall 对象
        SyncGrid(); // 同步网格
    }
    /// <summary>
    /// 每帧检测撤销和重置按键
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z)) // 检测撤销键按下
        {
            holdingUndo = true; // 标记长按撤销
            DoUndo(); // 执行撤销操作
            DOVirtual.DelayedCall(0.75f, UndoRepeat); // 延迟调用长按撤销逻辑
        }
        else if (Input.GetKeyDown(KeyCode.R)) // 检测重置键按下
        {
            DoReset(); // 执行重置操作
        }
        if (Input.GetKeyUp(KeyCode.Z)) // 检测撤销键松开
        {
            StartCoroutine(StopUndoing()); // 停止撤销操作
        }
    }
    #endregion Unity 生命周期方法
    #region 网格同步与刷新
    /// <summary>
    /// 刷新游戏状态
    /// </summary>
    public void Refresh()
    {
        movingCount = 0; // 重置移动计数
        Player.instance.ClearInputBuffer(); // 清空玩家输入缓冲
        PlannedMoves.Clear(); // 清空计划的移动
        foreach (var mover in movers)
            mover.Stop(); // 停止所有 Mover 的移动
        SyncGrid(); // 同步网格
    }
    /// <summary>
    /// 编辑器模式下刷新
    /// </summary>
    public void EditorRefresh()
    {
        SetReferences(); // 重新设置引用
    }
    /// <summary>
    /// 同步网格内容
    /// </summary>
    public void SyncGrid()
    {
        var tiles = GameObject.FindGameObjectsWithTag("Tile"); // 查找所有带有 "Tile" 标签的对象
        Grid.SyncContents(tiles); // 同步网格内容
    }
    #endregion 网格同步与刷新
    #region 重置功能
    /// <summary>
    /// 重置游戏状态
    /// </summary>
    void DoReset()
    {
        DOTween.KillAll(); // 停止所有动画
        State.DoReset(); // 重置游戏状态
        Refresh(); // 刷新游戏状态
        EventManager.onReset?.Invoke(); // 触发重置事件
    }
    #endregion 重置功能
    #region 撤销功能
    /// <summary>
    /// 执行撤销操作
    /// </summary>
    void DoUndo()
    {
        if (State.undoIndex <= 0) // 如果没有可撤销的操作
            return;
        DOTween.KillAll(); // 停止所有动画
        if (isMoving) // 如果有对象正在移动
        {
            CompleteMove(); // 完成当前移动
        }
        State.DoUndo(); // 执行撤销操作
        Refresh(); // 刷新游戏状态
        EventManager.onUndo?.Invoke(); // 触发撤销事件
    }
    /// <summary>
    /// 长按撤销操作
    /// </summary>
    void UndoRepeat()
    {
        if (Input.GetKey(KeyCode.Z) && holdingUndo) // 如果撤销键仍被按住
        {
            DoUndo(); // 执行撤销操作
            DOVirtual.DelayedCall(0.075f, UndoRepeat); // 延迟调用自身
        }
    }
    /// <summary>
    /// 停止撤销操作
    /// </summary>
    IEnumerator StopUndoing()
    {
        yield return WaitFor.EndOfFrame; // 等待一帧
        holdingUndo = false; // 停止长按撤销
    }
    #endregion 撤销功能
    #region 获取位置
    /// <summary>
    /// Mover 的位置结构体
    /// </summary>
    private struct MoverPos
    {
        public Mover m; // Mover 对象
        public Vector3 Pos; // Mover 的位置
        public MoverPos(Mover mov)
        {
            m = mov;
            Pos = mov.Pos(); // 获取 Mover 的当前位置
        }
    };
    /// <summary>
    /// 获取所有 Mover 的当前位置
    /// </summary>
    private List<MoverPos> GetMoversPositions()
    {
        var MoverPosList = new List<MoverPos>();
        foreach (var mover in movers)
        {
            if (mover != null)
            {
                MoverPosList.Add(new MoverPos(mover)); // 添加 Mover 的位置
            }
        }
        return MoverPosList;
    }
    /// <summary>
    /// 获取玩家当前位置
    /// </summary>
    public Vector3 GetPlayerPosition()
    {
        if (PlannedMoves.Count > 0) // 如果有计划的移动
        {
            foreach (var move in PlannedMoves[0])
            {
                return move.Pos; // 返回计划的第一个位置
            }
        }
        return Player.instance.transform.position; // 返回玩家当前的位置
    }
    #endregion 获取位置
    #region 移动逻辑
    /// <summary>
    /// 开始移动逻辑
    /// </summary>
    public void MoveStart(bool doPostMoveEffects = true)
    {
        PlannedMoves.Clear(); // 清空计划的移动
        //todo 有下落的话会执行多次
        for (int i = 0; i < 999 && movers.Any(m => m.HasPlannedMove()); ++i) // 循环处理所有计划的移动,
        {
            //todo 更新物体的位置
            PlannedMoves.Add(GetMoversPositions()); // 记录当前 Mover 的位置
            bool isPushing = false; // 是否有推动行为
            var moved = false;
            foreach (var mover in movers)
            {
                if (mover.ExecuteLogicalMove()) // 执行逻辑移动
                {
                    if (!mover.isPlayer) // 如果不是玩家
                        isPushing = true; // 标记为推动行为
                    moved = true;
                }
            }
            if (moved) // 如果有移动发生
            {
                if (isPushing)
                    EventManager.onPush?.Invoke(); // 触发推动事件
                SyncGrid(); // 同步网格
            }
            if (doPostMoveEffects) // 如果需要执行后续效果
            {
                foreach (var mover in movers)
                    mover.DoPostMoveEffects(); // 执行后续效果
            }
        }
        PlannedMoves.Add(GetMoversPositions()); // 添加最终位置
        StartMoveCycle(false); // 开始移动动画
    }
    /// <summary>
    /// 开始移动动画
    /// </summary>
    private void StartMoveCycle(bool falling)
    {
        foreach (var move in PlannedMoves[0]) // 设置所有 Mover 的位置为老的位置
            move.m.transform.position = move.Pos;
        PlannedMoves.RemoveAt(0); // 移除已处理的计划
        if (PlannedMoves.Count == 0) // 如果没有更多计划
        {
            CompleteMove(); // 完成移动
            return;
        }
        if (falling) // 如果是下落
        { Player.instance.ClearInputBuffer(); } // 清空输入缓冲
        float dur = falling ? fallTime : moveTime / (Player.instance.InputBuffer.Count() * moveBufferSpeedupFactor + 1); // 计算动画持续时间
        foreach (var move in PlannedMoves[0])
        {
            //move.Pos是新的坐标,move.m.Pos()是老的坐标
            if (move.Pos == move.m.Pos()) // 如果位置未改变
                continue;
            ++movingCount; // 增加移动计数
            if (move.m.CompareTag("Player"))
            {
                //todo player的翻滚处理,调用rollcube的方法?
                Player.instance.OnRollPlayer(rotateTime,rotateEase);
            }
            else
            {
                move.m.transform.DOMove(move.Pos, dur).OnComplete(MoveEnd).SetEase(moveEase); // 执行移动动画
            }
        }
    }
    /// <summary>
    /// 移动结束
    /// </summary>
    public void MoveEnd()
    {
        movingCount--; // 减少移动计数
        //由于动画的动作时间比循环的时间长很多,所以可以在movingCount为0时开始下落动画,而不会在动画还没有完成的时候直接开始下落的循环
        if (movingCount == 0) // 如果没有正在移动的对象
        {
            StartMoveCycle(true); // 开始下落动画
        }
    }
    #endregion 移动逻辑
    #region 移动完成
    /// <summary>
    /// 移动完成后的处理
    /// </summary>
    public void CompleteMove()
    {
        State.OnMoveComplete(); // 通知状态管理移动完成
        EventManager.onMoveComplete?.Invoke(); // 触发移动完成事件
    }
    #endregion 移动完成
}