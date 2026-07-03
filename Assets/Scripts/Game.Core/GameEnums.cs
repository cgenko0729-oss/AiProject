namespace Game.Core
{
    /// <summary>
    /// プレイヤーが入力できるコマンド。1コマンド = 1ターン
    /// </summary>
    public enum Command
    {
        Up,
        Down,
        Left,
        Right,
        Wait
    }

    /// <summary>
    /// マップの1マスの種類
    /// </summary>
    public enum TileType
    {
        Floor,
        Wall,
        Goal
    }

    /// <summary>
    /// ゲームの勝敗結果
    /// </summary>
    public enum GameResult
    {
        Playing,
        Win,
        Lose,
        Draw
    }
}
