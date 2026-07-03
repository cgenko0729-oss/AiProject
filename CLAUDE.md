# CLAUDE.md — Unity Fixed-Seed 3D Dungeon Roguelike (Project Root)

## Requirement documents (read before coding)

1. `Assets/Document/AI課題（Unity）_0630.pdf` — original homework spec (highest authority)
2. `Assets/SPEC.md` — concrete KISS game spec (authoritative for gameplay detail)
3. `Assets/SKILL.md` — homework skill / rules
4. `Assets/PROMPT_FOR_CLAUDE_CODE.md` — phase-by-phase prompts
5. `PLAN.md` — implementation plan and progress tracking

## Main rule

```text
Same Seed + same Command sequence = exactly same replay and result
```

## Layer structure (mandatory)

| Layer | Path | Rule |
|---|---|---|
| Core | `Assets/Scripts/Game.Core/` | Pure C#. asmdef with `"noEngineReferences": true`. Zero UnityEngine. |
| Presentation | `Assets/Scripts/Game.Presentation/` | MonoBehaviour view + input only. No gameplay rules. References Game.Core. |
| Tests | `Assets/Tests/EditMode/Game.Core.Tests/` | NUnit `[Test]` Edit Mode only. No `[UnityTest]`, no scene, no GameObject. |

## Forbidden in Game.Core

`using UnityEngine` / `MonoBehaviour` / `GameObject` / `Transform` / `Vector2` / `Vector2Int` / `Vector3` / `Time.*` / `Rigidbody` / `Collider` / `UnityEngine.Random` / `Random.Range` / `Random.value`

- Randomness: only `new System.Random(seed)`, injected through the `RogueGame` constructor.
- Gameplay advances ONLY through `RogueGame.Step(Command command)`.
- Movement / battle: integer `GridPos` math. No physics, no frames.
- A PostToolUse hook (`.claude/hooks/check-core.ps1`) automatically rejects edits that violate this.

## KISS

Keep only these classes:
`GameConfig, GridPos, GameMap, GameUnit, GameState, Replay, RogueGame, RogueGameView, RogueGameTests`
plus enums `Command, TileType, GameResult`.
No interfaces, no design patterns, no A*, no DI, no event bus. Simple English names. Comments in simple Japanese using `/// <summary>`.

## C# Coding Rules (user convention — overrides naming shown in SPEC.md examples)

| Item | Rule | Example |
|---|---|---|
| Constant public | UPPER_SNAKE_CASE | `MAX_TURN_DEFAULT` |
| Constant private | `_` + UPPER_SNAKE_CASE | `_MAX_TURN_DEFAULT` |
| Member variable public | snake_case | `player_hp` |
| Member variable private | `_` + snake_case | `_player_hp` |
| Local variable / parameter | snake_case | `next_pos` |
| Class | PascalCase | `RogueGame` |
| Method public | PascalCase | `Step` |
| Method private | `_` + PascalCase | `_MoveEnemies` |
| Accessor (property) | camelCase | `isEnd` |
| Getter/Setter method | `get` prefix camelCase | `getReplayText` |

Comment rules:
- Class: `/// <summary>` describing purpose and usage.
- Function: `/// <summary>` + `<param>` + `<returns>`.
- Public variables and private member variables: `/// <summary>` required.
- Line comments `//` above the line, only for important logic or intention.
- Comment text: simple Japanese.

## Input

Project uses the **new Input System only** (`activeInputHandler: 1`).
In Presentation use `UnityEngine.InputSystem.Keyboard.current` (e.g. `wKey.wasPressedThisFrame`).
Do NOT use legacy `Input.GetKeyDown` — it throws at runtime in this project.

## Unity environment

- Editor version: **6000.0.62f1** (URP)
- Editor exe: `C:\Program Files\Unity\Hub\Editor\6000.0.62f1\Editor\Unity.exe`
- Run Edit Mode tests headless: `powershell -File tools/run-tests.ps1` (editor must be closed first — batch mode fails while the project is open).

## Git workflow

- Remote: `origin https://github.com/cgenko0729-oss/AiProject.git`
- One branch per phase: `feature/phase1-structure`, `feature/phase2-core-skeleton`, ...
- Commit after each phase with a clear message, push, merge into `main` only after constraint check passes.
- Never commit `Library/`, `Temp/`, `TestResults/`, `CodeCoverage/`.

## After every phase, report

```text
Changed files:
What was done:
How to test:
Constraint check:
Remaining risks:
Next step:
```
