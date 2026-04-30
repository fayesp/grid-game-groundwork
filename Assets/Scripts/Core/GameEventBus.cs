using System;
using UnityEngine;

/// <summary>
/// Instance-based event bus that replaces the static EventManager.
/// Provides type-safe, decoupled communication between layers.
/// </summary>
public class GameEventBus
{
    public Action<string> onLevelStarted;
    public Action<string> onLevelQuit;
    public Action<string> onLevelComplete;
    public Action<Vector3> onMoveStart;
    public Action onMoveComplete;
    public Action onPush;
    public Action onUndo;
    public Action onReset;
    public Action onUISelect;
    public Action onUISubmit;

    /// <summary>
    /// Clears all event subscriptions. Call when resetting game state
    /// to prevent stale references.
    /// </summary>
    public void Clear()
    {
        onLevelStarted = null;
        onLevelQuit = null;
        onLevelComplete = null;
        onMoveStart = null;
        onMoveComplete = null;
        onPush = null;
        onUndo = null;
        onReset = null;
        onUISelect = null;
        onUISubmit = null;
    }
}
