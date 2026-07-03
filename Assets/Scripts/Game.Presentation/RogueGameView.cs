using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Presentation
{
    /// <summary>
    /// RogueGame を簡単な 3D ダンジョンとして表示する唯一の MonoBehaviour。
    /// 入力を Command に変換して Step を呼び、GameState を描画するだけ。
    /// ダメージ・勝敗・マップ生成などのルールは一切持たない
    /// </summary>
    public sealed class RogueGameView : MonoBehaviour
    {
        /// <summary>ゲームの Seed（インスペクタで変更できる）</summary>
        public int seed = 12345;

        /// <summary>コアのゲーム本体</summary>
        private RogueGame _game;

        /// <summary>プレイヤーの表示オブジェクト</summary>
        private GameObject _player_obj;

        /// <summary>敵の表示オブジェクト一覧（ID順で並ぶ）</summary>
        private readonly List<GameObject> _enemy_objs = new List<GameObject>();

        /// <summary>結果ログを一度だけ出すためのフラグ</summary>
        private bool _result_shown;

        /// <summary>
        /// ゲームを作って盤面とユニットを生成する
        /// </summary>
        private void Start()
        {
            _game = new RogueGame(seed, new GameConfig());
            _BuildBoard();
            _BuildUnits();
            _SetupCamera();
            _RefreshUnits();
        }

        /// <summary>
        /// キー入力を読み、押された時だけ1ターン進める
        /// </summary>
        private void Update()
        {
            if (_game.isEnd) return;

            // キーが押されたフレームだけコマンドを送る（毎フレーム進めない）
            if (_TryReadCommand(out Command command))
            {
                _game.Step(command);
                _RefreshUnits();
                _ShowResultOnce();
            }
        }

        /// <summary>
        /// WASD と Space を Command に変換する（新 Input System）
        /// </summary>
        /// <param name="command">変換されたコマンド</param>
        /// <returns>このフレームで入力があれば true</returns>
        private bool _TryReadCommand(out Command command)
        {
            command = Command.Wait;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;

            if (keyboard.wKey.wasPressedThisFrame) { command = Command.Up; return true; }
            if (keyboard.sKey.wasPressedThisFrame) { command = Command.Down; return true; }
            if (keyboard.aKey.wasPressedThisFrame) { command = Command.Left; return true; }
            if (keyboard.dKey.wasPressedThisFrame) { command = Command.Right; return true; }
            if (keyboard.spaceKey.wasPressedThisFrame) { command = Command.Wait; return true; }
            return false;
        }

        /// <summary>
        /// 床・壁・ゴールのキューブを生成する
        /// </summary>
        private void _BuildBoard()
        {
            GameMap map = _game.state.map;
            for (int x = 0; x < map.width; x++)
            {
                for (int y = 0; y < map.height; y++)
                {
                    var pos = new GridPos(x, y);
                    if (map.IsWall(pos))
                    {
                        // 壁 = 灰色キューブ
                        _CreateCube("Wall", pos, 0.5f, new Vector3(1f, 1f, 1f), Color.gray);
                    }
                    else
                    {
                        // 床 = 平たい暗色キューブ
                        _CreateCube("Floor", pos, -0.05f, new Vector3(1f, 0.1f, 1f), new Color(0.25f, 0.2f, 0.15f));
                        if (map.IsGoal(pos))
                        {
                            // ゴール = 緑の低いキューブ
                            _CreateCube("Goal", pos, 0.1f, new Vector3(1f, 0.2f, 1f), Color.green);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// プレイヤーと敵のカプセルを生成する
        /// </summary>
        private void _BuildUnits()
        {
            _player_obj = _CreateCapsule("Player", Color.blue);
            foreach (GameUnit enemy in _game.state.enemies)
            {
                _enemy_objs.Add(_CreateCapsule("Enemy" + enemy.id, Color.red));
            }
        }

        /// <summary>
        /// GameState に合わせてユニットの位置と表示を更新する
        /// </summary>
        private void _RefreshUnits()
        {
            GameState state = _game.state;
            _player_obj.transform.position = _ToWorld(state.player.pos, 0.6f);
            _player_obj.SetActive(state.player.hp > 0);

            // 敵リストと表示リストは同じID順なので添字で対応する
            for (int i = 0; i < state.enemies.Count; i++)
            {
                GameUnit enemy = state.enemies[i];
                _enemy_objs[i].transform.position = _ToWorld(enemy.pos, 0.6f);
                // 倒された敵は非表示にする
                _enemy_objs[i].SetActive(enemy.isAlive);
            }
        }

        /// <summary>
        /// 結果が出たら一度だけログに出す
        /// </summary>
        private void _ShowResultOnce()
        {
            if (!_game.isEnd || _result_shown) return;
            _result_shown = true;
            Debug.Log("Result: " + _game.state.result + " Turn: " + _game.state.turn);
        }

        /// <summary>
        /// 盤面全体が見える位置にメインカメラを置く
        /// </summary>
        private void _SetupCamera()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            GameMap map = _game.state.map;
            float cx = (map.width - 1) * 0.5f;
            float cz = (map.height - 1) * 0.5f;
            float size = Mathf.Max(map.width, map.height);
            camera.transform.position = new Vector3(cx, size * 1.1f, cz - size * 0.6f);
            camera.transform.LookAt(new Vector3(cx, 0f, cz));
        }

        /// <summary>
        /// グリッド座標をワールド座標へ変換する（X→X、Y→Z）
        /// </summary>
        /// <param name="pos">グリッド座標</param>
        /// <param name="height">ワールドのY座標</param>
        /// <returns>ワールド座標</returns>
        private static Vector3 _ToWorld(GridPos pos, float height)
        {
            return new Vector3(pos.x, height, pos.y);
        }

        /// <summary>
        /// 色付きキューブを1つ作る
        /// </summary>
        /// <param name="name">オブジェクト名</param>
        /// <param name="pos">グリッド座標</param>
        /// <param name="height">ワールドのY座標</param>
        /// <param name="scale">大きさ</param>
        /// <param name="color">色</param>
        /// <returns>作ったオブジェクト</returns>
        private GameObject _CreateCube(string name, GridPos pos, float height, Vector3 scale, Color color)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.parent = transform;
            cube.transform.position = _ToWorld(pos, height);
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().material.color = color;
            // 物理はゲームに使わないので当たり判定を消す
            Destroy(cube.GetComponent<UnityEngine.Collider>());
            return cube;
        }

        /// <summary>
        /// 色付きカプセルを1つ作る
        /// </summary>
        /// <param name="name">オブジェクト名</param>
        /// <param name="color">色</param>
        /// <returns>作ったオブジェクト</returns>
        private GameObject _CreateCapsule(string name, Color color)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = name;
            capsule.transform.parent = transform;
            capsule.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
            capsule.GetComponent<Renderer>().material.color = color;
            Destroy(capsule.GetComponent<UnityEngine.Collider>());
            return capsule;
        }

        /// <summary>
        /// ターン・HP・結果を画面に表示する（表示のみ、判定はしない）
        /// </summary>
        private void OnGUI()
        {
            if (_game == null) return;
            GameState state = _game.state;
            GUI.Label(new Rect(10, 10, 400, 24), "Turn: " + state.turn + "  HP: " + state.player.hp + "  Result: " + state.result);
            GUI.Label(new Rect(10, 34, 400, 24), "WASD = move / attack, Space = wait");
        }
    }
}
