using UnityEngine;

/// <summary>
/// Service Locator for the game. Single point of access for runtime services.
/// Replaces scattered singletons (Game, Player, LevelManager) with a single,
/// testable dependency container.
///
/// Usage: GameServices.Instance.Board, GameServices.Instance.Events, etc.
/// For tests, create a GameServices manually and inject mocks.
/// </summary>
public class GameServices : MonoBehaviour
{
    private static GameServices instanceRef;

    public static GameServices Instance
    {
        get
        {
            if (instanceRef == null)
            {
                instanceRef = FindObjectOfType<GameServices>();
            }
            return instanceRef;
        }
    }

    [Header("Services")]
    public GameEventBus Events = new GameEventBus();

    [Header("Presenter")]
    public GamePresenter Presenter;

    [Header("Settings")]
    public MovementSettings MovementSettings;

    void Awake()
    {
        if (instanceRef == null || instanceRef == this)
        {
            instanceRef = this;
        }
        else
        {
            Debug.LogError("Multiple GameServices instances detected. Only one is allowed per scene.");
        }
    }

    void OnDestroy()
    {
        if (instanceRef == this)
        {
            instanceRef = null;
        }
    }
}
