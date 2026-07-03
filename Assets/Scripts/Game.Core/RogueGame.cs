namespace Game.Core
{
    /// <summary>
    /// ゲーム本体。Step(command) だけでターンが進む決定論的なコアクラス
    /// </summary>
    public sealed class RogueGame
    {
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

            // TODO: Phase 4 でマップ生成・プレイヤー配置・敵配置を実装する
            _state.map = new GameMap(config.width, config.height);
            _state.player = new GameUnit { id = -1, is_player = true, pos = new GridPos(1, 1), hp = config.player_hp };
        }

        /// <summary>
        /// コマンドを1つ実行してターンを1つ進める。ゲーム進行の唯一の入口
        /// </summary>
        /// <param name="command">プレイヤーのコマンド</param>
        /// <returns>ターン処理後の状態</returns>
        public GameState Step(Command command)
        {
            // TODO: Phase 4 でターン処理（プレイヤー行動→敵行動→勝敗判定→リプレイ記録）を実装する
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
    }
}
