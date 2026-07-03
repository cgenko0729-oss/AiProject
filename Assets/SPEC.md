# SPEC.md — Unity 固定 Seed 3D Dungeon Roguelike 課題仕様 KISS版

## 0. Project Goal / 目的

Create a small **3D dungeon battle game** in Unity.

Visual direction:

- 3D dungeon atmosphere
- similar feeling to the dungeon battle part of 「風来之国」 as a loose reference
- simple grid-based dungeon room
- player moves one grid cell at a time
- enemies move and attack in turns
- simple but readable presentation

Important:

This is **not** a real-time action game.  
This homework is still a **fixed-seed turn-based roguelike**.

The main technical rule is:

```text
Same Seed + same Command sequence = exactly same replay and result
```

The goal is to prove that the game logic is deterministic, testable, and separated from Unity.

---

## 1. What Kind of Game / ゲーム内容

A small 3D grid dungeon game.

Example:

```text
##########
#P...E...#
#..##....#
#....#...#
#...E..G.#
##########
```

Meaning:

| Symbol | Meaning |
|---|---|
| `#` | Wall |
| `.` | Floor |
| `P` | Player |
| `E` | Enemy |
| `G` | Goal / Exit |

Unity view can show this as:

| Core Data | Unity View Example |
|---|---|
| Floor | cube / plane |
| Wall | cube |
| Player | capsule / simple character |
| Enemy | capsule / cube |
| Goal | glowing cube / door |

The player may use WASD, but WASD is only converted into Core commands.

```text
W -> MoveUp
S -> MoveDown
A -> MoveLeft
D -> MoveRight
Space -> Wait
```

---

## 2. Absolute Determinism Rule / 決定論ルール

The result does not need to be the same if player inputs are different.

Correct rule:

```text
Same Seed + same Command sequence = same result
```

Example:

```text
Seed = 12345
Commands = MoveUp, MoveRight, MoveRight, Wait, MoveDown
```

If this is run twice, both runs must produce exactly the same:

- map layout
- wall positions
- enemy positions
- enemy movement
- player position
- HP values
- turn count
- win / lose / draw result
- replay text

---

## 3. KISS Principle / シンプル設計方針

This project must stay simple.

Do not create too many classes.

Do not use complicated architecture.

Do not use many design patterns.

Avoid:

- complex inheritance
- dependency injection framework
- event bus
- service locator
- over-separated managers
- complex state machine framework
- Addressables
- object pooling
- DOTween
- ScriptableObject-heavy data system
- advanced pathfinding
- inventory
- skills
- item drops
- many enemy types

Use simple English names.

Good names:

```text
RogueGame
GameConfig
GridPos
GameMap
GameUnit
GameState
Replay
Command
TileType
GameResult
```

Bad for this homework:

```text
AbstractGridBasedDungeonGameplaySimulationController
DeterministicRoguelikeEntityOrchestrationService
IRuntimeDungeonBattleDependencyProvider
```

---

## 4. Required Layer Structure / 必須レイヤー構成

Use only two main code layers plus tests.

```text
Assets/
  Scripts/
    Game.Core/
      Game.Core.asmdef
      GameConfig.cs
      GridPos.cs
      GameMap.cs
      GameUnit.cs
      GameState.cs
      Replay.cs
      RogueGame.cs

    Game.Presentation/
      Game.Presentation.asmdef
      RogueGameView.cs

  Tests/
    EditMode/
      Game.Core.Tests/
        Game.Core.Tests.asmdef
        RogueGameTests.cs
```

This is intentionally simple.

Do not split into too many folders unless necessary.

---

## 5. Assembly Rules / asmdef ルール

### 5.1 Game.Core

`Game.Core` contains all real gameplay rules.

Allowed:

- plain C#
- `System`
- `System.Collections.Generic`
- `System.Text`
- `System.Random`

Forbidden:

```csharp
using UnityEngine;
MonoBehaviour
GameObject
Transform
Vector2
Vector2Int
Vector3
Time
Time.deltaTime
Rigidbody
Collider
UnityEngine.Random
Random.Range
Random.value
```

Core must not depend on Unity.

### 5.2 Game.Presentation

`Game.Presentation` contains Unity input and display only.

Allowed:

```csharp
using UnityEngine;
MonoBehaviour
GameObject
Transform
Input
KeyCode
PrimitiveType
```

Presentation can:

- read WASD
- convert key to `Command`
- call `RogueGame.Step(command)`
- render map and units
- show result

Presentation must not:

- decide win / lose
- calculate damage
- generate gameplay map
- use Unity physics for battle result
- use Unity random for gameplay

### 5.3 Game.Core.Tests

Use Edit Mode tests.

Allowed:

- NUnit `[Test]`
- direct creation of Core classes

Avoid:

- `[UnityTest]`
- waiting for frames
- scene dependency
- GameObject dependency

---

## 6. Minimum Class Design / 最小クラス設計

Keep class count small.

### 6.1 GameConfig

Stores all tunable numbers.

Example fields:

```csharp
public sealed class GameConfig
{
    public int Width = 10;
    public int Height = 10;
    public int MaxTurn = 100;

    public int PlayerHp = 10;
    public int PlayerAttack = 2;

    public int EnemyCount = 3;
    public int EnemyHp = 3;
    public int EnemyAttack = 1;

    public int WallPercent = 15;
}
```

Avoid magic numbers in logic.

### 6.2 GridPos

Integer position.

```csharp
public readonly struct GridPos
{
    public int X { get; }
    public int Y { get; }
}
```

Do not use Unity `Vector3` in Core.

### 6.3 TileType

```csharp
public enum TileType
{
    Floor,
    Wall,
    Goal
}
```

### 6.4 Command

```csharp
public enum Command
{
    Up,
    Down,
    Left,
    Right,
    Wait
}
```

### 6.5 GameResult

```csharp
public enum GameResult
{
    Playing,
    Win,
    Lose,
    Draw
}
```

### 6.6 GameUnit

Use one simple class for both player and enemy.

```csharp
public sealed class GameUnit
{
    public int Id;
    public bool IsPlayer;
    public GridPos Pos;
    public int Hp;

    public bool IsAlive => Hp > 0;
}
```

This avoids creating too many classes.

### 6.7 GameMap

Stores the grid.

Responsibilities:

- create map from Seed
- check tile
- check walkable
- expose width / height

### 6.8 GameState

Stores current game state.

Responsibilities:

- turn number
- result
- player
- enemies
- map

This can be a class or a simple data holder.

### 6.9 Replay

Stores stable replay text.

Responsibilities:

- record Seed
- record command
- record turn state
- output stable text

Enemies must be recorded in ID order.

### 6.10 RogueGame

Main Core class.

Public API:

```csharp
public sealed class RogueGame
{
    public RogueGame(int seed, GameConfig config);

    public GameState State { get; }

    public GameState Step(Command command);

    public string GetReplayText();

    public bool IsEnd { get; }
}
```

`Step(command)` is the only method that advances gameplay.

---

## 7. Turn Flow / ターン処理

Each key press equals one turn.

`RogueGame.Step(command)` should do:

```text
1. If game already ended, return current state.
2. Increase turn count.
3. Move or attack with player.
4. Remove dead enemies from behavior.
5. Move or attack with enemies in ID order.
6. Check win / lose / draw.
7. Record replay.
8. Return state.
```

### Player behavior

If player command points to floor / goal:

```text
move player
```

If player command points to wall:

```text
do not move
```

If player command points to enemy:

```text
attack enemy
do not move into enemy tile
```

### Enemy behavior

Keep AI simple.

For each alive enemy in ascending ID order:

```text
If enemy is next to player:
    attack player
Else:
    move one cell toward player
```

Simple movement rule:

```text
1. Compare abs(dx) and abs(dy).
2. Try larger axis first.
3. If blocked, try other axis.
4. If both blocked, wait.
```

No A* pathfinding needed.

---

## 8. Map Generation / マップ生成

Map must be generated from Seed.

Simple algorithm:

```text
1. Fill map with Floor.
2. Put Wall around outer border.
3. Set player spawn at (1, 1).
4. Set goal at (Width - 2, Height - 2).
5. Use System.Random(seed) to place some inner walls.
6. Do not place wall on player spawn.
7. Do not place wall on goal.
8. Place enemies on walkable cells using the same random source.
```

KISS version is enough.

Do not implement complex dungeon room generation unless time allows.

Visual presentation can still look like a 3D dungeon by using wall cubes and floor cubes.

---

## 9. Replay Format / リプレイ形式

Replay must be stable and easy to compare.

Example:

```text
Seed=12345
Turn=1 Command=Right Result=Playing Player=(2,1):10 Enemies=[0:(5,1):3,1:(7,4):3]
Turn=2 Command=Right Result=Playing Player=(3,1):10 Enemies=[0:(4,1):3,1:(6,4):3]
```

Rules:

- record seed
- record every command
- record turn
- record player position and HP
- record enemies sorted by ID
- record result
- use same format every time

---

## 10. Required Tests / 必須テスト

Use one simple test file:

```text
RogueGameTests.cs
```

Required tests:

### 10.1 SameSeedAndSameCommands_ShouldCreateSameReplay

- create two games with same Seed
- run same command list
- assert replay text equals

### 10.2 SameSeedAndDifferentCommands_ShouldCreateDifferentReplay

- create two games with same Seed
- run different command list
- assert replay text differs

### 10.3 PlayerCannotMoveIntoWall

- use a test map or Seed where wall exists
- move into wall
- assert player position did not change

### 10.4 PlayerAttackEnemy_ShouldReduceEnemyHp

- put enemy beside player
- step toward enemy
- assert enemy HP decreased

### 10.5 EnemyAttackPlayer_ShouldReducePlayerHp

- put enemy beside player
- step wait
- assert player HP decreased

### 10.6 Goal_ShouldWin

- move player to goal by fixed commands or test setup
- assert result is Win

### 10.7 MaxTurn_ShouldDraw

- set small MaxTurn
- step wait until max turn
- assert result is Draw

---

## 11. Presentation Specification / Unity 表示仕様

Use only one or very few MonoBehaviour classes.

Recommended:

```text
RogueGameView.cs
```

Responsibilities:

- create `RogueGame`
- build simple 3D grid view
- read WASD
- call `Step(command)`
- refresh unit objects
- show result with simple UI or Debug.Log

Simple view is enough:

```text
Wall = gray cube
Floor = flat cube
Player = blue capsule
Enemy = red capsule
Goal = green cube
```

Do not make code too complex.

---

## 12. Forbidden Implementation Examples / 禁止例

Bad Core code:

```csharp
using UnityEngine;

public class RogueGame : MonoBehaviour
{
    void Update()
    {
        transform.position += Vector3.forward * Time.deltaTime;
    }
}
```

Bad because:

- Core uses UnityEngine
- uses MonoBehaviour
- uses Transform
- uses Time.deltaTime
- frame-dependent

Bad random:

```csharp
int x = Random.Range(0, width);
```

Correct:

```csharp
var random = new System.Random(seed);
int x = random.Next(0, width);
```

Bad battle:

```csharp
void OnTriggerEnter(Collider other)
{
    playerHp--;
}
```

Correct:

```csharp
if (enemy.Pos.IsNextTo(player.Pos))
{
    player.Hp -= config.EnemyAttack;
}
```

---

## 13. Submission Checklist / 提出前チェック

Project is acceptable when:

- Core compiles without UnityEngine.
- Core has no MonoBehaviour.
- Core has no UnityEngine.Random.
- Core has no Time.deltaTime.
- Core has no Rigidbody / Collider.
- Gameplay advances only by `Step(Command command)`.
- WASD only converts to Command.
- Same Seed + same Commands gives same replay.
- Tests have real asserts.
- Code is simple and understandable.
- Class count is small.
- Function and variable names are simple English.
- Comments are simple Japanese.
