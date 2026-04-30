using DG.Tweening;
using UnityEngine;

/// <summary>
/// View component for the player. Handles roll animation via DOTween.
/// All input and game logic lives in PlayerInputController and GamePresenter.
/// </summary>
[RequireComponent(typeof(MoverView))]
public class PlayerView : MonoBehaviour
{
    [SerializeField]
    private MoverView moverView;

    private GameObject pivot;
    private GameObject parent;
    private Vector3 rotationAxis = Vector3.zero;

    void Start()
    {
        if (moverView == null)
            moverView = GetComponent<MoverView>();

        pivot = new GameObject("RollPivot");
        parent = new GameObject("PlayerParent");
        parent.transform.SetParent(transform.parent);
    }

    void OnDestroy()
    {
        if (pivot != null)
            Destroy(pivot);
        if (parent != null)
            Destroy(parent);
    }

    /// <summary>
    /// Initiates the roll animation for the player.
    /// </summary>
    public void AnimateRoll(Vector3 direction, float duration, Ease ease, System.Action onComplete)
    {
        CalculateRollPivot(direction);

        transform.SetParent(pivot.transform);
        pivot.transform.DORotate(rotationAxis * 90f, duration, RotateMode.LocalAxisAdd)
            .SetEase(ease)
            .OnComplete(() =>
            {
                transform.SetParent(parent.transform);
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// Snaps the player to the model position, killing any active roll animation.
    /// </summary>
    public void SnapToModel()
    {
        if (pivot != null)
            pivot.transform.rotation = Quaternion.identity;

        DOTween.Kill(pivot?.transform);
        moverView?.SyncToModel();
    }

    void CalculateRollPivot(Vector3 dir)
    {
        pivot.transform.position = transform.position + Vector3.forward * 0.5f + dir * 0.5f;
        rotationAxis = Vector3.Cross(Vector3.back, dir).normalized;
    }
}
