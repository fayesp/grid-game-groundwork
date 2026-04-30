using UnityEngine;

/// <summary>
/// Pure C# model for the player entity.
/// Extends MoverModel with player-specific data.
/// Input handling remains in the View layer (PlayerInputController).
/// </summary>
public class PlayerModel : MoverModel
{
    public PlayerModel(GridEntity entity) : base(entity)
    {
        entity.IsPlayer = true;
    }
}
