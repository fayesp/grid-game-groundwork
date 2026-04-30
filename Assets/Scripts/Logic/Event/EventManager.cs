using System;
using UnityEngine;

/// <summary>
/// Legacy static event manager. Delegates to GameEventBus instance for
/// backward compatibility during migration.
///
/// TODO: Replace all usages with GameServices.Instance.Events directly,
/// then remove this class.
/// </summary>
public class EventManager
{
    public static Action<string> onLevelStarted
    {
        get => GameServices.Instance?.Events?.onLevelStarted;
        set
        {
            if (GameServices.Instance?.Events != null)
                GameServices.Instance.Events.onLevelStarted = value;
        }
    }

    public static Action<string> onLevelQuit
    {
        get => GameServices.Instance?.Events?.onLevelQuit;
        set
        {
            if (GameServices.Instance?.Events != null)
                GameServices.Instance.Events.onLevelQuit = value;
        }
    }

    public static Action<string> onLevelComplete
    {
        get => GameServices.Instance?.Events?.onLevelComplete;
        set
        {
            if (GameServices.Instance?.Events != null)
                GameServices.Instance.Events.onLevelComplete = value;
        }
    }

    public static Action<Vector3> onMoveStart
    {
        get => GameServices.Instance?.Events?.onMoveStart;
        set
        {
            if (GameServices.Instance?.Events != null)
                GameServices.Instance.Events.onMoveStart = value;
        }
    }

    public static Action onMoveComplete
    {
        get => GameServices.Instance?.Events?.onMoveComplete;
        set
        {
            if (GameServices.Instance?.Events != null)
                GameServices.Instance.Events.onMoveComplete = value;
        }
    }

    public static Action onPush
    {
        get => GameServices.Instance?.Events?.onPush;
        set
        {
            if (GameServices.Instance?.Events != null)
                GameServices.Instance.Events.onPush = value;
        }
    }

    public static Action onUndo
    {
        get => GameServices.Instance?.Events?.onUndo;
        set
        {
            if (GameServices.Instance?.Events != null)
                GameServices.Instance.Events.onUndo = value;
        }
    }

    public static Action onReset
    {
        get => GameServices.Instance?.Events?.onReset;
        set
        {
            if (GameServices.Instance?.Events != null)
                GameServices.Instance.Events.onReset = value;
        }
    }

    public static Action onUISelect
    {
        get => GameServices.Instance?.Events?.onUISelect;
        set
        {
            if (GameServices.Instance?.Events != null)
                GameServices.Instance.Events.onUISelect = value;
        }
    }

    public static Action onUISubmit
    {
        get => GameServices.Instance?.Events?.onUISubmit;
        set
        {
            if (GameServices.Instance?.Events != null)
                GameServices.Instance.Events.onUISubmit = value;
        }
    }
}
