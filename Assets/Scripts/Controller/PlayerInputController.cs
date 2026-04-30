using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Collects player input and translates it into game commands.
/// Emits events that are consumed by GamePresenter.
/// No direct references to Game, Player, or movement logic.
/// </summary>
public class PlayerInputController : MonoBehaviour
{
    public System.Action<Vector3Int> OnMoveRequested;
    public System.Action OnUndoRequested;
    public System.Action OnResetRequested;

    [SerializeField]
    private bool blockInput = false;

    // Input buffering
    private Vector3Int lastMoveDirection = Vector3Int.zero;
    private float prevHorInput = 0;
    private float prevVerInput = 0;
    private List<Vector3Int> inputBuffer = new List<Vector3Int>();

    public bool IsBlocked { get => blockInput; set => blockInput = value; }
    public IReadOnlyList<Vector3Int> InputBuffer => inputBuffer;

    void Update()
    {
        if (blockInput)
            return;

        // Undo / Reset keys
        if (Input.GetKeyDown(KeyCode.Z))
        {
            OnUndoRequested?.Invoke();
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            OnResetRequested?.Invoke();
            return;
        }

        // Movement input buffering
        BufferInput();

        // Emit buffered moves
        if (inputBuffer.Count > 0)
        {
            Vector3Int dir = inputBuffer[0];
            inputBuffer.RemoveAt(0);
            OnMoveRequested?.Invoke(dir);
        }
    }

    void BufferInput()
    {
        float newHor = Input.GetAxisRaw("Horizontal");
        float newVer = Input.GetAxisRaw("Vertical");

        bool shouldBufferInput =
            (newHor != prevHorInput || newVer != prevVerInput) &&
            !((newHor == 0 && newVer != prevVerInput) ||
              (newVer == 0 && newHor != prevHorInput));

        Vector3Int dir = Vector3Int.zero;

        if (inputBuffer.Count == 0)
        {
            if (shouldBufferInput || lastMoveDirection == Vector3Int.zero)
            {
                dir = CalculateDirectionFromInput(lastMoveDirection);
            }
        }
        else
        {
            if (shouldBufferInput)
            {
                dir = CalculateDirectionFromInput(inputBuffer.Last());
            }
        }

        if (dir != Vector3Int.zero)
        {
            inputBuffer.Add(dir);
            lastMoveDirection = dir;
        }

        prevHorInput = newHor;
        prevVerInput = newVer;
    }

    Vector3Int CalculateDirectionFromInput(Vector3Int currentDir)
    {
        float hor = Input.GetAxisRaw("Horizontal");
        float ver = Input.GetAxisRaw("Vertical");

        if (hor == 0 && ver == 0)
            return Vector3Int.zero;

        if (hor != 0 && ver != 0)
        {
            if (currentDir == Vector3Int.right || currentDir == Vector3Int.left)
                hor = 0;
            else
                ver = 0;
        }

        if (hor == 1) return Vector3Int.right;
        if (hor == -1) return Vector3Int.left;
        if (ver == -1) return Vector3Int.down;
        if (ver == 1) return Vector3Int.up;

        return Vector3Int.zero;
    }

    public void ClearBuffer()
    {
        inputBuffer.Clear();
        prevHorInput = 0;
        prevVerInput = 0;
        lastMoveDirection = Vector3Int.zero;
    }
}
