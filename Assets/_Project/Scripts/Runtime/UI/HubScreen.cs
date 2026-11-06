using System.Collections.Generic;
using HearTheTide.App;
using HearTheTide.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HearTheTide.UI
{
    /// <summary>主界面：顶部资源栏 + 左侧导航 + 右侧内容面板。</summary>
    public sealed class HubScreen : MonoBehaviour
    {
        private GameRoot _app;
        private Canvas _canvas;
        private RectTransform _topBar;
        private RectTransform _content;
        private Text _toast;
        private float _toastTimer;
        private bool _dirty;

        private readonly List<HubPanel> _panels = new List<HubPanel>();
        private HubPanel _current;

        public GameService Game { get { return _app.Game; } }
        public GameRoot App { get { return _app; } }

        public void Init(GameRoot app)
        {
            _app = app;
            _canvas = UIKit.CreateCanvas("HubCanvas", 0);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;

            UIKit.Block(root, "Background", UIKit.Bg).rectTransform.Stretch();

            var top = UIKit.Block(root, "TopBar", UIKit.Panel);
            top.rectTransform.Anchor(0f, 0.9f, 1f, 1f);
            _topBar = UIKit.HList(top.transform, 24f, 12);
            _topBar.Stretch();

            var nav = UIKit.Block(root, "Nav", UIKit.Panel);
            nav.rectTransform.Anchor(0f, 0f, 0.16f, 0.89f);
            var navList = UIKit.VList(nav.transform, 10f, 14);
            navList.Stretch();

            var contentBg = UIKit.Block(root, "Content", new Color(0, 0, 0, 0));
            contentBg.rectTransform.Anchor(0.17f, 0.01f, 0.99f, 0.89f);
            _content = contentBg.rectTransform;

            _panels.Add(new HomePanel());
            _panels.Add(new StagePanel());
            _panels.Add(new ChatPanel());
            _panels.Add(new HavenPanel());
            _panels.Add(new DatePanel());
            _panels.Add(new CardPanel());
            _panels.Add(new GachaPanel());
            _panels.Add(new MemoryPanel());
            _panels.Add(new CalendarPanel());
            _panels.Add(new SettingsPanel());

            foreach (var p in _panels)
            {
                p.Hub = this;
                var panel = p;
                UIKit.Button(navList, p.Title, () => Open(panel), null, 28).Size(78f);
            }

            _toast = UIKit.Label(root, "", 34, TextAnchor.MiddleCenter, UIKit.Accent);
            _toast.rectTransform.Anchor(0.3f, 0.02f, 0.85f, 0.1f);

            Open(_panels[0]);
        }

        public void Open(HubPanel panel)
        {
            _current = panel;
            Refresh();
        }

        public void MarkDirty() { _dirty = true; }

        public void Refresh()
        {
            _dirty = false;
            BuildTopBar();
            UIKit.ClearChildren(_content);
            if (_current != null)
            {
                var root = UIKit.Rect(_content, _current.Title);
                root.Stretch();
                _current.Build(root);
            }
        }

        private void BuildTopBar()
        {
            UIKit.ClearChildren(_topBar);
            var s = Game.State;
            s.Stamina.Refresh(Game.Now);
            AddStat("调律师 · " + s.PlayerName);
            AddStat("今日潮汐：" + TideCalendar.DisplayName(Game.TodayTide));
            AddStat("体力 " + s.Stamina.Current + "/" + StaminaState.Max);
            AddStat("钻石 " + s.Wallet.Diamonds);
            AddStat("金币 " + s.Wallet.Get(ItemIds.Gold));
            AddStat("回响币 " + s.Wallet.Get(ItemIds.EchoCoin));
        }

        private void AddStat(string text)
        {
            UIKit.Label(_topBar, text, 28, TextAnchor.MiddleCenter);
        }

        public void Toast(string message)
        {
            _toast.text = message;
            _toastTimer = 2.5f;
        }

        private void LateUpdate()
        {
            if (_dirty) Refresh();
            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.unscaledDeltaTime;
                if (_toastTimer <= 0f) _toast.text = "";
            }
        }
    }

    /// <summary>主界面右侧内容面板基类。每次数据变化整体重建（原型阶段够用）。</summary>
    public abstract class HubPanel
    {
        public HubScreen Hub;
        protected GameService G { get { return Hub.Game; } }
        public abstract string Title { get; }
        public abstract void Build(RectTransform root);

        protected string SelectedCharacter;

        /// <summary>男主选择条。</summary>
        protected RectTransform CharacterTabs(Transform parent)
        {
            if (string.IsNullOrEmpty(SelectedCharacter)) SelectedCharacter = G.State.SelectedPartnerId;
            var row = UIKit.HList(parent, 10f);
            row.Size(70f);
            foreach (var c in G.Db.Characters)
            {
                var id = c.Id;
                bool sel = id == SelectedCharacter;
                var color = sel ? UIKit.Hex(c.PlaceholderColorHex) : UIKit.PanelLight;
                var b = UIKit.Button(row, c.Name + (c.AvailableInMvp ? "" : "（首发）"), () => { SelectedCharacter = id; Hub.Refresh(); }, color, 28);
                if (sel && c.PlaceholderColorHex == "E8E4D6") b.GetComponentInChildren<Text>().color = Color.black;
            }
            return row;
        }

        /// <summary>美术留白占位块。</summary>
        protected static Image ArtPlaceholder(Transform parent, string label, Color color)
        {
            var img = UIKit.Block(parent, "ArtPlaceholder", color);
            var t = UIKit.Label(img.transform, "【美术留白】\n" + label, 30, TextAnchor.MiddleCenter,
                color.grayscale > 0.6f ? Color.black : UIKit.TextColor);
            t.rectTransform.Stretch(10f);
            return img;
        }

        protected static Text Line(Transform parent, string text, int size = 30, Color? color = null)
        {
            // 不设固定高度：布局组会按文字实际行数取 preferredHeight
            return UIKit.Label(parent, text, size, TextAnchor.UpperLeft, color);
        }
    }
}
