---
name: core-dev
description: Use when creating or modifying any C# file inside Assets/Scripts/Game.Core (gameplay logic of the fixed-seed roguelike). Enforces determinism, forbidden Unity APIs, KISS class design, naming convention, and Japanese comments.
---

# Game.Core Development Rules

## Absolute constraints (compile-level + hook-level enforced)

Never write in Game.Core:

```csharp
using UnityEngine;
MonoBehaviour, GameObject, Transform,
Vector2, Vector2Int, Vector3,
Time, Time.deltaTime,
Rigidbody, Collider,
UnityEngine.Random, Random.Range, Random.value
```

Allowed namespaces: `System`, `System.Collections.Generic`, `System.Text`.

- Randomness: one `System.Random` created from the injected seed in the `RogueGame` constructor. Never create a second random source.
- The ONLY method that advances gameplay is `RogueGame.Step(Command command)`.
- All positions are integer `GridPos`. All battle checks are integer comparisons.
- Never iterate unordered collections (Dictionary/HashSet) for gameplay decisions — use List sorted by unit id.
- All tunable numbers live in `GameConfig`. No magic numbers in logic.
- No empty catch, no `catch (Exception) {}` swallowing.

## Fixed class list (do not add more)

```text
GameConfig   — all tunable numbers
GridPos      — readonly struct, int x / int y
GameMap      — grid tiles, generated from seed
GameUnit     — one class for player AND enemy (id, is_player, pos, hp)
GameState    — turn, result, player, enemies, map
Replay       — stable replay text (seed, per-turn lines, enemies by id order)
RogueGame    — main class, Step() drives everything
```

Enums: `Command { Up, Down, Left, Right, Wait }`, `TileType { Floor, Wall, Goal }`, `GameResult { Playing, Win, Lose, Draw }`.

## RogueGame public API (keep exactly this shape)

```csharp
public sealed class RogueGame
{
    public RogueGame(int seed, GameConfig config);

    /// <summary>現在のゲーム状態</summary>
    public GameState state { get; }

    /// <summary>コマンドを1つ実行してターンを1つ進める</summary>
    public GameState Step(Command command);

    /// <summary>安定したリプレイ文字列を返す</summary>
    public string GetReplayText();

    /// <summary>ゲームが終了したかどうか</summary>
    public bool isEnd { get; }
}
```

## Turn flow inside Step()

```text
1. If game already ended, return current state.
2. turn++.
3. Player: move / attack / blocked by wall.
4. Enemies (alive, ascending id order): if next to player -> attack, else move 1 cell toward player.
   Movement: try larger |dx|/|dy| axis first, then other axis, else wait.
5. Check Win (player on goal) / Lose (player hp <= 0) / Draw (turn >= max_turn).
6. Append replay line.
7. Return state.
```

## Map generation (BSP dungeon)

```text
1. Fill everything with Wall (carve out of solid rock).
2. Recursively split space (BSP): split the longer axis at a random cut,
   stop when area < min_area_size * 2. Fixed recursion order = deterministic.
3. Carve one random-sized room per leaf (>= min_room_size, 1-tile margin kept).
4. Connect consecutive rooms with L-shaped corridors (horizontal then vertical)
   -> chain guarantees full connectivity.
5. Player spawns at first room's center.
6. Goal = center of the room farthest (manhattan) from spawn
   (single-room map: room corner; throws if too small).
7. Enemies on random walkable cells, same single random source.
```

## Replay line format (stable, always identical layout)

```text
Seed=12345
Turn=1 Command=Right Result=Playing Player=(2,1):10 Enemies=[0:(5,1):3,1:(7,4):3]
```

## Naming convention (user rule — overrides SPEC.md example casing)

- Class PascalCase / public method PascalCase / private method `_PascalCase`
- public field snake_case / private field `_snake_case` / locals & params snake_case
- public const UPPER_SNAKE_CASE / private const `_UPPER_SNAKE_CASE`
- property (accessor) camelCase
- Comments: simple Japanese, `/// <summary>` on classes, methods, public fields and private member fields.

Example:

```csharp
/// <summary>
/// ゲーム全体の調整値を保持するクラス
/// </summary>
public sealed class GameConfig
{
    /// <summary>マップの幅</summary>
    public int width = 10;

    /// <summary>最大ターン数</summary>
    public int max_turn = 100;
}
```

## After finishing any Core change

Run the constraint-check skill (or `.claude/hooks/check-core.ps1` runs automatically after each edit). Fix violations immediately without weakening tests.
