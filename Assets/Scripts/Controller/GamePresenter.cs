using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Presenter that coordinates the Model (GameBoard, CommandStack) and Views (MoverView, PlayerView).
/// Replaces Game.cs as the central orchestrator.
///
/// Responsibilities:
/// - Receives input from PlayerInputController
/// - Plans moves via MovePlanner
/// - Executes moves via CommandStack
/// - Animates views via DOTween
/// - Handles undo/redo
/// </summary>
public class GamePresenter : MonoBehaviour
{
    #region Services & Model

    [Header("Services")]
    [SerializeField]
    private PlayerInputController inputController;

    [SerializeField]
    private PlayerView playerView;

    private GameBoard board;
    private CommandStack commandStack;
    private GameEventBus events;
    private MovementSettings settings;
    private MovePlanner movePlanner;

    #endregion

    #region State

    private List<MoverView> moverViews = new List<MoverView>();
    private bool isMoving;
    private bool blockInput;
    private int animatingCount;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        EnsureServices();
        board = new GameBoard();
        commandStack = new CommandStack();
        events = GameServices.Instance.Events;
        settings = GameServices.Instance.MovementSettings ?? ScriptableObject.CreateInstance<MovementSettings>();
        movePlanner = new MovePlanner(new GameBoardGridQuery(board), settings.allowPushMulti);
    }

    void Start()
    {
        DiscoverSceneEntities();

        if (inputController != null)
        {
            inputController.OnMoveRequested += HandleMoveRequested;
            inputController.OnUndoRequested += HandleUndoRequested;
            inputController.OnResetRequested += HandleResetRequested;
        }
    }

    void OnDestroy()
    {
        if (inputController != null)
        {
            inputController.OnMoveRequested -= HandleMoveRequested;
            inputController.OnUndoRequested -= HandleUndoRequested;
            inputController.OnResetRequested -= HandleResetRequested;
        }
    }

    #endregion

    #region Initialization

    void EnsureServices()
    {
        if (GameServices.Instance == null)
        {
            GameObject servicesGo = new GameObject("GameServices");
            servicesGo.AddComponent<GameServices>();
        }
    }

    void DiscoverSceneEntities()
    {
        var movers = FindObjectsOfType<Mover>();
        var walls = FindObjectsOfType<Wall>();

        foreach (var mover in movers)
        {
            if (mover == null) continue;

            string id = mover.GetInstanceID().ToString();
            var entity = new GridEntity(id, "Mover")
            {
                Position = mover.transform.position,
                Rotation = mover.transform.eulerAngles,
                IsPlayer = mover.isPlayer,
                IsStatic = false
            };
            board.RegisterEntity(entity);

            // Link or create MoverView
            var view = mover.GetComponent<MoverView>();
            if (view == null)
                view = mover.gameObject.AddComponent<MoverView>();

            view.Model = entity;
            moverViews.Add(view);

            // Link PlayerView if this is the player
            if (mover.isPlayer && playerView == null)
            {
                playerView = mover.GetComponent<PlayerView>();
                if (playerView == null)
                    playerView = mover.gameObject.AddComponent<PlayerView>();
            }
        }

        foreach (var wall in walls)
        {
            if (wall == null) continue;

            string id = "wall_" + wall.GetInstanceID().ToString();
            var entity = new GridEntity(id, "Wall")
            {
                Position = wall.transform.position,
                Rotation = wall.transform.eulerAngles,
                IsStatic = true
            };
            board.RegisterEntity(entity);
        }
    }

    #endregion

    #region Input Handlers

    void HandleMoveRequested(Vector3Int direction)
    {
        if (blockInput || isMoving)
            return;

        var playerEntity = board.GetPlayer();
        if (playerEntity == null)
            return;

        var playerModel = new PlayerModel(playerEntity);
        Direction dir = DirectionUtils.CheckDirection(direction);

        if (movePlanner.TryPlanMove(playerModel, direction, dir, out var plannedMove))
        {
            ExecutePlannedMove(plannedMove, false);
        }
    }

    void HandleUndoRequested()
    {
        if (isMoving)
            return;

        if (commandStack.UndoCount > 0)
        {
            commandStack.Undo();
            SyncViewsToBoard();
            events.onUndo?.Invoke();
        }
    }

    void HandleResetRequested()
    {
        if (isMoving)
            return;

        DOTween.KillAll();
        commandStack.ResetToBeginning();
        SyncViewsToBoard();
        events.onReset?.Invoke();
    }

    #endregion

    #region Move Execution

    void ExecutePlannedMove(PlannedMoveResult plannedMove, bool isFalling)
    {
        if (plannedMove == null || plannedMove.IsEmpty)
        {
            OnMoveCycleComplete();
            return;
        }

        isMoving = true;
        blockInput = true;
        if (inputController != null)
            inputController.IsBlocked = true;

        // Execute command on model
        var command = new MoveCommand(board, plannedMove);
        commandStack.Execute(command);

        // Animate views
        float duration = isFalling
            ? settings.fallTime
            : settings.moveTime;

        animatingCount = 0;
        foreach (var pair in plannedMove.EntityDeltas)
        {
            if (pair.Value == Vector3.zero)
                continue;

            var view = FindViewForEntity(pair.Key);
            if (view == null)
                continue;

            Vector3 from = view.transform.position;
            Vector3 to = from + pair.Value;

            animatingCount++;
            view.SetAnimating(true);

            bool isPlayer = board.GetEntity(pair.Key)?.IsPlayer ?? false;
            if (isPlayer && playerView != null)
            {
                playerView.AnimateRoll(pair.Value.normalized, settings.rotateTime, settings.rotateEase, OnAnimationComplete);
            }
            else
            {
                view.transform.DOMove(to, duration)
                    .SetEase(settings.moveEase)
                    .OnComplete(OnAnimationComplete);
            }
        }

        if (animatingCount == 0)
        {
            OnMoveCycleComplete();
        }
    }

    void OnAnimationComplete()
    {
        animatingCount--;
        if (animatingCount <= 0)
        {
            OnMoveCycleComplete();
        }
    }

    void OnMoveCycleComplete()
    {
        isMoving = false;
        blockInput = false;
        if (inputController != null)
            inputController.IsBlocked = false;

        // Snap all views to model
        SyncViewsToBoard();

        // Check for falling
        var fallResult = new PlannedMoveResult();
        foreach (var entity in board.GetMovers())
        {
            if (!board.GroundBelow(entity))
            {
                fallResult.EntityDeltas[entity.Id] = new Vector3(0, 0, 1); // forward = +Z
            }
        }

        if (!fallResult.IsEmpty)
        {
            ExecutePlannedMove(fallResult, true);
        }
        else
        {
            events.onMoveComplete?.Invoke();
        }
    }

    #endregion

    #region View Synchronization

    MoverView FindViewForEntity(string entityId)
    {
        foreach (var view in moverViews)
        {
            if (view.EntityId == entityId)
                return view;
        }
        return null;
    }

    void SyncViewsToBoard()
    {
        foreach (var view in moverViews)
        {
            if (view != null)
            {
                view.SetAnimating(false);
                view.SyncToModel();
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Refreshes the presenter state. Clears input and stops animations.
    /// </summary>
    public void Refresh()
    {
        DOTween.KillAll();
        inputController?.ClearBuffer();
        SyncViewsToBoard();
    }

    /// <summary>
    /// Re-discovers scene entities and rebuilds the board.
    /// Used by the level editor after changes.
    /// </summary>
    public void SyncFromScene()
    {
        board.Clear();
        moverViews.Clear();
        DiscoverSceneEntities();
    }

    #endregion
}
