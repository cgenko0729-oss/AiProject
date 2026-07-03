# CLAUDE.md — Project Instructions KISS Version

Read these first:

1. `SPEC.md`
2. `SKILL.md`
3. `PROMPT_FOR_CLAUDE_CODE.md`

Project:

A small 3D dungeon battle game with a loose 「風来之国」 dungeon battle atmosphere, but implemented as a deterministic fixed-seed turn-based grid roguelike.

Main rule:

```text
Same Seed + same Command sequence = exactly same replay
```

Design rule:

```text
KISS.
Simple code.
Few classes.
Simple English names.
Simple Japanese comments.
```

Hard Core restrictions:

Inside `Assets/Scripts/Game.Core/`, do not use:

```csharp
using UnityEngine;
MonoBehaviour;
GameObject;
Transform;
Vector2;
Vector2Int;
Vector3;
Time;
Time.deltaTime;
Rigidbody;
Collider;
UnityEngine.Random;
Random.Range;
Random.value;
```

Core gameplay must advance only through:

```csharp
RogueGame.Step(Command command)
```

Recommended class list:

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

After every phase, report:

```text
Changed files:
What was done:
How to test:
Constraint check:
Remaining risks:
Next step:
```
