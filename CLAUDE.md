# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 2019.x LTS framework for creating grid-based/block-pushing/Sokoban-like puzzle games. The project provides a complete foundation including level editor, undo/redo system, and animation handling via DOTween.

**Key Dependency**: DOTween by Demigiant (installed in Assets/Plugins/Demigiant/)

## Coordinate System Convention

**Important**: This project uses a non-standard Unity coordinate system:
- **X, Y**: Horizontal plane (left/right, up/down in 2D view)
- **Z**: Vertical axis (forward = down, back = up)

This is opposite to Unity's default where Y is typically the vertical up axis. When working with coordinates:
- `Utils.forward` = `(0, 0, 1)` (downward)
- `Utils.back` = `(0, 0, -1)` (upward)

## Core Architecture

### Main Singleton

**Game.cs** - The main game controller singleton (`Game.instance`). Manages:
- LogicalGrid for spatial tracking
- List of all Mover and Wall objects
- Movement animation timing (moveTime, rotateTime, fallTime)
- Input blocking and undo/redo coordination
- Move planning and execution cycle (`MoveStart()`)

### Movement System

**Mover.cs** - Base class for all movable objects. Key concepts:
- Tiles: Child gameObjects tagged "Tile" with BoxCollider define the shape
- PlannedMove: Thread-safe property for queued movement during a cycle
- CanMoveToward(): Checks walls, other movers, and validates multi-tile blocks
- PlanPushes(): Recursively plans pushes for connected movers
- DoPostMoveEffects(): Handles falling after movement

**Player.cs** - Extends Mover, handles input:
- Input buffering system for smooth multi-move input
- Rolling animation using pivot transform
- Single instance per scene enforced

**State.cs** - Undo/redo system:
- Tracks position and rotation of all movers
- Stack-based state management with undoIndex
- Z key triggers undo, R key triggers reset

### Grid System

**LogicalGrid.cs** - Spatial indexing:
- `Dictionary<Vector3, HashSet<GameObject>>` maps integer grid positions to objects
- `SyncContents()` rebuilds the grid from all "Tile" tagged objects
- `GetContentsAt()` returns objects at a grid position
- Handles multi-tile objects that span multiple grid cells

### Magnet System (New Feature)

**Magnet.cs** - Extends Mover, adds magnetic behavior:
- AttachBlock list tracks connected magnets
- MagnetType enum defines polarization (XP, XN, YP, YN, ZP, ZN)
- MagnetField components on each magnet handle physics calculations

**MagnetField.cs** - Magnetic field physics:
- Complex polar type system (S, N, XPS, XPN, YPS, YPN, ZPS, ZPN)
- Attraction/repulsion calculations based on relative positions
- Uses OnTriggerEnter/Exit for field detection

## Level System

**Level Structure**:
- Levels are stored as JSON files in `Assets/Resources/Levels/`
- Each level has a parent GameObject tagged "Level"
- LevelManager component tracks current level name

**LevelEditor** (Window -> Level Editor):
- Paint prefabs by left-clicking in Scene view
- Hold Alt + scroll to change spawn height
- Right-click to select existing prefab
- Rotate, invert, and save/load levels from editor window
- Prefab list defined in `Assets/leveleditorprefabs.txt`

## Tag System

Required tags (auto-created by TagHelper):
- `"Tile"` - Child objects with BoxCollider, defines mover/wall shape
- `"Player"` - The single player instance
- `"Level"` - Parent object for each level
- `"Magnet"` - Magnet objects

## Prefab Structure

**Mover Prefabs** (e.g., Crate, Player):
- Parent GameObject with Mover (or Player/Magnet) component
- Child GameObjects tagged "Tile" with BoxCollider components
- Multi-tile objects have multiple Tile children

**Wall Prefabs**:
- Parent GameObject with Wall component
- Child GameObjects tagged "Tile" with BoxCollider

## Editor Workflow

1. Open Scene
2. Add "GameController" prefab (found in ProjectSettings or create from Game prefab)
3. Open Level Editor (Window -> Level Editor)
4. Select prefab from dropdown
5. Paint in Scene view (left-click, hold to paint continuously)
6. Save level with "Save Level As"

## Important Utility Functions

**Utils.cs** contains static helpers:
- `GetMoverAtPos(pos)` / `GetWallAtPos(pos)` - Query grid contents
- `TileIsEmpty(pos)` - Check if position is free
- `GroundBelow(mover)` - Check if mover has ground support
- `IsRound(vector3, Direction)` - Check if position aligns with grid in direction
- `AvoidIntersect(Transform)` - Prevent object overlap during placement

**Tile.cs** - Simple struct referencing a child transform and its position

## Common Patterns

**Planning Movement**:
```csharp
// 1. Check if move is valid
if (!CanMoveToward(ref MoveV3, Dir))
    return false;

// 2. Plan the move
PlannedMove = MoveV3;

// 3. Plan pushes for other movers
PlanPushes(MoveV3, Dir);
```

**Multi-tile Objects**: Update `UpdateDirPos()` and get direction lists (`rightDir`, `leftDir`, etc.) before collision checks to handle objects spanning multiple grid cells.

**Attached Movers**: Use `AttachBlock` list for magnets and other objects that move together.
