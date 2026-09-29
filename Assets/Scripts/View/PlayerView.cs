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
        //游戏平面为 XY（Z 为深度），支点取行进前缘的底边中点
        Vector3 side = (dir.x != 0 || dir.z != 0) ? Vector3.down : Vector3.left;
        pivot.transform.position = transform.position + dir * 0.5f + side * 0.5f;
        //重置 pivot 旋转，避免 LocalAxisAdd 的局部轴被上次翻滚的累加旋转污染
        pivot.transform.rotation = Quaternion.identity;
        //旋转轴：水平/竖直移动绕 +Z（右/下 -90°，左/上 +90°），前后移动绕 X
        if (dir.z != 0)
        {
            rotationAxis = Vector3.right * dir.z;
        }
        else
        {
            rotationAxis = Vector3.forward * (dir.y - dir.x);
        }
    }
}
