# Grid Game Groundwork

A Unity framework for grid-based block-pushing (Sokoban-like) games. The codebase is mid-migration between two architectures; this glossary fixes which term means which.

## Language

**Mover**:
A grid object that can be pushed, falls, and is tracked for undo. The canonical implementation is the legacy `Mover` MonoBehaviour base class in `Logic/Entity/`; custom game objects derive from it.
_Avoid_: MoverModel

**MoverModel**:
The pure C# model counterpart of a Mover in the new architecture (in migration). Not the same thing as a Mover — a Mover is a scene object, a MoverModel is data.
_Avoid_: Mover

**Wall**:
A static grid object that blocks movement, including the ground.

**Player**:
The single Mover driven by user input. One per scene.

**翻滚 (Roll)**:
The movement animation convention for the player (and movers): a physical tip-over around the leading edge of the support face (+Z side) — up/down rolls around the X axis, left/right around the Y axis; there is no roll around Z. See ADR-0001.
_Avoid_: screen-plane roll（屏幕面内翻滚）

**Tile**:
One occupied grid cell — a transform reference plus a grid position (the `Tile` struct). Also the name of the Unity tag that child Box Colliders of Walls and Movers must carry.

**Game**:
The legacy scene orchestrator singleton: movement execution, undo/reset, and the collision grid. This is what runs in every scene today.
_Avoid_: GamePresenter

**GamePresenter**:
The new-stack orchestrator that replaces Game. Exists in code but is not wired into any scene yet.
_Avoid_: Game

**State**:
The legacy static undo stack, recording mover positions per move.
_Avoid_: CommandStack

**CommandStack**:
The new-stack undo/redo stack of `ICommand`s (in migration).
_Avoid_: State

**LogicalGrid**:
The legacy spatial hash from grid positions to GameObjects.
_Avoid_: SpatialGrid, GameBoard

**SpatialGrid / GameBoard**:
The new-stack pure C# spatial index and entity registry (in migration).
_Avoid_: LogicalGrid

**Level**:
A serialized JSON level file in `Assets/Resources/Levels/`, loaded and unloaded by the LevelManager.
