# PLAN.md — 実装計画 / Implementation Plan

固定シード式ターン制ローグライク（Unity 3D ダンジョン表示）の実装計画。
仕様の正: `Assets/Document/AI課題（Unity）_0630.pdf` + `Assets/SPEC.md`。

## Progress

- [x] Phase 0 — Read docs, create skills/hooks/plan
- [ ] Phase 1 — Folders + asmdef (Game.Core / Game.Presentation / Game.Core.Tests)
- [ ] Phase 2 — Core skeleton (7 classes + 3 enums, no logic)
- [ ] Phase 3 — Edit Mode tests first (7 required tests, red allowed)
- [ ] Phase 4 — Core implementation until all tests green
- [ ] Phase 5 — 3D Presentation (RogueGameView, WASD + Space)
- [ ] Phase 6 — Constraint check + simplify
- [ ] Phase 7 — Final review + submission checklist

## Branch strategy

| Phase | Branch | Merge condition |
|---|---|---|
| 1 | feature/phase1-structure | asmdef compiles, check-core passes |
| 2 | feature/phase2-core-skeleton | Core compiles without UnityEngine |
| 3 | feature/phase3-tests | 7 tests exist with real Asserts (red OK) |
| 4 | feature/phase4-core-logic | all Edit Mode tests green |
| 5 | feature/phase5-presentation | playable in editor, no gameplay in view |
| 6-7 | feature/phase6-review | constraint-check all PASS |

## Mechanical gates (the "番人")

1. `Game.Core.asmdef` → `"noEngineReferences": true` — UnityEngine in Core = compile error (制約F, strongest).
2. `.claude/hooks/check-core.ps1` — PostToolUse hook, greps forbidden API on every edit (制約A).
3. Determinism tests — same seed twice → identical replay text (制約B).
4. `GameConfig` holds all numbers (制約C). No empty catch (制約D).
5. ~~Coverage 90% (制約E)~~ — DEFERRED by user decision (2026-07-04). `tools/run-tests.ps1 -coverage` stays ready if needed later.

## Class plan (KISS, fixed)

```text
Game.Core:          GameConfig, GridPos, GameMap, GameUnit, GameState, Replay, RogueGame
                    enums: Command, TileType, GameResult
Game.Presentation:  RogueGameView (1 MonoBehaviour)
Tests:              RogueGameTests (7 tests)
```

## Required tests

1. SameSeedAndSameCommands_ShouldCreateSameReplay
2. SameSeedAndDifferentCommands_ShouldCreateDifferentReplay
3. PlayerCannotMoveIntoWall
4. PlayerAttackEnemy_ShouldReduceEnemyHp
5. EnemyAttackPlayer_ShouldReducePlayerHp
6. Goal_ShouldWin
7. MaxTurn_ShouldDraw

## Notes / decisions

- Input: new Input System only (`activeInputHandler: 1`) → `Keyboard.current` in view.
- Naming: CONFIRMED (2026-07-04) — user C# coding rules override SPEC example casing everywhere,
  including public API: `state` / `isEnd` properties, `width` / `max_turn` fields.
- Coverage: CONFIRMED (2026-07-04) — deferred. 7 required tests green is the gate.
- Empty legacy folders `Assets/Scripts/{Player,Map,Enemy}` will be removed in Phase 1.
- `TestResults/` and `CodeCoverage/` must be git-ignored.
