# FINAL_REVIEW.md — 提出前最終レビュー / Final Review

課題: 固定シード式ターン制ローグライク（Unity 3D ダンジョン表示）
日付: 2026-07-04

## 1. 最終ファイル構成

```text
Assets/
  Scripts/
    Game.Core/                 (純粋C#・UnityEngine 非参照)
      Game.Core.asmdef         noEngineReferences: true
      GameEnums.cs             Command / TileType / GameResult
      GridPos.cs               整数グリッド座標 (readonly struct)
      GameConfig.cs            全調整値（マジックナンバー禁止の受け皿）
      GameUnit.cs              プレイヤー・敵 共用ユニット
      GameMap.cs               タイルグリッドと歩行可否
      GameState.cs             ターン・結果・ユニット・マップ
      Replay.cs                安定リプレイ文字列（敵はID昇順）
      RogueGame.cs             本体。Step(Command) が唯一の進行入口
    Game.Presentation/
      Game.Presentation.asmdef Game.Core + Unity.InputSystem を参照
      RogueGameView.cs         唯一の MonoBehaviour（入力変換と描画のみ）
  Tests/
    EditMode/
      Game.Core.Tests/
        Game.Core.Tests.asmdef Editor 専用・NUnit 参照
        RogueGameTests.cs      必須7テスト
  Scenes/
    SampleScene.unity          "Game" オブジェクト（RogueGameView, seed=12345）設定済み
tools/
  run-tests.ps1                Unity バッチモードで Edit Mode テスト実行
  corecheck/                   Unity なしで Core をコンパイル検証 (dotnet build)
  testcheck/                   Unity なしでテスト実行 (dotnet test)
.claude/
  hooks/check-core.ps1         禁止 API の自動検出（編集ごとに実行）
```

## 2. テスト一覧（すべて実 Assert あり）

| # | テスト | 検証内容 |
|---|---|---|
| 1 | SameSeedAndSameCommands_ShouldCreateSameReplay | 決定論の核心。リプレイ文字列完全一致 |
| 2 | SameSeedAndDifferentCommands_ShouldCreateDifferentReplay | コマンド差でリプレイが変わる |
| 3 | PlayerCannotMoveIntoWall | 壁への移動は位置不変 |
| 4 | PlayerAttackEnemy_ShouldReduceEnemyHp | 攻撃力ぶんHP減少・攻撃時は移動しない |
| 5 | EnemyAttackPlayer_ShouldReducePlayerHp | 隣接する敵の攻撃 |
| 6 | Goal_ShouldWin | ゴール到達で Win |
| 7 | MaxTurn_ShouldDraw | 最大ターンで Draw・ターン数一致 |

結果: **7 passed / 0 failed**（dotnet test で確認済み）

## 3. テストの実行方法

- Unity 内: `Window > General > Test Runner > EditMode > Run All`
- CLI（Unity を閉じてから）: `powershell -File tools/run-tests.ps1`
- 高速確認（Unity 不要）: `cd tools/testcheck && dotnet test`

## 4. 遊び方

1. `SampleScene` を開いて Play。
2. WASD = 移動（敵の方向へ押すと攻撃）、Space = 待機。1キー = 1ターン。
3. 緑のゴールに到達で Win、HP 0 で Lose、100 ターンで Draw。
4. Seed はシーンの `Game` オブジェクトのインスペクタで変更できる。

## 5. 既知の制限

- 敵AIは「隣接なら攻撃、それ以外は1マス接近（大きい軸優先）」のみ。A* なし（仕様通り）。
- マップ生成は確率壁のみで、ゴールへの到達可能性は保証しない（Seed により詰みがありうる。KISS 仕様の割り切り）。
- 表示は Unity プリミティブのみ。アニメーションなし。
- GridPos.GetHashCode の乗数 397 は慣用値（ゲーム調整値ではないため GameConfig 対象外）。

## 6. 最終チェックリスト

| 項目 | 結果 |
|---|---|
| 3D ダンジョン風の小さいバトルゲーム | ✅ |
| ターン制・グリッド制 | ✅ |
| WASD は Command 変換のみ | ✅ |
| 同一 Seed + 同一 Command 列 = 同一リプレイ | ✅ テスト1 |
| Game.Core に UnityEngine なし | ✅ asmdef + hook + grep |
| Game.Core に MonoBehaviour なし | ✅ |
| Game.Core に Time.deltaTime なし | ✅ |
| Game.Core に Rigidbody / Collider なし | ✅ |
| Game.Core に UnityEngine.Random なし | ✅ System.Random(seed) 注入 |
| 進行は RogueGame.Step(Command) のみ | ✅ |
| 戦闘は GridPos 整数ロジック | ✅ |
| テストは Edit Mode ([Test] のみ) | ✅ |
| テストに実 Assert | ✅ |
| KISS（クラス数最小） | ✅ Core 7 + View 1 + Tests 1 |
| 関数・変数はシンプルな英語 | ✅ |
| コメントはシンプルな日本語 | ✅ |
| 空 catch / 例外握り潰しなし | ✅ grep 確認 |
| 数値は GameConfig に集約 | ✅ |
