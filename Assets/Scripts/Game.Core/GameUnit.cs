namespace Game.Core
{
    /// <summary>
    /// プレイヤーと敵の両方を表すユニット。クラスを増やさないため共用にする
    /// </summary>
    public sealed class GameUnit
    {
        /// <summary>ユニットID。敵の行動順に使う（プレイヤーは -1）</summary>
        public int id;

        /// <summary>プレイヤーなら true</summary>
        public bool is_player;

        /// <summary>現在のグリッド座標</summary>
        public GridPos pos;

        /// <summary>現在のHP</summary>
        public int hp;

        /// <summary>生きているかどうか</summary>
        public bool isAlive => hp > 0;
    }
}
