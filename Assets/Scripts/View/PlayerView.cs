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
        //物理接地翻滚（ADR-0001）：支点取支撑面（+Z 侧半格）上行进前缘边的中点，
        //与 Player.CalRollPivot 保持同一份公式
        pivot.transform.position = transform.position + dir * 0.5f + Vector3.forward * 0.5f;
        //重置 pivot 旋转，避免 LocalAxisAdd 的局部轴被上次翻滚的累加旋转污染
        pivot.transform.rotation = Quaternion.identity;
        //旋转轴 = Cross(back, dir)：上下移动绕 X，左右移动绕 Y，无绕 Z 的翻滚；
        //dir 沿 Z 时轴退化为零向量，DORotate 不旋转但仍触发 OnComplete 收尾（安全降级，该分支实际不会发生）
        rotationAxis = Vector3.Cross(Vector3.back, dir);
    }
}
