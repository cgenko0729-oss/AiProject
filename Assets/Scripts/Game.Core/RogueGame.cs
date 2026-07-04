using System.Collections.Generic;

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

        /// <summary>
        /// BSP で作った部屋の矩形（Core 内部専用）
        /// </summary>
        private readonly struct Room
        {
            /// <summary>左下のX座標</summary>
            public readonly int x;

            /// <summary>左下のY座標</summary>
            public readonly int y;

            /// <summary>幅</summary>
            public readonly int w;

            /// <summary>高さ</summary>
            public readonly int h;

            /// <summary>
            /// 部屋を作る
            /// </summary>
            /// <param name="x">左下X</param>
            /// <param name="y">左下Y</param>
            /// <param name="w">幅</param>
            /// <param name="h">高さ</param>
            public Room(int x, int y, int w, int h)
            {
                this.x = x;
                this.y = y;
                this.w = w;
                this.h = h;
            }

            /// <summary>部屋の中心座標</summary>
            public GridPos center => new GridPos(x + w / 2, y + h / 2);
        }

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

            // BSP でダンジョンを掘り、最初の部屋の中心にプレイヤーを置く
            var rooms = new List<Room>();
            _state.map = _CreateMap(rooms);
            GridPos spawn = rooms[0].center;
            _state.player = new GameUnit { id = _PLAYER_ID, is_player = true, pos = spawn, hp = config.player_hp };

            GridPos goal = _PickGoal(rooms, spawn);
            _state.map.SetTile(goal, TileType.Goal);
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
        /// BSP 方式で Seed からダンジョンを生成する。
        /// 全マスを壁で埋め、空間を再帰分割して部屋を掘り、部屋同士をL字通路でつなぐ
        /// </summary>
        /// <param name="rooms">掘った部屋の一覧（生成順）を受け取るリスト</param>
        /// <returns>生成したマップ</returns>
        private GameMap _CreateMap(List<Room> rooms)
        {
            var map = new GameMap(_config.width, _config.height);

            // まず全マスを壁にする（岩盤から掘っていく方式）
            for (int x = 0; x < _config.width; x++)
            {
                for (int y = 0; y < _config.height; y++)
                {
                    map.SetTile(new GridPos(x, y), TileType.Wall);
                }
            }

            // 空間を再帰的に分割して部屋を作る（再帰の順序が固定なので決定論を守れる）
            _SplitArea(map, 0, 0, _config.width, _config.height, rooms);

            // 生成順に隣り合う部屋をつないでいく（全部屋の連結を保証する）
            for (int i = 1; i < rooms.Count; i++)
            {
                _CarveCorridor(map, rooms[i - 1].center, rooms[i].center);
            }

            return map;
        }

        /// <summary>
        /// エリアを BSP で再帰分割する。分割できない大きさになったら部屋を掘る
        /// </summary>
        /// <param name="map">対象マップ</param>
        /// <param name="x">エリア左下X</param>
        /// <param name="y">エリア左下Y</param>
        /// <param name="w">エリア幅</param>
        /// <param name="h">エリア高さ</param>
        /// <param name="rooms">部屋一覧の出力先</param>
        private void _SplitArea(GameMap map, int x, int y, int w, int h, List<Room> rooms)
        {
            bool can_split_w = w >= _config.min_area_size * 2;
            bool can_split_h = h >= _config.min_area_size * 2;

            // これ以上割れないので部屋を掘って終わる
            if (!can_split_w && !can_split_h)
            {
                rooms.Add(_CarveRoom(map, x, y, w, h));
                return;
            }

            // 両方向に割れるときは長い方を割る（規則を固定して決定論を守る）
            bool split_vertical = can_split_w && can_split_h ? w >= h : can_split_w;

            if (split_vertical)
            {
                // 縦線で左右に分ける
                int cut = _random.Next(_config.min_area_size, w - _config.min_area_size + 1);
                _SplitArea(map, x, y, cut, h, rooms);
                _SplitArea(map, x + cut, y, w - cut, h, rooms);
            }
            else
            {
                // 横線で上下に分ける
                int cut = _random.Next(_config.min_area_size, h - _config.min_area_size + 1);
                _SplitArea(map, x, y, w, cut, rooms);
                _SplitArea(map, x, y + cut, w, h - cut, rooms);
            }
        }

        /// <summary>
        /// エリアの内側にランダムな大きさの部屋を掘る。エリア境界1マスは必ず壁で残す
        /// </summary>
        /// <param name="map">対象マップ</param>
        /// <param name="area_x">エリア左下X</param>
        /// <param name="area_y">エリア左下Y</param>
        /// <param name="area_w">エリア幅</param>
        /// <param name="area_h">エリア高さ</param>
        /// <returns>掘った部屋</returns>
        private Room _CarveRoom(GameMap map, int area_x, int area_y, int area_w, int area_h)
        {
            // 境界を1マスずつ残した掘れる最大サイズ
            int max_w = area_w - 2;
            int max_h = area_h - 2;

            // 最小部屋サイズとエリアの間でランダムに決める（小さいエリアでは全部使う）
            int room_w = max_w <= _config.min_room_size ? max_w : _random.Next(_config.min_room_size, max_w + 1);
            int room_h = max_h <= _config.min_room_size ? max_h : _random.Next(_config.min_room_size, max_h + 1);

            // 部屋の位置もエリア内でランダムにずらす
            int room_x = area_x + 1 + (max_w - room_w <= 0 ? 0 : _random.Next(0, max_w - room_w + 1));
            int room_y = area_y + 1 + (max_h - room_h <= 0 ? 0 : _random.Next(0, max_h - room_h + 1));

            var room = new Room(room_x, room_y, room_w, room_h);
            for (int x = room.x; x < room.x + room.w; x++)
            {
                for (int y = room.y; y < room.y + room.h; y++)
                {
                    map.SetTile(new GridPos(x, y), TileType.Floor);
                }
            }
            return room;
        }

        /// <summary>
        /// 2点の間をL字（横→縦）で床に掘って通路にする
        /// </summary>
        /// <param name="map">対象マップ</param>
        /// <param name="from">始点（部屋の中心）</param>
        /// <param name="to">終点（部屋の中心）</param>
        private void _CarveCorridor(GameMap map, GridPos from, GridPos to)
        {
            // まず横に掘る
            int x = from.x;
            while (x != to.x)
            {
                map.SetTile(new GridPos(x, from.y), TileType.Floor);
                x += System.Math.Sign(to.x - x);
            }
            // 次に縦に掘る
            int y = from.y;
            while (y != to.y)
            {
                map.SetTile(new GridPos(to.x, y), TileType.Floor);
                y += System.Math.Sign(to.y - y);
            }
            map.SetTile(to, TileType.Floor);
        }

        /// <summary>
        /// ゴールの位置を決める。スポーンから最も遠い部屋の中心にする
        /// </summary>
        /// <param name="rooms">部屋一覧</param>
        /// <param name="spawn">プレイヤーのスポーン座標</param>
        /// <returns>ゴール座標</returns>
        private GridPos _PickGoal(List<Room> rooms, GridPos spawn)
        {
            // マンハッタン距離が最大の部屋を選ぶ（同値なら先の部屋を採用して順序を固定）
            Room best = rooms[0];
            int best_dist = -1;
            foreach (Room room in rooms)
            {
                GridPos center = room.center;
                int dist = System.Math.Abs(center.x - spawn.x) + System.Math.Abs(center.y - spawn.y);
                if (dist > best_dist)
                {
                    best_dist = dist;
                    best = room;
                }
            }

            // 部屋が1つしかないときは中心がスポーンと重なるので部屋の右上隅にする
            if (best_dist == 0)
            {
                var corner = new GridPos(best.x + best.w - 1, best.y + best.h - 1);
                if (corner.Equals(spawn))
                {
                    // 部屋が小さすぎてゴールを置けないのは設定ミスなので隠さず知らせる
                    throw new System.InvalidOperationException("map too small to place goal");
                }
                return corner;
            }
            return best.center;
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
