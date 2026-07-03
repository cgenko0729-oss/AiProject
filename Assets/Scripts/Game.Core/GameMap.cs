namespace Game.Core
{
    /// <summary>
    /// グリッドマップ。Seed から生成し、マスの種類と歩行可否を答える
    /// </summary>
    public sealed class GameMap
    {
        /// <summary>マスの種類の2次元配列 [x, y]</summary>
        private readonly TileType[,] _tiles;

        /// <summary>マップの幅</summary>
        public readonly int width;

        /// <summary>マップの高さ</summary>
        public readonly int height;

        /// <summary>
        /// 空のマップを作る（生成ロジックは RogueGame 側から呼ぶ）
        /// </summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        public GameMap(int width, int height)
        {
            this.width = width;
            this.height = height;
            _tiles = new TileType[width, height];
        }

        /// <summary>
        /// 座標のマスの種類を返す
        /// </summary>
        /// <param name="pos">座標</param>
        /// <returns>マスの種類。マップ外は Wall 扱い</returns>
        public TileType GetTile(GridPos pos)
        {
            if (!IsInside(pos)) return TileType.Wall;
            return _tiles[pos.x, pos.y];
        }

        /// <summary>
        /// 座標のマスの種類を設定する
        /// </summary>
        /// <param name="pos">座標</param>
        /// <param name="tile">マスの種類</param>
        public void SetTile(GridPos pos, TileType tile)
        {
            if (!IsInside(pos)) return;
            _tiles[pos.x, pos.y] = tile;
        }

        /// <summary>
        /// マップ内の座標かを返す
        /// </summary>
        /// <param name="pos">座標</param>
        /// <returns>マップ内なら true</returns>
        public bool IsInside(GridPos pos)
        {
            return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
        }

        /// <summary>
        /// 壁かどうかを返す
        /// </summary>
        /// <param name="pos">座標</param>
        /// <returns>壁なら true</returns>
        public bool IsWall(GridPos pos)
        {
            return GetTile(pos) == TileType.Wall;
        }

        /// <summary>
        /// 歩けるマスかどうかを返す（床とゴール）
        /// </summary>
        /// <param name="pos">座標</param>
        /// <returns>歩けるなら true</returns>
        public bool IsWalkable(GridPos pos)
        {
            return !IsWall(pos);
        }

        /// <summary>
        /// ゴールかどうかを返す
        /// </summary>
        /// <param name="pos">座標</param>
        /// <returns>ゴールなら true</returns>
        public bool IsGoal(GridPos pos)
        {
            return GetTile(pos) == TileType.Goal;
        }
    }
}
