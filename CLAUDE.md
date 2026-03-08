# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Grid Game Groundwork is a Unity project and level editor for making grid-based/block-pushing/Sokoban-like games. Built with Unity 2019.4.10f1 LTS.

**Required Dependency**: DOTween by Demigiant (http://dotween.demigiant.com/)

## Unity Development

Open the project in Unity Editor (2019.4.x LTS recommended). The main scene is `Assets/Scenes/LevelScene.unity`. Add the "GameController" prefab to new scenes.

## Core Architecture

### Object Hierarchy
- **Mover**: Base class for objects that can move, fall, and be tracked for undo. Derive custom game objects from this class (e.g., Player).
- **Wall**: Static objects that block movement. Both Walls and Movers require child GameObjects with Box Colliders tagged "Tile".
- **Tile**: Simple struct holding transform reference and grid position. Used by Movers to track their occupied cells.
- **Player**: Derives from Mover. Handles input buffering and movement. Single instance per scene.

### Game Management
- **Game**: Singleton that manages movers, walls, movement execution, undo/reset, and the LogicalGrid. Controls movement timing via `moveTime`, `fallTime`, and `moveBufferSpeedupFactor`.
- **LogicalGrid**: Spatial hash mapping `Vector3Int` positions to GameObjects. Used for collision detection.
- **State**: Static class tracking undo stack. Records mover positions at each move for undo/reset functionality.
- **Utils**: Static utility class for position queries (GetMoverAtPos, WallIsAtPos, TileIsEmpty, etc.).

### Level System
- **LevelManager**: Singleton managing level loading/unloading via serialized level files.
- **LevelLoader**: Loads `SerializedLevel` from JSON files in `Assets/Resources/Levels/`.
- **LevelSerialization**: `SerializedLevel` and `SerializedLevelObject` classes for JSON serialization.
- **SaveData**: Binary serialization for persistent player data (levels beaten).

### Events
- **EventManager**: Static C# events (`onLevelStarted`, `onMoveComplete`, `onPush`, `onUndo`, `onReset`, etc.) for game-wide communication.

## Level Editor

Access via **Window -> Level Editor** in Unity Editor.

- Prefabs defined in `Assets/leveleditorprefabs.txt` (one prefab name per line)
- Levels saved as JSON in `Assets/Resources/Levels/`
- Supports grid painting, erase mode, rotation (0/90/180/270), and spawn height adjustment

## Movement System

The movement system uses a two-phase approach:
1. **Planning Phase**: `TryPlanMove()` validates and plans moves, propagating pushes to other movers
2. **Execution Phase**: `MoveStart()` executes planned moves logically, then animates via DOTween

Key coordinate: Z-axis is depth (forward = falling direction). Ground is at Z=0.

## Polyban Mode

`Game.isPolyban` (default: true) determines push behavior:
- `true`: Any mover can push other movers
- `false`: Only the player can push (classic Sokoban)

## Example Games

- `Assets/Examples/Sokoban/` - Classic Sokoban win condition example
- `Assets/Examples/PipePushParadise/` - Pipe/water puzzle game example
