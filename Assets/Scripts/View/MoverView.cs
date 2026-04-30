using UnityEngine;

/// <summary>
/// View component that synchronizes a MonoBehaviour's transform to a GridEntity model.
/// Responsible for visual presentation only. All game logic lives in the Model layer.
///
/// TODO: Add DOTween animation support in Phase 5.
/// </summary>
public class MoverView : MonoBehaviour
{
    [SerializeField]
    private string entityId;

    private GridEntity model;
    private bool isAnimating;

    /// <summary>
    /// The model this view represents. Set during initialization.
    /// </summary>
    public GridEntity Model
    {
        get => model;
        set
        {
            model = value;
            if (model != null)
                entityId = model.Id;
        }
    }

    /// <summary>
    /// Unique identifier linking this view to its model.
    /// </summary>
    public string EntityId => entityId;

    void Update()
    {
        if (model == null || isAnimating)
            return;

        // Direct sync when not animating.
        // During animation, the view temporarily detaches from the model
        // and interpolates visually.
        SyncToModel();
    }

    /// <summary>
    /// Snaps the transform to the model's current position and rotation.
    /// </summary>
    public void SyncToModel()
    {
        if (model == null)
            return;

        transform.position = model.Position;
        transform.eulerAngles = model.Rotation;
    }

    /// <summary>
    /// Temporarily detaches the view from model updates for animation.
    /// Call SnapToModel() or set isAnimating = false when animation completes.
    /// </summary>
    public void SetAnimating(bool animating)
    {
        isAnimating = animating;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        // Draw a wire cube at the object's position for editor visibility.
        Gizmos.DrawWireCube(transform.position, Vector3.one);
    }
#endif
}
