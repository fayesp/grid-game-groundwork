# Grid Game Groundwork for Unity

A Unity project & level editor for making grid-based/block-pushing/Sokoban-like games.  
Made with Unity 2019.4 LTS  

![Example GIF](https://raw.githubusercontent.com/mytoboggan/grid-game-groundwork/master/ggg-demo.gif)

## Dependencies:
DOTween by Demigiant  
http://dotween.demigiant.com/  
https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676

## Setting Up Scenes:
The main scene is `Assets/Scenes/LevelScene.unity` — open it and press Play. It contains the "GameController" object (which hosts the `Game` singleton) and a `LevelManager`. There is no GameController prefab; to set up your own scene, copy that setup over from LevelScene.

See also the example scenes under `Assets/Examples/`.

## Using the Editor:
The editor window can be found under "Window" -> "Level Editor".
- Define the list of prefabs at `Assets/leveleditorprefabs.txt`, or assign prefabs manually to the "Prefabs" dropdown
- Select a game object (prefab)
- Left-click anywhere in the scene to paint in the selected gameObject
- Hold left-click to paint continuously
- Rotation (0/90/180/270) and spawn height can be adjusted in the window

_Note: Selecting "Erase" will clear any objects at that position._

## Deeper Dive:

The project assumes plenty about what kind of game you want to make, but there are no rules about friction or win conditions. There are two main object types that you can use in scenes, Wall and Mover.

I typically make objects derive from the Mover class (as the Player class does). Walls and Movers both need child gameObjects with a Box Collider component and tagged with "Tile". See the prefabs "Crate" and "Crate L" under `Assets/Prefabs` for examples of single-tile and multi-tile Movers.

## Architecture:

```
Assets/Scripts/
├── Logic/                   # Current runtime — game rules & entities
│   ├── Entity/              # Mover, Player, Wall, Game, Magnet, ...
│   │   └── BaseClass/       # Enum, State, Tile
│   ├── Event/               # EventManager (static C# events)
│   ├── Grid/                # LogicalGrid (spatial hash)
│   ├── Log/                 # Logger
│   └── Utility/             # Utils, GridQuery, DirectionUtils, WaitFor
│
├── Data/                    # Persistence & serialization
│   ├── DataSave/            # SaveData (JSON)
│   └── Level/               # LevelManager, LevelLoader, LevelSerialization
│
├── Presentation/            # Editor tools, gizmos, shaders
│   ├── Editor/              # LevelEditor window
│   ├── Gizmo/
│   └── Shaders/
│
├── Core/                    # (in migration) GameServices, GameEventBus, MovementSettings
├── Model/                   # (in migration) GameBoard, SpatialGrid, MovePlanner, MoverModel, CommandStack, ...
├── View/                    # (in migration) MoverView, PlayerView
└── Controller/              # (in migration) GamePresenter, PlayerInputController
```

Scenes currently run on the **Logic/** stack. The **Core/Model/View/Controller** stack is the target architecture, being migrated in piece by piece — see "New Architecture (In Migration)" below.

## Class Breakdown:

### Core Objects

**Mover**:  
Things that can move and fall and that should be tracked for the undo system. Derive your own game objects from this (as the Player does).  
Location: `Logic/Entity/Mover.cs`

**Wall**:  
Static things you should not be able to move and that should stop you from moving, including the ground.  
Location: `Logic/Entity/Wall.cs`

**Player**:  
Derives from Mover, handles character movement input. There should only be one Player in the scene.  
Location: `Logic/Entity/Player.cs`

**Tile**:  
Simple struct holding a transform reference and grid position. Movers track their occupied cells as a list of Tiles.  
Location: `Logic/Entity/BaseClass/Tile.cs`

**State**:  
Static class that tracks the undo stack — records mover positions at each move for undo/reset.  
Location: `Logic/Entity/BaseClass/State.cs`

### Game Management

**Game**:  
Singleton that manages movers, walls, movement execution, undo/reset, and the LogicalGrid. Also controls movement timing via `moveTime`, `fallTime`, and `moveBufferSpeedupFactor`.  
Location: `Logic/Entity/Game.cs`

**LogicalGrid**:  
Spatial hash mapping `Vector3Int` positions to GameObjects. Used for collision detection.  
Location: `Logic/Grid/LogicalGrid.cs`

**EventManager**:  
Static C# events (`onLevelStarted`, `onMoveComplete`, `onPush`, `onUndo`, `onReset`, etc.) for game-wide communication.  
Location: `Logic/Event/EventManager.cs`

**MyLogger**:  
Async file logger — queues messages from the main thread and writes them out on a background thread.  
Location: `Logic/Log/Logger.cs`

### Level System

**LevelManager**:  
Singleton managing level loading/unloading via serialized level files.  
Location: `Data/Level/LevelManager.cs`

**LevelLoader**:  
Loads `SerializedLevel` from JSON files in `Assets/Resources/Levels/`.  
Location: `Data/Level/LevelLoader.cs`

**LevelSerialization**:  
`SerializedLevel` and `SerializedLevelObject` classes for JSON serialization.  
Location: `Data/Level/LevelSerialization.cs`

**SaveData**:  
For saving persistent data, like a list of beaten levels by name/string.  
Location: `Data/DataSave/SaveData.cs`

### Utilities

**Utils**:  
General utilities class. Position queries delegate to GridQuery, direction math to DirectionUtils.  
Location: `Logic/Utility/Utils.cs`

**GridQuery**:  
Position queries — `GetMoverAtPos`, `WallIsAtPos`, `TileIsEmpty`, etc.  
Location: `Logic/Utility/GridQuery.cs`

**DirectionUtils**:  
Direction calculations — `CheckDirection`, `IsRound`, vector helpers.  
Location: `Logic/Utility/DirectionUtils.cs`

**WaitFor**:  
For caching yield instructions, borrowed from... somewhere on the internet years ago.  
Location: `Logic/Utility/WaitFor.cs`

### Example Entities

**Magnet**:  
A Mover with a magnet type that pulls attached blocks along via its MagnetFields. See the magnetField example.  
Location: `Logic/Entity/Magnet.cs`

**MagnetField**:  
Component on a Magnet that tracks which Movers are inside its field.  
Location: `Logic/Entity/MagnetField.cs`

**CubeRoller**:  
Standalone rolling-cube behaviour — pivot-based roll animation with ground detection. Used by the magnetField example.  
Location: `Logic/Entity/RollCube.cs`

### New Architecture (In Migration)

The project is mid-migration from the Logic stack above to an MVP + Command Pattern architecture: `GamePresenter` (replacing `Game`), `GameBoard` + `SpatialGrid` (replacing `LogicalGrid`), `MovePlanner` (pure C# move planning), `MoverModel` / `PlayerModel` (replacing `Mover` / `Player`), `CommandStack` + `ICommand` (replacing `State`), and `GameServices` + `GameEventBus` (replacing the scattered singletons and static events). `MoverView` / `PlayerView` handle the DOTween animation side, and `PlayerInputController` collects input.

No scene is wired to the new stack yet — the LevelEditor and Mover simply prefer it when it's present and fall back to the legacy stack otherwise. The full writeup lives in `docs/ARCHITECTURE.md`.

## Movement System:

Movement is two-phase:
1. **Planning**: `TryPlanMove()` validates the move and propagates pushes to other movers
2. **Execution**: `MoveStart()` applies the plan logically, then animates via DOTween

Key coordinate: Z-axis is depth — forward is the falling direction, and the ground is at Z=0.

`Game.allowPushMulti` (default: true) decides who can push:
- `true`: any mover can push other movers
- `false`: only the player can push (classic Sokoban)

Planning keeps a `HashSet<Mover> visited` so circular block configurations can't cause infinite recursion.

## Example Games:

- `Assets/Examples/Sokoban/` - classic Sokoban win-condition example
- `Assets/Examples/PipePushParadise/` - pipe/water puzzle example
- `Assets/Examples/magnetField/` - magnets that pull blocks along; shows off Magnet, MagnetField, and CubeRoller

## Documentation:

- `docs/ARCHITECTURE.md` — the new MVP architecture, in detail
- `docs/MIGRATION_GUIDE.md` — migrating from the legacy stack
- `docs/MVP_PATTERN_GUIDE.md` — the MVP pattern as used here
- `CLAUDE.md` — contributor guide & coding guidelines
