namespace Game.Core
{
    /// <summary>
    /// グリッド上の整数座標。Unity の Vector 型の代わりに使う
    /// </summary>
    public readonly struct GridPos : System.IEquatable<GridPos>
    {
        /// <summary>X座標（横）</summary>
        public readonly int x;

        /// <summary>Y座標（縦）</summary>
        public readonly int y;

        /// <summary>
        /// 座標を作る
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        public GridPos(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// コマンドの方向に1マス動いた座標を返す
        /// </summary>
        /// <param name="command">移動コマンド</param>
        /// <returns>移動後の座標。Wait のときは同じ座標</returns>
        public GridPos Next(Command command)
        {
            switch (command)
            {
                case Command.Up: return new GridPos(x, y + 1);
                case Command.Down: return new GridPos(x, y - 1);
                case Command.Left: return new GridPos(x - 1, y);
                case Command.Right: return new GridPos(x + 1, y);
                default: return this;
            }
        }

        /// <summary>
        /// 相手と上下左右で隣接しているかを返す
        /// </summary>
        /// <param name="other">相手の座標</param>
        /// <returns>隣接していれば true</returns>
        public bool IsNextTo(GridPos other)
        {
            int dx = System.Math.Abs(x - other.x);
            int dy = System.Math.Abs(y - other.y);
            return dx + dy == 1;
        }

        /// <summary>
        /// 同じ座標かを返す
        /// </summary>
        /// <param name="other">比較する座標</param>
        /// <returns>同じなら true</returns>
        public bool Equals(GridPos other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPos other && Equals(other);
        }

        public override int GetHashCode()
        {
            return x * 397 ^ y;
        }

        /// <summary>
        /// リプレイ用の安定した文字列 "(x,y)" を返す
        /// </summary>
        /// <returns>座標文字列</returns>
        public override string ToString()
        {
            return "(" + x + "," + y + ")";
        }
    }
}
