using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// ゲームの現在状態。Presentation はこれを読んで描画する
    /// </summary>
    public sealed class GameState
    {
        /// <summary>現在のターン数</summary>
        public int turn;

        /// <summary>勝敗結果</summary>
        public GameResult result = GameResult.Playing;

        /// <summary>プレイヤーユニット</summary>
        public GameUnit player;

        /// <summary>敵ユニット一覧。必ずID昇順で並べる（決定論のため）</summary>
        public List<GameUnit> enemies = new List<GameUnit>();

        /// <summary>マップ</summary>
        public GameMap map;
    }
}
