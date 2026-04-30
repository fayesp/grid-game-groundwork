using UnityEngine;
using DG.Tweening;

/// <summary>
/// ScriptableObject that centralizes all movement and animation timing parameters.
/// Allows tuning without modifying code.
/// </summary>
[CreateAssetMenu(fileName = "MovementSettings", menuName = "Game/Movement Settings")]
public class MovementSettings : ScriptableObject
{
    [Header("Movement Timing")]
    [Tooltip("Time to move 1 unit horizontally/vertically")]
    public float moveTime = 0.18f;

    [Tooltip("Time to rotate/roll 90 degrees")]
    public float rotateTime = 0.18f;

    [Tooltip("Time to fall 1 unit")]
    public float fallTime = 0.1f;

    [Tooltip("Factor by which to speed up movement when input is buffered")]
    public float moveBufferSpeedupFactor = 0.5f;

    [Header("Animation Easing")]
    public Ease moveEase = Ease.Linear;
    public Ease rotateEase = Ease.OutCubic;

    [Header("Game Rules")]
    [Tooltip("If true, any mover can push other movers. If false, only the player can push.")]
    public bool allowPushMulti = true;
}
