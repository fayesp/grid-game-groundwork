# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Grid Game Groundwork is a Unity project and level editor for making grid-based/block-pushing/Sokoban-like games. Built with Unity 2019.4.10f1 LTS.

**Required Dependency**: DOTween by Demigiant (http://dotween.demigiant.com/)

## Unity Development

Open the project in Unity Editor (2019.4.x LTS recommended). The main scene is `Assets/Scenes/LevelScene.unity`. Add the "GameController" prefab to new scenes.

## Architecture

The project follows a **three-layer architecture**:

```
Assets/Scripts/
├── Data/                    # Data Layer - Persistence & Serialization
│   ├── DataSave/           # Player save data (JSON)
│   └── Level/              # Level loading and serialization
├── Logic/                   # Logic Layer - Game Rules & Entities
│   ├── Entity/             # Game objects (Mover, Player, Wall, etc.)
│   │   └── BaseClass/      # Base types (Enum, State, Tile)
│   ├── Event/              # Event management
│   ├── Grid/               # Spatial grid for collision detection
│   ├── Log/                # Logging utilities
│   └── Utility/            # Helper utilities (Utils, GridQuery, DirectionUtils)
└── Presentation/            # Presentation Layer - UI, Editor, Visuals
    ├── Editor/             # Unity Editor tools (Level Editor)
    ├── Gizmo/              # Scene gizmos
    └── Shaders/            # Custom shaders
```

### Layer Responsibilities

- **Data Layer**: Handles persistence, serialization, and data storage
- **Logic Layer**: Contains pure game logic, rules, and entity behavior
- **Presentation Layer**: Manages Unity Editor tools, UI, and visual effects

## Core Architecture

### Object Hierarchy
- **Mover**: Base class for objects that can move, fall, and be tracked for undo. Derive custom game objects from this class (e.g., Player).
  - Location: `Logic/Entity/Mover.cs`
- **Wall**: Static objects that block movement. Both Walls and Movers require child GameObjects with Box Colliders tagged "Tile".
  - Location: `Logic/Entity/Wall.cs`
- **Tile**: Simple struct holding transform reference and grid position. Used by Movers to track their occupied cells.
  - Location: `Logic/Entity/BaseClass/Tile.cs`
- **Player**: Derives from Mover. Handles input buffering and movement. Single instance per scene.
  - Location: `Logic/Entity/Player.cs`

### Game Management
- **Game**: Singleton that manages movers, walls, movement execution, undo/reset, and the LogicalGrid. Controls movement timing via `moveTime`, `fallTime`, and `moveBufferSpeedupFactor`.
  - Location: `Logic/Entity/Game.cs`
- **LogicalGrid**: Spatial hash mapping `Vector3Int` positions to GameObjects. Used for collision detection.
  - Location: `Logic/Grid/LogicalGrid.cs`
- **State**: Static class tracking undo stack. Records mover positions at each move for undo/reset functionality.
  - Location: `Logic/Entity/BaseClass/State.cs`
- **Utils**: Static utility class for position queries. Delegates to specialized classes:
  - **GridQuery**: Position queries (GetMoverAtPos, WallIsAtPos, TileIsEmpty, etc.)
  - **DirectionUtils**: Direction calculations (CheckDirection, IsRound, etc.)
  - Location: `Logic/Utility/`

### Level System
- **LevelManager**: Singleton managing level loading/unloading via serialized level files.
  - Location: `Data/Level/LevelManager.cs`
- **LevelLoader**: Loads `SerializedLevel` from JSON files in `Assets/Resources/Levels/`.
  - Location: `Data/Level/LevelLoader.cs`
- **LevelSerialization**: `SerializedLevel` and `SerializedLevelObject` classes for JSON serialization.
  - Location: `Data/Level/LevelSerialization.cs`
- **SaveData**: JSON serialization for persistent player data (levels beaten).
  - Location: `Data/DataSave/SaveData.cs`

### Events
- **EventManager**: Static C# events (`onLevelStarted`, `onMoveComplete`, `onPush`, `onUndo`, `onReset`, etc.) for game-wide communication.
  - Location: `Logic/Event/EventManager.cs`

## Level Editor

Access via **Window -> Level Editor** in Unity Editor.

- Prefabs defined in `Assets/leveleditorprefabs.txt` (one prefab name per line)
- Levels saved as JSON in `Assets/Resources/Levels/`
- Supports grid painting, erase mode, rotation (0/90/180/270), and spawn height adjustment
- Editor scripts located in: `Presentation/Editor/`

## Movement System

The movement system uses a two-phase approach:
1. **Planning Phase**: `TryPlanMove()` validates and plans moves, propagating pushes to other movers
2. **Execution Phase**: `MoveStart()` executes planned moves logically, then animates via DOTween

Key coordinate: Z-axis is depth (forward = falling direction). Ground is at Z=0.

### Push Behavior
- `Game.allowPushMulti` (default: true) determines push behavior:
  - `true`: Any mover can push other movers
  - `false`: Only the player can push (classic Sokoban)

## Example Games

- `Assets/Examples/Sokoban/` - Classic Sokoban win condition example
- `Assets/Examples/PipePushParadise/` - Pipe/water puzzle game example

## Coding Guidelines

### Utility Classes
For new code, prefer using specialized utility classes directly:
- `GridQuery`: Position queries (GetMoverAtPos, WallIsAtPos, TileIsEmpty, etc.)
- `DirectionUtils`: Direction calculations (CheckDirection, IsRound, vector utilities)

The `Utils` class provides backward-compatible methods that delegate to these classes.

### Infinite Loop Prevention
Movement planning methods (`TryPlanMove`, `CanMoveToward`, `PlanMove`, `PlanPushes`) use a `HashSet<Mover> visited` parameter to prevent infinite recursion with circular block configurations.
