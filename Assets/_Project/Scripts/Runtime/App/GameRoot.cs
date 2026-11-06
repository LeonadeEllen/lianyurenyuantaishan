using HearTheTide.Battle;
using HearTheTide.Core;
using HearTheTide.UI;
using UnityEngine;

namespace HearTheTide.App
{
    /// <summary>
    /// 游戏入口。运行任何场景都会自动创建（见 Bootstrap），持有 GameService 并在主界面与战斗间切换。
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        public const string SaveSlot = "save_0";

        public static GameRoot Instance { get; private set; }

        public GameService Game { get; private set; }

        private HubScreen _hub;
        private BattleController _battle;
        private Camera _uiCamera;
        private bool _dirty;
        private float _saveTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[GameRoot]");
            DontDestroyOnLoad(go);
            go.AddComponent<GameRoot>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            var db = DefaultContent.Create();
            var state = SaveStore.Load(SaveSlot) ?? GameService.CreateNewState(db);
            Game = new GameService(db, state, new UtcClock(), new SystemRandom());
            WireGame();
            Game.RefreshDaily();

            UIKit.EnsureEventSystem();
            CreateUiCamera();
            ShowHub();
        }

        private void CreateUiCamera()
        {
            // 主界面背景相机（美术留白：纯色背景，后续替换为 3D 主界面场景）
            var camGo = new GameObject("HubCamera");
            camGo.transform.SetParent(transform, false);
            _uiCamera = camGo.AddComponent<Camera>();
            _uiCamera.clearFlags = CameraClearFlags.SolidColor;
            _uiCamera.backgroundColor = UIKit.Bg;
            _uiCamera.cullingMask = 0;
        }

        public void ShowHub()
        {
            if (_hub == null)
            {
                var go = new GameObject("Hub");
                go.transform.SetParent(transform, false);
                _hub = go.AddComponent<HubScreen>();
                _hub.Init(this);
            }
            _uiCamera.enabled = true;
            _hub.gameObject.SetActive(true);
            Game.RefreshDaily();
            _hub.Refresh();
        }

        public void StartBattle(string stageId, string partnerId)
        {
            if (!Game.TryStartStage(stageId))
            {
                _hub.Toast("体力不足");
                return;
            }
            Game.State.SelectedPartnerId = partnerId;
            _hub.gameObject.SetActive(false);
            _uiCamera.enabled = false;
            var go = new GameObject("Battle");
            _battle = go.AddComponent<BattleController>();
            _battle.Init(this, Game.Db.Stage(stageId), Game.ComputeLoadout(partnerId), Game.Db.Character(partnerId));
        }

        /// <summary>战斗结束后由 BattleController 调用。</summary>
        public StageReward? OnBattleFinished(string stageId, string partnerId, bool victory)
        {
            if (!victory) return null;
            return Game.CompleteStage(stageId, partnerId);
        }

        public void ExitBattle()
        {
            if (_battle != null) Destroy(_battle.gameObject);
            _battle = null;
            Time.timeScale = 1f;
            ShowHub();
        }

        public void ResetSave()
        {
            SaveStore.Delete(SaveSlot);
            var db = Game.Db;
            Game = new GameService(db, GameService.CreateNewState(db), new UtcClock(), new SystemRandom());
            WireGame();
            Game.RefreshDaily();
            _dirty = true;
            _hub.Refresh();
        }

        private void WireGame()
        {
            Game.Changed += () =>
            {
                _dirty = true;
                if (_hub != null) _hub.MarkDirty();
            };
        }

        private void Update()
        {
            if (!_dirty) return;
            _saveTimer += Time.unscaledDeltaTime;
            if (_saveTimer < 1f) return;
            _saveTimer = 0f;
            _dirty = false;
            SaveStore.Save(SaveSlot, Game.State);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveStore.Save(SaveSlot, Game.State);
            else if (Game != null && _hub != null && _hub.gameObject.activeSelf) ShowHub();
        }

        private void OnApplicationQuit() { SaveStore.Save(SaveSlot, Game.State); }
    }
}
