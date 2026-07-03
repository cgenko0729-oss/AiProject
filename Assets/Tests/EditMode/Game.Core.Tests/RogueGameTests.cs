using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>
    /// Game.Core の決定論とゲームルールを検証する Edit Mode テスト。
    /// GameObject もフレーム待ちも使わない
    /// </summary>
    public sealed class RogueGameTests
    {
        /// <summary>
        /// テスト用の設定を作る
        /// </summary>
        /// <param name="width">マップの幅</param>
        /// <param name="height">マップの高さ</param>
        /// <param name="enemy_count">敵の数</param>
        /// <param name="wall_percent">内壁の確率</param>
        /// <param name="max_turn">最大ターン数</param>
        /// <returns>テスト用 GameConfig</returns>
        private static GameConfig _MakeConfig(int width = 10, int height = 10, int enemy_count = 3, int wall_percent = 15, int max_turn = 100)
        {
            return new GameConfig
            {
                width = width,
                height = height,
                enemy_count = enemy_count,
                wall_percent = wall_percent,
                max_turn = max_turn
            };
        }

        /// <summary>
        /// コマンド列をまとめて実行する
        /// </summary>
        /// <param name="game">対象のゲーム</param>
        /// <param name="commands">実行するコマンド列</param>
        private static void _Run(RogueGame game, IEnumerable<Command> commands)
        {
            foreach (Command command in commands)
            {
                game.Step(command);
            }
        }

        [Test]
        public void SameSeedAndSameCommands_ShouldCreateSameReplay()
        {
            // 同じ Seed + 同じコマンド列 → リプレイ完全一致（決定論の核心）
            var commands = new[] { Command.Right, Command.Up, Command.Right, Command.Wait, Command.Down };

            var game_a = new RogueGame(12345, _MakeConfig());
            var game_b = new RogueGame(12345, _MakeConfig());
            _Run(game_a, commands);
            _Run(game_b, commands);

            Assert.AreEqual(game_a.GetReplayText(), game_b.GetReplayText());
            // 空のリプレイ同士の一致で緑になるのを防ぐ
            Assert.IsTrue(game_a.GetReplayText().Contains("Turn=1"), "リプレイにターン記録が無い");
        }

        [Test]
        public void SameSeedAndDifferentCommands_ShouldCreateDifferentReplay()
        {
            // 同じ Seed でもコマンド列が違えばリプレイは変わる
            var game_a = new RogueGame(12345, _MakeConfig());
            var game_b = new RogueGame(12345, _MakeConfig());
            _Run(game_a, new[] { Command.Right, Command.Right, Command.Wait });
            _Run(game_b, new[] { Command.Up, Command.Right, Command.Wait });

            Assert.AreNotEqual(game_a.GetReplayText(), game_b.GetReplayText());
        }

        [Test]
        public void PlayerCannotMoveIntoWall()
        {
            // プレイヤーは (1,1) 開始。左の (0,1) は必ず外周の壁
            var game = new RogueGame(1, _MakeConfig(enemy_count: 0));
            GridPos before = game.state.player.pos;

            game.Step(Command.Left);

            Assert.AreEqual(before, game.state.player.pos, "壁に向かって移動したのに位置が変わった");
            Assert.IsTrue(game.state.map.IsWall(new GridPos(0, 1)), "外周が壁になっていない");
        }

        [Test]
        public void PlayerAttackEnemy_ShouldReduceEnemyHp()
        {
            // 敵をプレイヤーの右隣に置いて、右へ進む → 攻撃になる
            var config = _MakeConfig(enemy_count: 1, wall_percent: 0);
            var game = new RogueGame(1, config);
            GameUnit enemy = game.state.enemies[0];
            enemy.pos = new GridPos(2, 1);
            int hp_before = enemy.hp;
            GridPos player_before = game.state.player.pos;

            game.Step(Command.Right);

            Assert.AreEqual(hp_before - config.player_attack, enemy.hp, "敵のHPが攻撃力ぶん減っていない");
            Assert.AreEqual(player_before, game.state.player.pos, "攻撃時はプレイヤーは移動しないはず");
        }

        [Test]
        public void EnemyAttackPlayer_ShouldReducePlayerHp()
        {
            // 敵をプレイヤーの隣に置いて待機 → 敵の攻撃を受ける
            var config = _MakeConfig(enemy_count: 1, wall_percent: 0);
            var game = new RogueGame(1, config);
            game.state.enemies[0].pos = new GridPos(2, 1);
            int hp_before = game.state.player.hp;

            game.Step(Command.Wait);

            Assert.AreEqual(hp_before - config.enemy_attack, game.state.player.hp, "プレイヤーのHPが減っていない");
        }

        [Test]
        public void Goal_ShouldWin()
        {
            // 4x4 の壁なし・敵なしマップ。(1,1) → (2,1) → ゴール (2,2)
            var game = new RogueGame(1, _MakeConfig(width: 4, height: 4, enemy_count: 0, wall_percent: 0));

            game.Step(Command.Right);
            game.Step(Command.Up);

            Assert.AreEqual(GameResult.Win, game.state.result);
            Assert.IsTrue(game.isEnd);
        }

        [Test]
        public void MaxTurn_ShouldDraw()
        {
            // 最大ターンまで待機し続けると引き分け
            var game = new RogueGame(1, _MakeConfig(enemy_count: 0, max_turn: 3));

            game.Step(Command.Wait);
            game.Step(Command.Wait);
            Assert.AreEqual(GameResult.Playing, game.state.result, "最大ターン前に終了している");

            game.Step(Command.Wait);

            Assert.AreEqual(GameResult.Draw, game.state.result);
            Assert.IsTrue(game.isEnd);
            Assert.AreEqual(3, game.state.turn, "ターン数が想定と違う");
        }
    }
}
