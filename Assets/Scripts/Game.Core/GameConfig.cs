namespace Game.Core
{
    /// <summary>
    /// ゲームの調整値をすべて持つクラス。ロジック中のマジックナンバーを防ぐ
    /// </summary>
    public sealed class GameConfig
    {
        /// <summary>マップの幅</summary>
        public int width = 40;

        /// <summary>マップの高さ</summary>
        public int height = 24;

        /// <summary>引き分けになる最大ターン数</summary>
        public int max_turn = 300;

        /// <summary>プレイヤーの初期HP</summary>
        public int player_hp = 10;

        /// <summary>プレイヤーの攻撃力</summary>
        public int player_attack = 2;

        /// <summary>敵の数</summary>
        public int enemy_count = 8;

        /// <summary>敵の初期HP</summary>
        public int enemy_hp = 3;

        /// <summary>敵の攻撃力</summary>
        public int enemy_attack = 1;

        /// <summary>BSP で分割するエリアの最小サイズ（これ未満は分割しない）</summary>
        public int min_area_size = 8;

        /// <summary>部屋の最小サイズ</summary>
        public int min_room_size = 4;
    }
}
