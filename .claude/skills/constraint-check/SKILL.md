---
name: constraint-check
description: Use to mechanically verify the homework constraints — forbidden Unity API usage in Game.Core, asmdef purity, test quality, and KISS review. Run at the end of every phase and before every merge to main.
---

# Constraint Check

Run all checks below. Report each as PASS / FAIL with evidence. Fix FAIL before merging.

## 1. Forbidden API grep (Game.Core)

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/hooks/check-core.ps1
```

Exit 0 = clean. Exit 2 = violations printed to stderr.

Equivalent manual grep (Git Bash):

```bash
grep -RnE "using +UnityEngine|MonoBehaviour|GameObject|Transform|Vector2|Vector3|\bTime\.|Rigidbody|Collider|UnityEngine\.Random|Random\.Range|Random\.value" Assets/Scripts/Game.Core --include=*.cs
```

Expected: no output.

## 2. asmdef purity

`Assets/Scripts/Game.Core/Game.Core.asmdef` must contain:

```json
"noEngineReferences": true
```

and must not reference any Unity assembly.
`Game.Presentation.asmdef` references `Game.Core`.
`Game.Core.Tests.asmdef` is an Edit Mode test assembly (`"includePlatforms": ["Editor"]`) referencing `Game.Core` with NUnit.

## 3. Determinism / test quality

- Every test has a meaningful `Assert` (replay text, position, HP, turn, result). A test that only runs code is a violation.
- Tests use NUnit `[Test]` only — no `[UnityTest]`, no frame waits, no GameObject, no scene.
- Required test list exists in `RogueGameTests.cs`:
  `SameSeedAndSameCommands_ShouldCreateSameReplay`, `SameSeedAndDifferentCommands_ShouldCreateDifferentReplay`, `PlayerCannotMoveIntoWall`, `PlayerAttackEnemy_ShouldReduceEnemyHp`, `EnemyAttackPlayer_ShouldReducePlayerHp`, `Goal_ShouldWin`, `MaxTurn_ShouldDraw`.

## 4. Presentation purity

`RogueGameView.cs` must NOT contain: damage numbers, win/lose decisions, map generation, gameplay randomness, `OnTriggerEnter`, `OnCollisionEnter`, or per-frame automatic `Step()` calls. It only: reads keys → converts to `Command` → calls `Step` → renders `GameState`.

## 5. KISS review

- Class count small (the fixed list only).
- No interfaces / design patterns / A* / DI / event bus.
- Simple English names, simple Japanese comments.
- No empty catch or `catch (Exception)` swallowing.
- Numbers concentrated in `GameConfig`.

## 6. Run tests (proof)

Use the unity-test skill (`tools/run-tests.ps1`). All tests green is required before merge.
