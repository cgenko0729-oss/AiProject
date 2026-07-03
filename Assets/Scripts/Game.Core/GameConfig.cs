namespace Game.Core
{
    /// <summary>
    /// ゲームの調整値をすべて持つクラス。ロジック中のマジックナンバーを防ぐ
    /// </summary>
    public sealed class GameConfig
    {
        /// <summary>マップの幅</summary>
        public int width = 10;

        /// <summary>マップの高さ</summary>
        public int height = 10;

        /// <summary>引き分けになる最大ターン数</summary>
        public int max_turn = 100;

        /// <summary>プレイヤーの初期HP</summary>
        public int player_hp = 10;

        /// <summary>プレイヤーの攻撃力</summary>
        public int player_attack = 2;

        /// <summary>敵の数</summary>
        public int enemy_count = 3;

        /// <summary>敵の初期HP</summary>
        public int enemy_hp = 3;

        /// <summary>敵の攻撃力</summary>
        public int enemy_attack = 1;

        /// <summary>内側の壁が生成される確率（パーセント）</summary>
        public int wall_percent = 15;
    }
}
