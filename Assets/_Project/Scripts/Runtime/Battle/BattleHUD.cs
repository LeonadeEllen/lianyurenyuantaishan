using System.Collections.Generic;
using HearTheTide.Core;
using HearTheTide.UI;
using UnityEngine;
using UnityEngine.UI;

namespace HearTheTide.Battle
{
    /// <summary>战斗界面：血条、共鸣值、潮音提示、虚拟摇杆与按键（移动端），以及结算面板。</summary>
    public sealed class BattleHUD : MonoBehaviour
    {
        public VirtualJoystick Joystick { get; private set; }

        private BattleController _battle;
        private RectTransform _root;
        private Image _hpFill;
        private Text _hpText;
        private Image _resFill;
        private Text _resText;
        private Text _cueText;
        private float _cueTimer;
        private Text _banner;
        private float _bannerTimer;
        private Text _skillLabel, _assistLabel, _dodgeLabel, _resLabel;
        private Button _resButton;

        private sealed class Floater { public Text Text; public Vector3 World; public float Life; public float MaxLife; }
        private readonly List<Floater> _floaters = new List<Floater>();

        public void Init(BattleController battle, CharacterDefinition partner)
        {
            _battle = battle;
            var canvas = UIKit.CreateCanvas("BattleCanvas", 10);
            canvas.transform.SetParent(transform, false);
            _root = (RectTransform)canvas.transform;

            // 左上：女主血条
            var hpBg = UIKit.Block(_root, "HpBg", new Color(0, 0, 0, 0.6f));
            hpBg.rectTransform.Anchor(0.02f, 0.9f, 0.32f, 0.95f);
            _hpFill = UIKit.Block(hpBg.transform, "HpFill", UIKit.Good);
            _hpFill.rectTransform.Stretch(3f);
            _hpText = UIKit.Label(hpBg.transform, "", 24, TextAnchor.MiddleCenter);
            _hpText.rectTransform.Stretch();
            var who = UIKit.Label(_root, "搭档：" + partner.Name + " · " + partner.Title, 24, TextAnchor.MiddleLeft, UIKit.TextDim);
            who.rectTransform.Anchor(0.02f, 0.84f, 0.5f, 0.89f);

            // 右上：撤退
            var retreat = UIKit.Button(_root, "撤退", () => _battle.Retreat(), new Color(0, 0, 0, 0.5f), 26);
            ((RectTransform)retreat.transform).Anchor(0.9f, 0.9f, 0.98f, 0.97f);

            // 顶部中间：潮音提示
            _cueText = UIKit.Label(_root, "", 44, TextAnchor.MiddleCenter);
            _cueText.rectTransform.Anchor(0.3f, 0.78f, 0.7f, 0.9f);
            _banner = UIKit.Label(_root, "", 48, TextAnchor.MiddleCenter, UIKit.Accent);
            _banner.rectTransform.Anchor(0.15f, 0.55f, 0.85f, 0.75f);

            // 底部中间：共鸣值
            var resBg = UIKit.Block(_root, "ResBg", new Color(0, 0, 0, 0.6f));
            resBg.rectTransform.Anchor(0.3f, 0.03f, 0.62f, 0.07f);
            _resFill = UIKit.Block(resBg.transform, "ResFill", UIKit.Accent);
            _resFill.rectTransform.Stretch(3f);
            _resText = UIKit.Label(resBg.transform, "", 22, TextAnchor.MiddleCenter, Color.black);
            _resText.rectTransform.Stretch();

            BuildJoystick();
            BuildButtons();
        }

        private void BuildJoystick()
        {
            var area = UIKit.Block(_root, "Joystick", new Color(1, 1, 1, 0.08f));
            var rt = area.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.12f, 0.22f);
            rt.sizeDelta = new Vector2(300, 300);
            var knob = UIKit.Block(area.transform, "Knob", new Color(1, 1, 1, 0.35f));
            knob.rectTransform.sizeDelta = new Vector2(120, 120);
            knob.raycastTarget = false;
            Joystick = area.gameObject.AddComponent<VirtualJoystick>();
            Joystick.Knob = knob.rectTransform;
        }

        private Button Btn(string label, Vector2 anchor, float size, Color color, UnityEngine.Events.UnityAction onClick, out Text text)
        {
            var b = UIKit.Button(_root, label, onClick, color, 26);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(size, size);
            text = b.GetComponentInChildren<Text>();
            return b;
        }

        private void BuildButtons()
        {
            var p = _battle.Player;
            Text dummy;
            Btn("攻击", new Vector2(0.86f, 0.2f), 210, new Color(0.25f, 0.3f, 0.4f, 0.85f), () => p.Attack(), out dummy);
            Btn("闪避", new Vector2(0.73f, 0.12f), 150, new Color(0.85f, 0.85f, 0.85f, 0.5f), () => p.Dodge(), out _dodgeLabel);

            // 格挡需要"按住"，用 HoldButton 而不是 onClick
            Text parryLabel;
            var parry = Btn("格挡", new Vector2(0.74f, 0.32f), 150, new Color(0.25f, 0.55f, 1f, 0.6f), null, out parryLabel);
            var hold = parry.gameObject.AddComponent<HoldButton>();
            hold.Pressed += () => p.ParryDown();
            hold.Released += () => p.ParryUp();

            Btn("技能", new Vector2(0.84f, 0.44f), 140, new Color(0.3f, 0.6f, 0.75f, 0.7f), () => p.Skill(), out _skillLabel);
            Btn("协攻", new Vector2(0.94f, 0.4f), 140, new Color(1f, 0.6f, 0.3f, 0.7f), () => p.Assist(), out _assistLabel);
            _resButton = Btn("共鸣", new Vector2(0.94f, 0.6f), 150, new Color(0.95f, 0.77f, 0.43f, 0.85f), () => p.Resonance(), out _resLabel);
        }

        public void ShowCue(TideCue cue)
        {
            _cueText.text = UIColors.CueHint(cue);
            _cueText.color = UIColors.Cue(cue);
            _cueTimer = 1.2f;
        }

        public void Banner(string text)
        {
            _banner.text = text;
            _bannerTimer = 1.8f;
        }

        public void Float(Vector3 world, string text, Color color, float scale = 1f)
        {
            var t = UIKit.Label(_root, text, Mathf.RoundToInt(34 * scale), TextAnchor.MiddleCenter, color);
            t.rectTransform.sizeDelta = new Vector2(400, 80);
            t.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            _floaters.Add(new Floater { Text = t, World = world, Life = 0.9f, MaxLife = 0.9f });
        }

        private void Update()
        {
            var p = _battle.Player;
            float now = Time.time;
            _hpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(p.Hp / p.Stats.MaxHp), 1f);
            _hpText.text = "调律师  " + Mathf.CeilToInt(p.Hp) + " / " + Mathf.CeilToInt(p.Stats.MaxHp);
            _resFill.rectTransform.anchorMax = new Vector2(_battle.Resonance.Ratio, 1f);
            _resText.text = _battle.Resonance.IsFull ? "共鸣值已满！" : "共鸣值 " + Mathf.FloorToInt(_battle.Resonance.Value);
            _skillLabel.text = now < p.SkillReadyAt ? "技能\n" + (p.SkillReadyAt - now).ToString("0.0") : "技能";
            _assistLabel.text = now < p.AssistReadyAt ? "协攻\n" + (p.AssistReadyAt - now).ToString("0.0") : "协攻";
            _dodgeLabel.text = "闪避\n" + p.DodgeCharges;
            _resButton.interactable = _battle.Resonance.IsFull;

            if (_cueTimer > 0f) { _cueTimer -= Time.unscaledDeltaTime; if (_cueTimer <= 0f) _cueText.text = ""; }
            if (_bannerTimer > 0f) { _bannerTimer -= Time.unscaledDeltaTime; if (_bannerTimer <= 0f) _banner.text = ""; }

            var cam = _battle.Camera;
            for (int i = _floaters.Count - 1; i >= 0; i--)
            {
                var f = _floaters[i];
                f.Life -= Time.unscaledDeltaTime;
                if (f.Life <= 0f || cam == null) { Destroy(f.Text.gameObject); _floaters.RemoveAt(i); continue; }
                float k = 1f - f.Life / f.MaxLife;
                var screen = cam.WorldToScreenPoint(f.World + Vector3.up * k * 1.2f);
                f.Text.transform.position = screen;
                var c = f.Text.color;
                c.a = Mathf.Clamp01(f.Life / f.MaxLife * 1.5f);
                f.Text.color = c;
            }
        }

        public void ShowResult(bool victory, StageReward? reward)
        {
            var panel = UIKit.Block(_root, "Result", new Color(0, 0, 0, 0.8f));
            panel.rectTransform.Stretch();
            var list = UIKit.VList(panel.transform, 18f, 40);
            list.Anchor(0.3f, 0.2f, 0.7f, 0.85f);
            UIKit.Label(list, victory ? "战斗胜利" : "战斗失败", 64, TextAnchor.MiddleCenter, victory ? UIKit.Accent : UIKit.Danger);
            if (reward.HasValue)
            {
                var r = reward.Value;
                UIKit.Label(list, (r.FirstClear ? "首通奖励！\n" : "") +
                                  "金币 +" + r.Gold + "   钻石 +" + r.Diamonds + "\n经验道具 +" + r.CardExpItems +
                                  (r.BreakthroughMats > 0 ? "   突破材料 +" + r.BreakthroughMats : "") +
                                  "\n搭档好感 +" + r.AffectionGained, 32, TextAnchor.MiddleCenter);
            }
            else
            {
                UIKit.Label(list, "试试在潮音填满的瞬间格挡或闪避，触发「回潮」。\n红潮要点「协攻」让他打断。", 30, TextAnchor.MiddleCenter);
            }
            UIKit.Button(list, "返回", () => _battle.Exit(), UIKit.Accent * 0.85f, 34).Size(90f);
        }
    }
}
