# SKILL.md — Claude Code Skill: KISS Unity Fixed-Seed 3D Dungeon

## Skill Name

KISS Unity Fixed-Seed 3D Dungeon Roguelike Builder

---

## Use This Skill When

Use this skill for this project:

- Unity homework
- fixed Seed turn-based roguelike
- 3D dungeon visual style
- simple grid battle
- Eastward / 風来之国 dungeon-battle-like atmosphere as a loose visual reference
- KISS implementation
- Claude Code assisted coding

---

## Main Goal

Build a small 3D dungeon game where:

```text
Same Seed + same Command sequence = exactly same replay
```

The game is turn-based and grid-based.

It can look like a 3D dungeon, but the logic must be pure C# and deterministic.

---

## Design Philosophy

### KISS First

Always prefer the simpler solution.

Do not make many classes.

Do not over-separate responsibilities.

Do not create architecture that is bigger than the homework.

Good:

```text
GameConfig
GridPos
GameMap
GameUnit
GameState
Replay
RogueGame
RogueGameView
RogueGameTests
```

Bad:

```text
IGridMapRepository
DungeonDomainRuntimeService
UnitBattleCalculationStrategyFactory
TurnPipelineExecutionContext
```

### Simple English Names

Use simple English names.

Good variable names:

```text
seed
config
map
player
enemies
turn
result
command
nextPos
enemy
damage
```

Good function names:

```text
Step
MovePlayer
MoveEnemies
AttackEnemy
AttackPlayer
CanMove
IsWall
IsGoal
AddReplay
```

Avoid long abstract names.

---

## Absolute Core Rules

In `Assets/Scripts/Game.Core/`, never write:

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

Core must be plain C#.

Core must not depend on frame rate, Unity physics, or Unity random.

---

## Correct Runtime Flow

Presentation:

```text
WASD key input
-> convert to Command
-> call RogueGame.Step(command)
-> get GameState
-> update 3D view
```

Core:

```text
Step(command)
-> move / attack player
-> move / attack enemies
-> check result
-> write replay
```

---

## Required File Structure

Use this simple structure:

```text
Assets/Scripts/Game.Core/
  Game.Core.asmdef
  GameConfig.cs
  GridPos.cs
  GameMap.cs
  GameUnit.cs
  GameState.cs
  Replay.cs
  RogueGame.cs

Assets/Scripts/Game.Presentation/
  Game.Presentation.asmdef
  RogueGameView.cs

Assets/Tests/EditMode/Game.Core.Tests/
  Game.Core.Tests.asmdef
  RogueGameTests.cs
```

Do not add extra folders unless really needed.

---

## Required Core API

Try to keep `RogueGame` close to this:

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

The only gameplay progression method is:

```csharp
Step(Command command)
```

---

## Required Enums

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

```csharp
public enum TileType
{
    Floor,
    Wall,
    Goal
}
```

```csharp
public enum GameResult
{
    Playing,
    Win,
    Lose,
    Draw
}
```

---

## Random Rule

Use explicit Seed.

Correct:

```csharp
private readonly System.Random _random;

public RogueGame(int seed, GameConfig config)
{
    _random = new System.Random(seed);
}
```

Incorrect:

```csharp
Random.Range(0, 10);
UnityEngine.Random.value;
```

---

## Turn Rule

One command means one turn.

Correct:

```csharp
game.Step(Command.Right);
```

Incorrect:

```csharp
void Update()
{
    game.Step(Command.Wait);
}
```

`Update()` may read input, but must not automatically advance the turn every frame.

---

## Battle Rule

No Unity physics for battle.

Correct:

```text
If enemy grid position is next to player grid position:
    player HP -= enemy attack
```

Incorrect:

```csharp
void OnTriggerEnter(Collider other)
{
    playerHp--;
}
```

---

## Enemy AI Rule

Keep enemy AI simple.

For each enemy by ID order:

```text
If next to player:
    attack player
Else:
    move one cell toward player
```

No A* unless specifically requested later.

---

## Replay Rule

Replay must be stable.

Record:

- seed
- turn
- command
- result
- player position
- player HP
- enemies sorted by ID
- enemy HP

Never rely on unordered collection order.

---

## Test Rules

Use one test file first:

```text
RogueGameTests.cs
```

Required tests:

```text
SameSeedAndSameCommands_ShouldCreateSameReplay
SameSeedAndDifferentCommands_ShouldCreateDifferentReplay
PlayerCannotMoveIntoWall
PlayerAttackEnemy_ShouldReduceEnemyHp
EnemyAttackPlayer_ShouldReducePlayerHp
Goal_ShouldWin
MaxTurn_ShouldDraw
```

Every test must have meaningful Assert.

No scene.

No GameObject.

No frame wait.

---

## Presentation Rules

Use one simple MonoBehaviour:

```text
RogueGameView.cs
```

It can:

- create floor/wall/player/enemy/goal objects
- read WASD
- call Core Step
- update positions
- Debug.Log result

It must not:

- decide battle damage
- decide win/lose
- generate gameplay randomness
- use physics as gameplay authority

---

## Claude Code Work Procedure

Follow these phases.

### Phase 0 — Read and Plan

Read:

- `SPEC.md`
- `SKILL.md`

Then output plan only.

Do not edit files yet.

### Phase 1 — Structure

Create folders and asmdef files.

### Phase 2 — Core Skeleton

Create simple Core files.

Do not implement Presentation yet.

### Phase 3 — Tests

Create `RogueGameTests.cs`.

Tests may fail at first.

### Phase 4 — Core Implementation

Implement only enough code to pass tests.

### Phase 5 — Presentation

Create `RogueGameView.cs`.

### Phase 6 — Review

Check constraints.

### Phase 7 — Final Summary

Report file list, tests, and known limitations.

---

## Mechanical Check Commands

Bash:

```bash
grep -R "using UnityEngine" Assets/Scripts/Game.Core
grep -R "UnityEngine.Random\|Random.Range\|Random.value" Assets/Scripts/Game.Core
grep -R "Time.deltaTime\|Rigidbody\|Collider\|MonoBehaviour\|Transform\|GameObject" Assets/Scripts/Game.Core
```

PowerShell:

```powershell
Select-String -Path "Assets/Scripts/Game.Core/**/*.cs" -Pattern "using UnityEngine","UnityEngine.Random","Random.Range","Random.value","Time.deltaTime","Rigidbody","Collider","MonoBehaviour","Transform","GameObject"
```

Expected result:

```text
No forbidden usage in Core.
```

---

## Reject and Fix Prompts

### If Core uses UnityEngine

```text
Reject this code. Game.Core must not use UnityEngine.
Remove UnityEngine and replace all Unity types with plain C# types.
Use GridPos instead of Vector3 or Vector2Int.
```

### If code is too complicated

```text
The code is too complicated for this homework.
Simplify it.
Use fewer classes.
Use simple English names.
Avoid interfaces and design patterns unless truly necessary.
Follow KISS.
```

### If gameplay is in Presentation

```text
Move this gameplay logic to Game.Core.
Presentation should only read input, call RogueGame.Step(command), and update view.
```

### If using physics

```text
Do not use Rigidbody, Collider, OnTriggerEnter, or OnCollisionEnter for gameplay result.
Use integer grid position checks inside Game.Core.
```

### If tests are weak

```text
This test is weak because it only runs code.
Add real Assert checks for replay, position, HP, turn, or result.
```

---

## Final Answer Format for Claude

After each phase, answer with:

```text
Changed files:
What was done:
How to test:
Constraint check:
Remaining risks:
Next step:
```
