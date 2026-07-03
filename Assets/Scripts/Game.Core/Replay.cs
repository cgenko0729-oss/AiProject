using System.Text;

namespace Game.Core
{
    /// <summary>
    /// リプレイ記録。同じ Seed と同じコマンド列なら必ず同じ文字列になる
    /// </summary>
    public sealed class Replay
    {
        /// <summary>リプレイ本文を組み立てるバッファ</summary>
        private readonly StringBuilder _builder = new StringBuilder();

        /// <summary>
        /// Seed をヘッダーとして記録する
        /// </summary>
        /// <param name="seed">ゲームの Seed</param>
        public Replay(int seed)
        {
            _builder.AppendLine("Seed=" + seed);
        }

        /// <summary>
        /// 1ターン分の状態を安定した書式で追加する。敵はID昇順で書く
        /// </summary>
        /// <param name="command">このターンのコマンド</param>
        /// <param name="state">ターン処理後の状態</param>
        public void AddTurn(Command command, GameState state)
        {
            _builder.Append("Turn=").Append(state.turn);
            _builder.Append(" Command=").Append(command);
            _builder.Append(" Result=").Append(state.result);
            _builder.Append(" Player=").Append(state.player.pos).Append(":").Append(state.player.hp);
            _builder.Append(" Enemies=[");
            // 敵はID昇順で記録する（順序を安定させるため）
            for (int i = 0; i < state.enemies.Count; i++)
            {
                if (i > 0) _builder.Append(",");
                GameUnit enemy = state.enemies[i];
                _builder.Append(enemy.id).Append(":").Append(enemy.pos).Append(":").Append(enemy.hp);
            }
            _builder.AppendLine("]");
        }

        /// <summary>
        /// リプレイ全文を返す
        /// </summary>
        /// <returns>リプレイ文字列</returns>
        public string GetText()
        {
            return _builder.ToString();
        }
    }
}
