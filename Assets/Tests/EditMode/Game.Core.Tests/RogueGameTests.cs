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
        /// <param name="max_turn">最大ターン数</param>
        /// <returns>テスト用 GameConfig</returns>
        private static GameConfig _MakeConfig(int width = 20, int height = 20, int enemy_count = 3, int max_turn = 100)
        {
            return new GameConfig
            {
                width = width,
                height = height,
                enemy_count = enemy_count,
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

        /// <summary>
        /// マップからゴールの座標を探す
        /// </summary>
        /// <param name="map">対象マップ</param>
        /// <returns>ゴール座標</returns>
        private static GridPos _FindGoal(GameMap map)
        {
            for (int x = 0; x < map.width; x++)
            {
                for (int y = 0; y < map.height; y++)
                {
                    var pos = new GridPos(x, y);
                    if (map.IsGoal(pos)) return pos;
                }
            }
            Assert.Fail("マップにゴールが無い");
            return default;
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
            // 左へ歩き続ければ必ずどこかの壁で止まる（BSP マップでも成立する）
            var config = _MakeConfig(width: 12, height: 12, enemy_count: 0);
            var game = new RogueGame(1, config);
            for (int i = 0; i < config.width; i++)
            {
                game.Step(Command.Left);
            }

            GridPos before = game.state.player.pos;
            Assert.IsTrue(game.state.map.IsWall(before.Next(Command.Left)), "左隣が壁になっていない");

            game.Step(Command.Left);

            Assert.AreEqual(before, game.state.player.pos, "壁に向かって移動したのに位置が変わった");
        }

        [Test]
        public void PlayerAttackEnemy_ShouldReduceEnemyHp()
        {
            // 敵をプレイヤーの右隣に置いて、右へ進む → 攻撃になる
            var config = _MakeConfig(enemy_count: 1);
            var game = new RogueGame(1, config);
            GameUnit enemy = game.state.enemies[0];
            enemy.pos = game.state.player.pos.Next(Command.Right);
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
            var config = _MakeConfig(enemy_count: 1);
            var game = new RogueGame(1, config);
            game.state.enemies[0].pos = game.state.player.pos.Next(Command.Right);
            int hp_before = game.state.player.hp;

            game.Step(Command.Wait);

            Assert.AreEqual(hp_before - config.enemy_attack, game.state.player.hp, "プレイヤーのHPが減っていない");
        }

        [Test]
        public void Goal_ShouldWin()
        {
            // 6x6 は分割されず部屋が1つだけになるので、ゴールへまっすぐ歩ける
            var game = new RogueGame(1, _MakeConfig(width: 6, height: 6, enemy_count: 0));
            GridPos goal = _FindGoal(game.state.map);

            // ゴールへ向かって1マスずつ歩く（安全のため上限あり）
            int safety = 100;
            while (!game.isEnd && safety-- > 0)
            {
                GridPos pos = game.state.player.pos;
                if (pos.x < goal.x) game.Step(Command.Right);
                else if (pos.x > goal.x) game.Step(Command.Left);
                else if (pos.y < goal.y) game.Step(Command.Up);
                else if (pos.y > goal.y) game.Step(Command.Down);
                else break;
            }

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
