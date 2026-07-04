# WorkAiDoInProject.md — AI（Claude Code）が本プロジェクトで行った全作業記録

作成日: 2026-07-04
課題: 固定シード式ターン制ローグライク（Unity 3D ダンジョン表示）
リポジトリ: https://github.com/cgenko0729-oss/AiProject

本書は、このセッションで AI が実際に行った作業を、開始から完了まで時系列で詳細に記録したものである。
失敗した手順・その原因・修正方法も含めて省略せずに書く。

---

## 全体の流れ（サマリー）

| 順序 | フェーズ | 内容 | ブランチ | 結果 |
|---|---|---|---|---|
| 1 | Phase 0 | 要件読解・Skill/Hook/計画の整備 | main 直接 | commit `bff1687` |
| 2 | Phase 1 | フォルダと asmdef 構成 | feature/phase1-structure | commit `a6be574` → merge |
| 3 | Phase 2 | Core スケルトン（8ファイル） | feature/phase2-core-skeleton | commit `669d2f3` → merge |
| 4 | Phase 3 | テスト先行（7テスト・赤確認） | feature/phase3-tests | commit `8960959` → merge |
| 5 | Phase 4 | Core ロジック実装（7/7 緑） | feature/phase4-core-logic | commit `1dc34e8` → merge |
| 6 | Phase 5 | 3D 表示（RogueGameView） | feature/phase5-presentation | commit `91a6d34` → merge |
| 7 | Phase 6+7 | 制約総点検・シーン設定・最終レビュー | feature/phase6-review | commit `8f334a7` → merge |

最終的に main の HEAD は `fff7b02`。全ブランチをリモートへ push 済み。

---

## Phase 0 — 要件読解と開発基盤の整備

### 0-1. 要件ドキュメントの読解

以下を全て読んだ。

1. `Assets/Document/AI課題（Unity）_0630.pdf`（6ページ全部）
   - 課題の本質は「Claude Code を機械的制約で矯正しながら使う」こと
   - 決定論ルール：同一 Seed + 同一 Command 列 = 同一リプレイ
   - 制約A〜F（グローバル乱数禁止 / 決定論テスト常緑 / マジックナンバー禁止 /
     例外握り潰し禁止 / カバレッジ90% / コア層 Unity 非依存）
   - 最重要の初手 = `Game.Core.asmdef` を `noEngineReferences: true` で先に作ること
2. `Assets/CLAUDE.md` — KISS 版プロジェクト指示
3. `Assets/SPEC.md` — ゲーム仕様の詳細（クラス設計・ターン処理・マップ生成・リプレイ形式・必須7テスト）
4. `Assets/SKILL.md` — 作業スキル定義（禁止事項・作業手順・拒否プロンプト）
5. `Assets/PROMPT_FOR_CLAUDE_CODE.md` — Phase 0〜7 の段階別プロンプトと修理プロンプト
6. `Assets/START_HERE.md` — 進め方（一括依頼禁止・フェーズ分割）

### 0-2. プロジェクト現状の調査

実行したコマンドと判明事項:

- `ProjectSettings/ProjectVersion.txt` → Unity **6000.0.62f1**（URP）
- `Packages/manifest.json` → Test Framework 1.6.0 あり / Code Coverage パッケージなし /
  Input System 1.14.2 あり
- `ProjectSettings/ProjectSettings.asset` の `activeInputHandler: 1`
  → **新 Input System のみ有効**。旧 `Input.GetKeyDown` は実行時エラーになるため
  Presentation は `Keyboard.current` を使う必要があると判断（SPEC の例は旧 Input だが
  プロジェクト設定を優先）
- `Assets/Scripts/` に空の `Player/` `Map/` `Enemy/` フォルダ（後で削除対象）
- `git remote -v` → origin = github.com/cgenko0729-oss/AiProject、ブランチは main のみ
- Unity エディタ本体は `C:\Program Files\Unity\Hub\Editor\6000.0.62f1\` に存在確認

### 0-3. 作成したファイル（開発基盤）

1. **`CLAUDE.md`（プロジェクトルート）**
   - 要件ドキュメントの索引、レイヤー構成表、Core 禁止 API 一覧
   - ユーザー指定の C# コーディング規約表（snake_case メンバー変数、
     camelCase アクセサ、`_PascalCase` プライベートメソッド等）
   - 新 Input System の注意、Unity のパス、Git 運用ルール、フェーズ報告書式

2. **`.claude/skills/core-dev/SKILL.md`**
   - Game.Core を書くときの絶対制約（禁止 API、乱数は Seed 注入のみ、
     Step が唯一の進行入口、順序不定コレクション禁止）
   - 固定クラスリスト、RogueGame の公開 API 形状、ターン処理手順、
   - マップ生成手順、リプレイ書式、命名規約の実例

3. **`.claude/skills/constraint-check/SKILL.md`**
   - フェーズ完了ごと・マージ前に回す 6 項目の機械チェック手順
     （禁止 API grep / asmdef 純度 / テスト品質 / Presentation 純度 / KISS / テスト実行）

4. **`.claude/skills/unity-test/SKILL.md`**
   - Unity バッチモードでの Edit Mode テスト実行方法
   - エディタを閉じる必要がある等の注意点、結果 XML の読み方

5. **`.claude/hooks/check-core.ps1`（PostToolUse フック）**
   - Game.Core 内の全 .cs を 13 種の禁止パターン
     （`using UnityEngine` / `MonoBehaviour` / `GameObject` / `Transform` /
     `Vector2` / `Vector2Int` / `Vector3` / `Time.` / `Rigidbody` / `Collider` /
     `UnityEngine.Random` / `Random.Range` / `Random.value`）で走査
   - `Game.Core.asmdef` の `noEngineReferences: true` も検証
   - 違反があれば exit 2 で stderr を AI に突き返す（自動矯正装置）

6. **`.claude/settings.json`**
   - Edit / Write ツール実行のたびに上記フックを自動起動する設定

7. **`tools/run-tests.ps1`**
   - Unity をバッチモードで起動して Edit Mode テストを実行、
     NUnit XML を解析して失敗テスト名を表示、失敗時 exit 1
   - `-coverage` オプションで課題 PDF 6章のコマンド形式
     （`-debugCodeOptimization -enableCodeCoverage -coverageOptions ...`）を付加し、
     Game.Core の行カバレッジ 90% 未満なら fail する仕組みも実装

8. **`PLAN.md`** — フェーズ計画・ブランチ戦略・機械的ゲート一覧・進捗チェックボックス

### 0-4. フックの動作検証（失敗と修正を含む）

- **失敗1**: 初版 check-core.ps1 は日本語コメント入りの UTF-8（BOM なし）で保存
  → PowerShell 5.1 が文字化けしてパースエラー（exit 1）
  → **修正**: スクリプトを ASCII コメントのみで書き直し
- **検証**: わざと `MonoBehaviour` / `Random.Range` / `Time.deltaTime` を含む
  `Bad.cs` を Game.Core に置いてフックを実行
  → 違反 4 件を行番号付きで正しく検出（exit 2）。テスト用ファイルは削除
- Core フォルダが無い状態では exit 0（正常通過）も確認

### 0-5. ユーザーへの質問と決定事項

AskUserQuestion で 2 点を確認:

1. **命名規約の衝突**: SPEC の例は `State` / `IsEnd` / `Width`（PascalCase）だが、
   ユーザー規約はアクセサ camelCase・public フィールド snake_case
   → **決定: 全面的にユーザー規約を採用**（`state` / `isEnd` / `width` / `max_turn`）
2. **カバレッジ 90%（制約E）の扱い**
   → **決定: 今回は見送り**（必須7テストの緑をゲートとする）。
   計測スクリプト自体は tools/run-tests.ps1 に残置

### 0-6. コミットと push（トラブルあり）

- `.gitignore` に `TestResults/` `CodeCoverage/` を追加
- 要件ドキュメント（PDF 含む）・基盤ファイル一式をコミット → `bff1687`
- **失敗2**: `git push` が 401 Unauthorized
  （Bash ツールのサンドボックスでは Git Credential Manager が対話できない）
  → ユーザーに一度手動 push を依頼 → 認証確立
- **後の発見**: push は **PowerShell ツール経由なら成功する**。以後 push は全て
  PowerShell で実行（この知見はメモリファイルにも保存）

---

## Phase 1 — フォルダと asmdef 構成（ブランチ: feature/phase1-structure）

### 1-1. 作成した asmdef（3つ）

1. `Assets/Scripts/Game.Core/Game.Core.asmdef`
   - `"noEngineReferences": true`、references 空
   - → Core に Unity 型が入った瞬間コンパイルエラーになる最強の制約（制約F）
2. `Assets/Scripts/Game.Presentation/Game.Presentation.asmdef`
   - references: `Game.Core`, `Unity.InputSystem`
3. `Assets/Tests/EditMode/Game.Core.Tests/Game.Core.Tests.asmdef`
   - `includePlatforms: ["Editor"]`（Edit Mode 専用）
   - references: `Game.Core`, TestRunner 系
   - `overrideReferences: true` + `nunit.framework.dll`
   - `defineConstraints: ["UNITY_INCLUDE_TESTS"]`

### 1-2. 旧フォルダ削除

空の `Assets/Scripts/Player` `Map` `Enemy`（+ .meta）を削除。

### 1-3. コンパイル検証（失敗と回避）

- **失敗3**: `Unity.exe -batchmode -quit` で検証しようとしたが
  「It looks like another Unity instance is running with this project open.」
  → **ユーザーがエディタでプロジェクトを開いているためバッチモード不可**
- **回避**: ユーザーに Unity ウィンドウをフォーカスしてもらい、
  エディタ側の自動インポートで .meta 生成とコンパイルを実施
  → 約 20 秒後に 3 つの asmdef.meta と フォルダ .meta の生成を確認、エラーなし

### 1-4. チェックとマージ

- check-core.ps1 → PASS
- コミット `a6be574` → main へ `--no-ff` マージ → push（PowerShell 経由で成功 `b6f53c3`）

---

## Phase 2 — Core スケルトン（ブランチ: feature/phase2-core-skeleton）

### 2-1. 作成した 8 ファイル（すべて純 C#・日本語コメント・ユーザー命名規約）

1. `GameEnums.cs` — `Command`(Up/Down/Left/Right/Wait), `TileType`(Floor/Wall/Goal),
   `GameResult`(Playing/Win/Lose/Draw)
2. `GridPos.cs` — readonly struct。`x` `y`（int）、`Next(command)`（1マス先の座標）、
   `IsNextTo`（上下左右隣接判定）、`Equals` / `GetHashCode`、
   リプレイ用の安定した `ToString()` = `"(x,y)"`。この段階で完全実装
3. `GameConfig.cs` — 全調整値: `width=10, height=10, max_turn=100, player_hp=10,
   player_attack=2, enemy_count=3, enemy_hp=3, enemy_attack=1, wall_percent=15`
4. `GameUnit.cs` — プレイヤーと敵の共用クラス（`id`, `is_player`, `pos`, `hp`,
   アクセサ `isAlive`）
5. `GameMap.cs` — `TileType[,]` 保持。`GetTile` / `SetTile` / `IsInside` / `IsWall` /
   `IsWalkable` / `IsGoal`。マップ外は Wall 扱い。この段階で完全実装
6. `GameState.cs` — `turn`, `result`, `player`, `enemies`（必ずID昇順のList）, `map`
7. `Replay.cs` — StringBuilder でヘッダー `Seed=<seed>` と
   `Turn=.. Command=.. Result=.. Player=(x,y):hp Enemies=[id:(x,y):hp,...]` 行を蓄積。
   敵はID昇順で記録（決定論保証）。この段階で完全実装
8. `RogueGame.cs` — コンストラクタで `System.Random(seed)` を生成・保持。
   `state` / `isEnd` アクセサ、`Step()` は TODO スタブ、`GetReplayText()` 実装

### 2-2. Unity なしでのコンパイル検証（失敗と改良）

- **失敗4**: .NET Framework 付属の古い csc.exe（C# 5）でコンパイル
  → `=>`（expression-bodied member）や readonly struct が構文エラー。
  さらに `/` 区切りパスをオプションと誤認する問題もあった
- **改良**: dotnet SDK 9.0.308 を発見 → `netstandard2.1` / `LangVersion 9.0` の
  csproj を作って `dotnet build` → **0 Warning / 0 Error**
- この方法を常設ツール化: **`tools/corecheck/corecheck.csproj`**
  （`dotnet build` だけで Unity なしに Core を検証できる）

### 2-3. チェックとマージ

- check-core.ps1 → PASS（8ファイル全部クリーン）
- Unity エディタ側でも .meta 自動生成＝インポート成功を確認
- コミット `669d2f3` → merge → push（`b2166b1`）
- メモリファイル `git-push-use-powershell.md` を保存
  （push は PowerShell / エディタ開放中はバッチ不可という環境知見）

---

## Phase 3 — テスト先行・TDD 赤確認（ブランチ: feature/phase3-tests）

### 3-1. `RogueGameTests.cs` に必須 7 テストを作成

| テスト | 検証方法 |
|---|---|
| SameSeedAndSameCommands_ShouldCreateSameReplay | 同 Seed 2 ゲームに同一 5 コマンド → `GetReplayText()` 完全一致 + `Turn=1` 含有（空リプレイ同士の偽陽性防止） |
| SameSeedAndDifferentCommands_ShouldCreateDifferentReplay | 同 Seed でコマンド列を変えて不一致を Assert |
| PlayerCannotMoveIntoWall | (1,1) から左 = 外周壁 (0,1) へ移動 → 位置不変 + (0,1) が壁であることも Assert |
| PlayerAttackEnemy_ShouldReduceEnemyHp | 敵を (2,1) に配置して右へ → 敵HPが `player_attack` ぶん減少 + プレイヤー位置不変 |
| EnemyAttackPlayer_ShouldReducePlayerHp | 敵を隣接配置して Wait → プレイヤーHPが `enemy_attack` ぶん減少 |
| Goal_ShouldWin | 4x4・壁0%・敵0 → Right, Up でゴール (2,2) → Win + isEnd |
| MaxTurn_ShouldDraw | max_turn=3 で Wait×3 → 2手目まで Playing、3手目で Draw + turn==3 |

設計上の工夫:
- テストヘルパーは `_MakeConfig()`（設定生成）と `_Run()`（コマンド一括実行）の 2 つだけ
- `[Test]` のみ使用。`[UnityTest]` / GameObject / シーン / フレーム待ちは不使用
- 「敵を隣に置く」は GameState の public フィールドを直接書き換える方式
  （SPEC 10.4「put enemy beside player」の KISS 実現）

### 3-2. Unity なしでテストを実行できる環境を構築

- **`tools/testcheck/testcheck.csproj`** を作成
  （net9.0 + NUnit 3.13.3 + NUnit3TestAdapter + Microsoft.NET.Test.Sdk、
  Core とテストのソースを直接 Compile Include）
- `dotnet test` 実行 → **7 failed / 0 passed** — Step がスタブなので全部赤。
  **TDD の「実装前は赤」を機械的に証明**

### 3-3. gitignore の落とし穴（失敗と修正）

- **失敗5**: コミット後に `tools/testcheck/testcheck.csproj` が入っていないことを発見
  → Unity 標準 .gitignore の全域 `*.csproj` ルール（Unity が生成する csproj 除外用）が
  自作ツールの csproj まで無視していた（Phase 2 の corecheck.csproj も同様に漏れていた）
- **修正**: `.gitignore` に `!tools/**/*.csproj` の否定ルールを追加し、
  両 csproj を追加コミット（`50c5fb9`）

### 3-4. マージ

- コミット `8960959` → merge → push

---

## Phase 4 — Core ロジック実装・7/7 緑（ブランチ: feature/phase4-core-logic）

### 4-1. `RogueGame.cs` の完全実装

**定数（マジックナンバー排除）**: `_PLAYER_ID = -1`, `_MAX_SPAWN_TRY = 1000`,
`_PERCENT_MAX = 100`

**マップ生成 `_CreateMap()`**:
1. 全マス Floor（enum 既定値）
2. 外周を Wall
3. 内側は x→y の固定ループ順で `random.Next(100) < wall_percent` なら Wall
   （ループ順固定 = 乱数消費順固定 = 決定論維持）
4. スポーン (1,1) とゴール (width-2, height-2) には壁を置かない
5. 最後にゴールタイルを設定

**敵配置 `_SpawnEnemies()` / `_FindEnemySpawn()`**:
- ID 0 から昇順で生成
- 空きマス探索は乱数で最大 1000 回試行。歩行可 / ゴール外 / プレイヤー位置外 /
  他敵と非重複を満たすマスに配置
- 見つからない場合は例外を投げる（制約D: 握り潰し禁止に従い隠さない）

**ターン処理 `Step(command)`**（SPEC 7章の順序通り）:
1. 終了済みなら現状態を返す
2. `turn++`
3. `_PlayerAct`: Wait なら何もしない / 移動先に生存敵がいれば攻撃して移動しない /
   歩行可能なら移動 / 壁なら何もしない
4. `_EnemiesAct`: 生存敵をID昇順で処理。プレイヤーに隣接なら攻撃、
   そうでなければ `_MoveEnemyTowardPlayer`
5. `_CheckResult`: 死亡(Lose) → ゴール(Win) → 最大ターン(Draw) の優先順
6. `_replay.AddTurn` で記録
7. 状態を返す

**敵の移動 `_MoveEnemyTowardPlayer`**（SPEC のシンプル規則）:
- |dx| と |dy| を比較して大きい軸を先に 1 マス試行（同値なら横優先で順序固定）
- 塞がれていればもう片方の軸、両方無理なら待機
- 移動先条件 `_CanEnemyMoveTo`: 歩行可能 / プレイヤーのマス不可 / 他の生存敵のマス不可

**その他**:
- `_Attack`: HP を減らし 0 未満にはしない（リプレイの見た目安定）
- `_FindAliveEnemyAt`: ID昇順リストを走査（順序不定コレクション不使用）
- A* なし、インターフェイスなし、デザインパターンなし（KISS 遵守）

### 4-2. 検証

- `dotnet test` → **7 passed / 0 failed（11ms）** — 赤→緑の完了
- check-core.ps1 → PASS
- コミット `1dc34e8` → merge → push
  （push 1 回目がネットワークでタイムアウト、再実行で成功 `1bf4aea`）

---

## Phase 5 — 3D 表示（ブランチ: feature/phase5-presentation）

### 5-1. `RogueGameView.cs`（唯一の MonoBehaviour）

**Start()**:
- `new RogueGame(seed, new GameConfig())` を生成（seed はインスペクタ公開、既定 12345）
- `_BuildBoard()`: マップを読んで
  壁 = 灰色キューブ / 床 = 平たい暗色キューブ / ゴール = 緑の低いキューブを生成
- `_BuildUnits()`: プレイヤー = 青カプセル、敵 = 赤カプセル（ID順のリストで保持）
- `_SetupCamera()`: Camera.main を盤面中央上空へ自動配置して LookAt
- 生成した全プリミティブの **Collider は即 Destroy**（物理をゲームに関与させない意思表示）

**Update()**:
- `Keyboard.current`（新 Input System）で W/S/A/D/Space の
  `wasPressedThisFrame` だけを読む → 押されたフレームのみ `Step(command)` を 1 回呼ぶ
- **毎フレーム自動進行は一切しない**（ターン制の維持）

**表示更新**:
- `_RefreshUnits()`: GameState の座標をワールド座標（x→X, y→Z）へ変換して反映。
  死んだ敵は SetActive(false)
- `_ShowResultOnce()`: 終了時に一度だけ Debug.Log
- `OnGUI()`: Turn / HP / Result と操作説明のラベル表示（表示のみ、判定なし）

**含まれないもの（Presentation 純度）**:
ダメージ計算 / 勝敗判定 / マップ生成 / ゲーム用乱数 / OnTriggerEnter / OnCollisionEnter /
Time.deltaTime による進行 — すべてなし

### 5-2. Unity なしでのコンパイル検証

- 一時 csproj で `UnityEngine.dll`（エディタ付属）+
  `Library/ScriptAssemblies/Unity.InputSystem.dll` + `Game.Core.dll` を参照して
  `dotnet build` → **0 Warning / 0 Error**
- 検証後に一時ファイルは削除

### 5-3. マージとユーザー確認依頼

- コミット `91a6d34` → merge → push（`5be3845`）
- ユーザーへ「シーンに空オブジェクトを作って RogueGameView を付けて Play」
  「Test Runner で EditMode 実行」を依頼 → **ユーザーが動作確認、正常動作を報告**

---

## Phase 6 — 制約総点検 + .meta 補完（ブランチ: feature/phase6-review）

### 6-1. .meta の補完

- Unity エディタが生成した `RogueGameView.cs.meta` と `RogueGameTests.cs.meta` を
  ステージ（Phase 3・5 の時点ではエディタ未リフレッシュのため未生成だった）

### 6-2. シーンへの組み込み（YAML 直接編集）

- `SampleScene.unity` に `Game` オブジェクトが無いことを grep で確認
- `RogueGameView.cs.meta` から script GUID `93951640dde2fca48a7f020904d5f53e` を取得
- シーン YAML に以下を手書きで追記:
  - GameObject（fileID 1000000001, 名前 "Game"）
  - Transform（fileID 1000000002）
  - MonoBehaviour（fileID 1000000003, m_Script = 上記 GUID, `seed: 12345`）
  - `SceneRoots.m_Roots` に Transform を追加
- → クローン直後でもシーンを開いて Play するだけで遊べる状態にした
- ユーザーへ「エディタで旧シーンを上書き保存せず、ダブルクリックで再読込」を案内

### 6-3. 制約チェック一括実行（すべて PASS）

| チェック | 方法 | 結果 |
|---|---|---|
| Core 禁止 API | check-core.ps1 + 手動 grep（二重確認） | 違反 0 |
| Presentation の物理・乱数・Time | grep（OnTriggerEnter / OnCollisionEnter / Rigidbody / UnityEngine.Random / Random.Range / Random.value / Time.deltaTime） | 違反 0 |
| asmdef 純度 | Game.Core.asmdef の references 空 + noEngineReferences: true | OK |
| 空 catch / 例外握り潰し | grep `catch {}` 系 | 0 件 |
| クラス数（KISS） | public 型を全列挙 → Core 7 クラス + 3 enum + View 1 + Tests 1 で SPEC と完全一致 | OK |
| テスト再実行 | dotnet test | 7/7 緑 |
| コード規模 | wc -l → 全体約 1,000 行（コメント込み） | KISS 維持 |

### 6-4. 気付き事項の整理

- `GridPos.GetHashCode` の乗数 397 は慣用のハッシュ係数であり
  ゲーム調整値ではないため GameConfig 集約の対象外と判断（FINAL_REVIEW に明記）
- マップ生成はゴール到達可能性を保証しない（Seed によっては詰み得る）
  → KISS 仕様の割り切りとして既知の制限に記載

---

## Phase 7 — 最終レビュー（同ブランチで実施）

### 7-1. `FINAL_REVIEW.md` を作成（提出用・日本語）

内容:
- 最終ファイル構成ツリー
- テスト 7 件の一覧表と結果（7 passed / 0 failed）
- テスト実行方法 3 通り（Unity Test Runner / tools/run-tests.ps1 / dotnet test）
- 遊び方（WASD・Space、勝敗条件、Seed 変更方法）
- 既知の制限 4 点
- 提出前チェックリスト 18 項目 → **全て ✅**

### 7-2. 進捗更新とマージ

- PLAN.md の Phase 6・7 をチェック済みに更新
- コミット `8f334a7` → main へ `--no-ff` マージ → push（main = `fff7b02`）

---

## 発生した問題と解決の一覧（トラブルシューティング記録）

| # | 問題 | 原因 | 解決 |
|---|---|---|---|
| 1 | check-core.ps1 がパースエラー | PowerShell 5.1 は BOM なし UTF-8 の日本語コメントを誤読 | スクリプトを ASCII のみで書き直し |
| 2 | git push が 401 | Bash ツール内では Git Credential Manager が対話不可 | 初回はユーザーが手動 push、以後は PowerShell ツール経由で push |
| 3 | Unity バッチモード起動失敗 | ユーザーのエディタがプロジェクトを開いたまま | エディタのフォーカスで自動インポートさせる方式に切替。CLI テストは dotnet test で代替 |
| 4 | 旧 csc.exe でコンパイル不能 | .NET Framework 4.0 の csc は C# 5 まで | dotnet SDK (C# 9) の csproj 方式へ変更、tools/corecheck として常設 |
| 5 | 自作ツールの csproj が git に入らない | Unity 用 .gitignore の全域 `*.csproj` ルール | `!tools/**/*.csproj` の否定ルールを追加して再コミット |
| 6 | push が 2 分でタイムアウト（1回） | 一時的なネットワーク遅延 | 再実行で成功 |

---

## 作成・変更した全ファイル一覧

### ゲーム本体
- `Assets/Scripts/Game.Core/Game.Core.asmdef`
- `Assets/Scripts/Game.Core/GameEnums.cs`
- `Assets/Scripts/Game.Core/GridPos.cs`
- `Assets/Scripts/Game.Core/GameConfig.cs`
- `Assets/Scripts/Game.Core/GameUnit.cs`
- `Assets/Scripts/Game.Core/GameMap.cs`
- `Assets/Scripts/Game.Core/GameState.cs`
- `Assets/Scripts/Game.Core/Replay.cs`
- `Assets/Scripts/Game.Core/RogueGame.cs`
- `Assets/Scripts/Game.Presentation/Game.Presentation.asmdef`
- `Assets/Scripts/Game.Presentation/RogueGameView.cs`
- `Assets/Tests/EditMode/Game.Core.Tests/Game.Core.Tests.asmdef`
- `Assets/Tests/EditMode/Game.Core.Tests/RogueGameTests.cs`
- `Assets/Scenes/SampleScene.unity`（Game オブジェクト追記）
- 各種 .meta（Unity 生成分をコミット）

### 開発基盤・ドキュメント
- `CLAUDE.md`（ルート）
- `PLAN.md`
- `FINAL_REVIEW.md`
- `WorkAiDoInProject.md`（本書）
- `.claude/settings.json`
- `.claude/hooks/check-core.ps1`
- `.claude/skills/core-dev/SKILL.md`
- `.claude/skills/constraint-check/SKILL.md`
- `.claude/skills/unity-test/SKILL.md`
- `tools/run-tests.ps1`
- `tools/corecheck/corecheck.csproj`
- `tools/testcheck/testcheck.csproj`
- `.gitignore`（TestResults / coverage / tools の bin・obj / csproj 否定ルール追記）

### 削除
- `Assets/Scripts/Player/` `Assets/Scripts/Map/` `Assets/Scripts/Enemy/`（空フォルダ + .meta）

---

## Git 履歴（main）

```text
fff7b02  Merge feature/phase6-review: final constraint check and review
8f334a7  Phase 6+7: constraint review, scene setup, final review document
5be3845  Merge feature/phase5-presentation: 3D view
91a6d34  Phase 5: add RogueGameView 3D presentation (single MonoBehaviour)
1bf4aea  Merge feature/phase4-core-logic: deterministic Core complete
1dc34e8  Phase 4: implement Core logic, all 7 tests green
50c5fb9  Fix: un-ignore tools/*.csproj (Unity's global *.csproj rule hid them)
（merge） Merge feature/phase3-tests: 7 required tests (red)
8960959  Phase 3: add 7 required Edit Mode tests (TDD red confirmed)
b2166b1  Merge feature/phase2-core-skeleton: Core skeleton
669d2f3  Phase 2: create Game.Core skeleton (8 files, pure C#)
b6f53c3  Merge feature/phase1-structure: layer folders and asmdef
a6be574  Phase 1: create layer folders and asmdef structure
bff1687  Phase 0: add requirement docs, Claude Code skills/hooks, test runner script and plan
eca7c71  InitProject
ccc9d8f  Initial commit
```

リモートに保存されているブランチ:
`main`, `feature/phase1-structure`, `feature/phase2-core-skeleton`,
`feature/phase3-tests`, `feature/phase4-core-logic`,
`feature/phase5-presentation`, `feature/phase6-review`

---

## 課題 PDF の完成判定との対応（最終状態）

| PDF の完成判定 | 状態 |
|---|---|
| 決定論テスト（同一シードでリプレイ一致）がグリーン | ✅ テスト1（+ユーザーがエディタで動作確認） |
| ゴールデンテスト（勝敗・ターン数固定）がグリーン | ✅ テスト6・7（Win / Draw とターン数を固定値で Assert） |
| コア層カバレッジ 90% 以上 | ⏸ ユーザー決定により見送り（計測スクリプトは tools/run-tests.ps1 -coverage に準備済み） |
| 制約A・C・D の違反ゼロ | ✅ フック + grep で機械確認 |
| コア層 .asmdef が UnityEngine 非参照（制約F） | ✅ noEngineReferences: true |
| 勝敗ロジックが物理・Time・UnityEngine.Random に非依存 | ✅ 整数 GridPos 演算のみ |
