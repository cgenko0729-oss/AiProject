namespace Game.Core
{
    /// <summary>
    /// ゲーム本体。Step(command) だけでターンが進む決定論的なコアクラス
    /// </summary>
    public sealed class RogueGame
    {
        /// <summary>プレイヤーのユニットID</summary>
        private const int _PLAYER_ID = -1;

        /// <summary>敵配置の再試行上限（無限ループ防止）</summary>
        private const int _MAX_SPAWN_TRY = 1000;

        /// <summary>確率計算に使うパーセントの最大値</summary>
        private const int _PERCENT_MAX = 100;

        /// <summary>Seed から作った唯一の乱数源</summary>
        private readonly System.Random _random;

        /// <summary>ゲームの調整値</summary>
        private readonly GameConfig _config;

        /// <summary>リプレイ記録</summary>
        private readonly Replay _replay;

        /// <summary>現在のゲーム状態</summary>
        private readonly GameState _state;

        /// <summary>現在のゲーム状態を返す</summary>
        public GameState state => _state;

        /// <summary>ゲームが終了したかどうか</summary>
        public bool isEnd => _state.result != GameResult.Playing;

        /// <summary>
        /// Seed と設定からゲームを作る。マップと敵配置もここで決まる
        /// </summary>
        /// <param name="seed">乱数の Seed</param>
        /// <param name="config">ゲームの調整値</param>
        public RogueGame(int seed, GameConfig config)
        {
            _config = config;
            _random = new System.Random(seed);
            _replay = new Replay(seed);
            _state = new GameState();

            _state.map = _CreateMap();
            _state.player = new GameUnit { id = _PLAYER_ID, is_player = true, pos = new GridPos(1, 1), hp = config.player_hp };
            _SpawnEnemies();
        }

        /// <summary>
        /// コマンドを1つ実行してターンを1つ進める。ゲーム進行の唯一の入口
        /// </summary>
        /// <param name="command">プレイヤーのコマンド</param>
        /// <returns>ターン処理後の状態</returns>
        public GameState Step(Command command)
        {
            // 終了後は何もしない
            if (isEnd) return _state;

            _state.turn++;
            _PlayerAct(command);
            _EnemiesAct();
            _CheckResult();
            _replay.AddTurn(command, _state);
            return _state;
        }

        /// <summary>
        /// リプレイ全文を返す
        /// </summary>
        /// <returns>安定したリプレイ文字列</returns>
        public string GetReplayText()
        {
            return _replay.GetText();
        }

        /// <summary>
        /// Seed からマップを生成する。外周は壁、内側は確率で壁、ゴールは右下付近
        /// </summary>
        /// <returns>生成したマップ</returns>
        private GameMap _CreateMap()
        {
            var map = new GameMap(_config.width, _config.height);
            var spawn = new GridPos(1, 1);
            var goal = new GridPos(_config.width - 2, _config.height - 2);

            // 外周を壁にする
            for (int x = 0; x < _config.width; x++)
            {
                map.SetTile(new GridPos(x, 0), TileType.Wall);
                map.SetTile(new GridPos(x, _config.height - 1), TileType.Wall);
            }
            for (int y = 0; y < _config.height; y++)
            {
                map.SetTile(new GridPos(0, y), TileType.Wall);
                map.SetTile(new GridPos(_config.width - 1, y), TileType.Wall);
            }

            // 内側の壁を Seed から確率で置く（ループ順を固定して決定論を守る）
            for (int x = 1; x < _config.width - 1; x++)
            {
                for (int y = 1; y < _config.height - 1; y++)
                {
                    var pos = new GridPos(x, y);
                    // スポーンとゴールには壁を置かない
                    if (pos.Equals(spawn) || pos.Equals(goal)) continue;
                    if (_random.Next(_PERCENT_MAX) < _config.wall_percent)
                    {
                        map.SetTile(pos, TileType.Wall);
                    }
                }
            }

            map.SetTile(goal, TileType.Goal);
            return map;
        }

        /// <summary>
        /// 敵を歩けるマスへ Seed から配置する。ID は 0 から昇順
        /// </summary>
        private void _SpawnEnemies()
        {
            for (int i = 0; i < _config.enemy_count; i++)
            {
                GridPos pos = _FindEnemySpawn();
                _state.enemies.Add(new GameUnit { id = i, is_player = false, pos = pos, hp = _config.enemy_hp });
            }
        }

        /// <summary>
        /// 敵を置ける空きマスを乱数で探す
        /// </summary>
        /// <returns>敵のスポーン座標</returns>
        private GridPos _FindEnemySpawn()
        {
            for (int i = 0; i < _MAX_SPAWN_TRY; i++)
            {
                var pos = new GridPos(_random.Next(1, _config.width - 1), _random.Next(1, _config.height - 1));
                if (!_state.map.IsWalkable(pos)) continue;
                if (_state.map.IsGoal(pos)) continue;
                if (pos.Equals(_state.player.pos)) continue;
                if (_FindAliveEnemyAt(pos) != null) continue;
                return pos;
            }
            // 空きが見つからないのは設定ミスなので隠さずに知らせる
            throw new System.InvalidOperationException("enemy spawn failed: no empty cell");
        }

        /// <summary>
        /// プレイヤーの行動。敵がいれば攻撃、歩ければ移動、壁なら何もしない
        /// </summary>
        /// <param name="command">プレイヤーのコマンド</param>
        private void _PlayerAct(Command command)
        {
            if (command == Command.Wait) return;

            GridPos next = _state.player.pos.Next(command);
            GameUnit enemy = _FindAliveEnemyAt(next);
            if (enemy != null)
            {
                // 敵のいるマスへ進むと攻撃になり、移動はしない
                _Attack(enemy, _config.player_attack);
                return;
            }
            if (_state.map.IsWalkable(next))
            {
                _state.player.pos = next;
            }
        }

        /// <summary>
        /// 生きている敵をID昇順で行動させる。隣なら攻撃、違えばプレイヤーへ1マス近づく
        /// </summary>
        private void _EnemiesAct()
        {
            foreach (GameUnit enemy in _state.enemies)
            {
                // 倒した敵は行動しない
                if (!enemy.isAlive) continue;

                if (enemy.pos.IsNextTo(_state.player.pos))
                {
                    _Attack(_state.player, _config.enemy_attack);
                }
                else
                {
                    _MoveEnemyTowardPlayer(enemy);
                }
            }
        }

        /// <summary>
        /// 敵をプレイヤーへ1マス近づける。差が大きい軸を先に試し、塞がれていればもう片方、両方無理なら待機
        /// </summary>
        /// <param name="enemy">動かす敵</param>
        private void _MoveEnemyTowardPlayer(GameUnit enemy)
        {
            int dx = _state.player.pos.x - enemy.pos.x;
            int dy = _state.player.pos.y - enemy.pos.y;

            var step_x = new GridPos(enemy.pos.x + System.Math.Sign(dx), enemy.pos.y);
            var step_y = new GridPos(enemy.pos.x, enemy.pos.y + System.Math.Sign(dy));

            // 差が大きい軸を先に試す（同じときは横を先にして順序を固定する）
            GridPos first = System.Math.Abs(dx) >= System.Math.Abs(dy) ? step_x : step_y;
            GridPos second = System.Math.Abs(dx) >= System.Math.Abs(dy) ? step_y : step_x;

            if (dx != 0 || dy != 0)
            {
                if (_CanEnemyMoveTo(first, enemy))
                {
                    enemy.pos = first;
                }
                else if (_CanEnemyMoveTo(second, enemy) && !second.Equals(enemy.pos))
                {
                    enemy.pos = second;
                }
                // 両方塞がれていれば待機
            }
        }

        /// <summary>
        /// 敵がそのマスへ移動できるかを返す
        /// </summary>
        /// <param name="pos">移動先</param>
        /// <param name="self">動こうとしている敵</param>
        /// <returns>移動できれば true</returns>
        private bool _CanEnemyMoveTo(GridPos pos, GameUnit self)
        {
            if (!_state.map.IsWalkable(pos)) return false;
            // プレイヤーのマスには乗れない
            if (pos.Equals(_state.player.pos)) return false;
            // 他の生きている敵のマスには乗れない
            GameUnit other = _FindAliveEnemyAt(pos);
            if (other != null && other != self) return false;
            return true;
        }

        /// <summary>
        /// 指定座標にいる生きている敵を返す。いなければ null
        /// </summary>
        /// <param name="pos">調べる座標</param>
        /// <returns>敵ユニット、いなければ null</returns>
        private GameUnit _FindAliveEnemyAt(GridPos pos)
        {
            // enemies はID昇順なので走査順も安定する
            foreach (GameUnit enemy in _state.enemies)
            {
                if (enemy.isAlive && enemy.pos.Equals(pos)) return enemy;
            }
            return null;
        }

        /// <summary>
        /// ユニットへダメージを与える。HP は 0 未満にしない
        /// </summary>
        /// <param name="target">攻撃対象</param>
        /// <param name="damage">ダメージ量</param>
        private void _Attack(GameUnit target, int damage)
        {
            target.hp -= damage;
            if (target.hp < 0) target.hp = 0;
        }

        /// <summary>
        /// 勝敗を判定する。死亡 → ゴール → 最大ターンの順に見る
        /// </summary>
        private void _CheckResult()
        {
            if (_state.player.hp <= 0)
            {
                _state.result = GameResult.Lose;
            }
            else if (_state.map.IsGoal(_state.player.pos))
            {
                _state.result = GameResult.Win;
            }
            else if (_state.turn >= _config.max_turn)
            {
                _state.result = GameResult.Draw;
            }
        }
    }
}
