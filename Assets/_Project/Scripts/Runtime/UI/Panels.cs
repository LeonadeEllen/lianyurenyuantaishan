using System;
using System.Collections.Generic;
using System.Text;
using HearTheTide.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HearTheTide.UI
{
    // ====================================================================== 首页

    public sealed class HomePanel : HubPanel
    {
        public override string Title { get { return "首页"; } }

        public override void Build(RectTransform root)
        {
            var partner = G.Db.Character(G.State.SelectedPartnerId);
            var art = ArtPlaceholder(root, "主界面 3D 男主：" + partner.Name + "\n（待机动作 / 点击反应）", UIKit.Hex(partner.PlaceholderColorHex));
            art.rectTransform.Anchor(0f, 0f, 0.55f, 1f);

            var info = UIKit.Block(root, "Info", UIKit.Panel);
            info.rectTransform.Anchor(0.57f, 0f, 1f, 1f);
            var list = UIKit.VList(info.transform, 14f, 24);
            list.Stretch();

            var tide = G.TodayTide;
            Line(list, "今日潮汐：" + TideCalendar.DisplayName(tide), 38, UIKit.Accent);
            Line(list, "栖所行动点：" + G.State.HavenActionPoints + "/" + HavenRules.DailyActionPoints);
            Line(list, "约会券：" + G.State.Wallet.Get(ItemIds.DateTicket));
            Line(list, " ");
            Line(list, "他们今天的心情", 32, UIKit.Accent);
            foreach (var c in G.Db.Characters)
            {
                var a = G.GetAffection(c.Id);
                string note = c.TroubledTide == tide ? "（情绪低落，去陪陪他？）" : c.FavoredTide == tide ? "（心情很好）" : "";
                Line(list, c.Name + "  好感 Lv." + a.Level + "  心情 " + a.Mood + note, 28);
            }
            var next = G.NextChat(G.State.SelectedPartnerId);
            if (next != null) Line(list, "\n" + partner.Name + " 发来一条新消息。", 28, UIKit.Accent);
        }
    }

    // ====================================================================== 关卡

    public sealed class StagePanel : HubPanel
    {
        public override string Title { get { return "主线战斗"; } }

        public override void Build(RectTransform root)
        {
            var col = UIKit.VList(root, 12f, 0);
            col.Stretch();
            Line(col, "选择出战搭档（同时决定心核盘与共鸣技）", 28, UIKit.TextDim);
            CharacterTabs(col);

            var loadout = G.ComputeLoadout(SelectedCharacter);
            Line(col, string.Format("战力预览  攻击 {0:0}  生命 {1:0}  防御 {2:0}  ·  判定窗口 {3:0.00}s  ·  共鸣技「{4}」",
                loadout.PlayerStats.Attack, loadout.PlayerStats.MaxHp, loadout.PlayerStats.Defense, loadout.TideWindow, loadout.ResonanceSkillName), 26);

            var scroll = UIKit.ScrollList(col);
            scroll.parent.GetComponent<Image>().Size(700f);
            foreach (var stage in G.Db.Stages)
            {
                bool unlocked = G.IsStageUnlocked(stage.Id);
                bool cleared = G.State.ClearedStages.Contains(stage.Id);
                var row = UIKit.HList(scroll, 12f);
                row.Size(80f);
                UIKit.Label(row, (cleared ? "✓ " : "") + stage.Name + (stage.HasBoss ? "  [Boss]" : ""), 28);
                UIKit.Label(row, "体力 " + stage.StaminaCost + "   波次 " + stage.Waves, 26, TextAnchor.MiddleCenter, UIKit.TextDim);
                var id = stage.Id;
                var btn = UIKit.Button(row, unlocked ? "出发" : "未解锁", () => Hub.App.StartBattle(id, SelectedCharacter), unlocked ? UIKit.Accent * 0.8f : UIKit.PanelLight);
                btn.interactable = unlocked;
            }
        }
    }

    // ====================================================================== 心语

    public sealed class ChatPanel : HubPanel
    {
        private string _lastReply;
        private string _lastReplyCharacter;

        public override string Title { get { return "心语"; } }

        public override void Build(RectTransform root)
        {
            var col = UIKit.VList(root, 12f, 0);
            col.Stretch();
            CharacterTabs(col);
            var def = G.Db.Character(SelectedCharacter);

            var phone = UIKit.Block(col, "Phone", UIKit.Panel);
            phone.Size(760f);
            var msgs = UIKit.VList(phone.transform, 16f, 24);
            msgs.Stretch();

            Line(msgs, def.Name + " · " + def.Title, 30, UIKit.Accent);
            if (_lastReply != null && _lastReplyCharacter == SelectedCharacter)
                Line(msgs, def.Name + "：" + _lastReply, 30);

            var thread = G.NextChat(SelectedCharacter);
            if (thread == null)
            {
                Line(msgs, "（暂时没有新消息。提升好感后会收到更多话题。）", 28, UIKit.TextDim);
                return;
            }
            foreach (var l in thread.Opening) Line(msgs, def.Name + "：" + l, 30);
            Line(msgs, " ");
            for (int i = 0; i < thread.Options.Count; i++)
            {
                int idx = i;
                string tid = thread.Id;
                UIKit.Button(msgs, thread.Options[i].Text, () =>
                {
                    _lastReply = G.AnswerChat(tid, idx);
                    _lastReplyCharacter = SelectedCharacter;
                    Hub.Refresh();
                }).Size(72f);
            }
        }
    }

    // ====================================================================== 栖所

    public sealed class HavenPanel : HubPanel
    {
        private string _feedback = "";

        public override string Title { get { return "栖所"; } }

        private static readonly string[] Places = { "办公室公寓", "车库", "灯塔书房", "海上别墅" };
        private static readonly string[] WorkNames = { "整理卷宗", "一起修车", "修复古籍", "下一盘棋" };

        public override void Build(RectTransform root)
        {
            var col = UIKit.VList(root, 12f, 0);
            col.Stretch();
            CharacterTabs(col);
            int ci = G.Db.Characters.FindIndex(c => c.Id == SelectedCharacter);
            var def = G.Db.Characters[ci];
            var a = G.GetAffection(def.Id);

            var body = UIKit.HList(col, 16f);
            body.Size(760f);
            ArtPlaceholder(body, def.Name + "的" + Places[ci] + "\n（3D 栖所场景 + 实时表情动作）", UIKit.Hex(def.PlaceholderColorHex) * 0.7f);

            var side = UIKit.Block(body, "Side", UIKit.Panel);
            var list = UIKit.VList(side.transform, 12f, 20);
            list.Stretch();
            Line(list, "好感 Lv." + a.Level + "（" + a.Exp + "/" + AffectionRules.ExpToNext(a.Level) + "）", 30);
            Line(list, "心情 " + a.Mood + (a.Mood < HavenRules.ComfortThreshold ? "  · 他有点低落" : ""), 30);
            Line(list, "行动点 " + G.State.HavenActionPoints + "/" + HavenRules.DailyActionPoints + "   礼物 " + G.State.Wallet.Get(ItemIds.Gift), 28);
            Action(list, "聊聊天", HavenAction.Chat);
            Action(list, WorkNames[ci], HavenAction.Work);
            Action(list, "触碰（反应占位）", HavenAction.Touch);
            Action(list, "送礼物", HavenAction.Gift);
            Line(list, _feedback, 28, UIKit.Accent);
        }

        private void Action(Transform parent, string label, HavenAction action)
        {
            UIKit.Button(parent, label, () =>
            {
                var r = G.DoHaven(SelectedCharacter, action);
                switch (r.Code)
                {
                    case HavenResultCode.NoActionPoints: _feedback = "今天的行动点用完了，明天再来吧。"; break;
                    case HavenResultCode.NoGift: _feedback = "没有礼物了。"; break;
                    default:
                        _feedback = (r.ComfortTriggered ? "【安慰事件】他好像轻松了一点。" : "") + "好感 +" + r.AffectionGained + (r.LevelsGained > 0 ? "  好感升级！" : "");
                        break;
                }
                Hub.Refresh();
            }).Size(70f);
        }
    }

    // ====================================================================== 约会

    public sealed class DatePanel : HubPanel
    {
        private string _feedback = "";
        public override string Title { get { return "约会"; } }

        public override void Build(RectTransform root)
        {
            var col = UIKit.VList(root, 12f, 0);
            col.Stretch();
            CharacterTabs(col);
            var a = G.GetAffection(SelectedCharacter);
            Line(col, "今日潮汐：" + TideCalendar.DisplayName(G.TodayTide) + "   约会券：" + G.State.Wallet.Get(ItemIds.DateTicket) + "（每周一发放 2 张）", 30);
            if (!AffectionRules.IsUnlocked(a, RomanceFeature.Dating))
            {
                Line(col, "好感达到 Lv." + AffectionRules.RequiredLevel(RomanceFeature.Dating) + " 后解锁约会。", 30, UIKit.TextDim);
                return;
            }
            Line(col, "今天潮汐下可去的地点：", 28, UIKit.TextDim);
            foreach (var spot in G.TodayDateSpots())
            {
                string sid = spot.Id;
                string name = spot.Name;
                UIKit.Button(col, name, () =>
                {
                    _feedback = G.GoOnDate(SelectedCharacter, sid)
                        ? "和 " + G.Db.Character(SelectedCharacter).Name + " 在" + name + "度过了一段时光。（3D 约会剧情留白）"
                        : "约会券不足。";
                    Hub.Refresh();
                }).Size(72f);
            }
            Line(col, _feedback, 28, UIKit.Accent);
        }
    }

    // ====================================================================== 卡牌 · 心核盘

    public sealed class CardPanel : HubPanel
    {
        private int _selectedSlot = -1;
        private string _feedback = "";

        public override string Title { get { return "回响卡·心核盘"; } }

        public override void Build(RectTransform root)
        {
            var col = UIKit.VList(root, 10f, 0);
            col.Stretch();
            CharacterTabs(col);

            var body = UIKit.Rect(col, "Body");
            body.Size(780f);

            // 左：心核盘
            var boardBg = UIKit.Block(body, "Board", UIKit.Panel);
            boardBg.rectTransform.Anchor(0f, 0f, 0.42f, 1f);
            BuildBoard(boardBg.rectTransform);

            // 右：卡牌列表
            var listBg = UIKit.Block(body, "Cards", UIKit.Panel);
            listBg.rectTransform.Anchor(0.44f, 0f, 1f, 1f);
            var scroll = UIKit.ScrollList(listBg.transform, 10f);
            scroll.parent.GetComponent<RectTransform>().Stretch(10f);
            BuildCardList(scroll);
        }

        private void BuildBoard(RectTransform parent)
        {
            var board = G.GetBoard(SelectedCharacter);
            var aff = G.GetAffection(SelectedCharacter);
            var bonus = board.Evaluate(G.Db.Card);

            var area = UIKit.Rect(parent, "Hex");
            area.Anchor(0f, 0.32f, 1f, 1f);
            const float size = 95f;
            for (int i = 0; i < HeartCoreBoard.MaxSlots; i++)
            {
                var hex = HeartCoreBoard.SlotOrder[i];
                float x = size * 1.5f * hex.Q;
                float y = -size * Mathf.Sqrt(3f) * (hex.R + hex.Q / 2f);
                bool unlocked = i < board.UnlockedSlots;
                string cardId = board.SlotCardIds[i];
                var def = G.Db.Card(cardId);
                string label = !unlocked ? "未解锁" : def == null ? (i == 0 ? "主卡位" : "空") :
                    def.Rarity + "\n" + EmotionRules.DisplayName(def.Emotion) + "\n" + Short(def.Name);
                Color color = !unlocked ? new Color(0.1f, 0.1f, 0.1f) : def == null ? UIKit.PanelLight : EmotionColor(def.Emotion);
                if (i == _selectedSlot) color = UIKit.Accent;
                int slot = i;
                var btn = UIKit.Button(area, label, () => { _selectedSlot = slot; _feedback = "已选中第 " + (slot + 1) + " 格，点右侧卡牌放入"; Hub.Refresh(); }, color, 20);
                btn.interactable = unlocked;
                var rt = (RectTransform)btn.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(size * 1.6f, size * 1.4f);
                rt.anchoredPosition = new Vector2(x, y);
            }

            var info = UIKit.VList(parent, 6f, 16);
            info.Anchor(0f, 0f, 1f, 0.32f);
            Line(info, "情绪连线：最长 " + bonus.LongestChain + " 连   攻击 +" + (bonus.AttackBonus * 100f).ToString("0") + "%   判定窗口 +" + bonus.TideWindowBonus.ToString("0.00") + "s", 24);
            Line(info, bonus.HeartOverflow ? "心潮满溢：共鸣技伤害 +50%" : "集齐 喜/哀/怒/惧/念 五色可触发「心潮满溢」", 24, bonus.HeartOverflow ? UIKit.Accent : UIKit.TextDim);
            Line(info, "男主等级 " + aff.PartnerLevel + "（每 10 级多一格）   " + _feedback, 24);
            var row = UIKit.HList(info, 10f);
            row.Size(64f);
            UIKit.Button(row, "一键镶嵌", () => { G.AutoArrange(SelectedCharacter); _feedback = ""; }, null, 26);
            UIKit.Button(row, "男主升级", () => { _feedback = G.LevelUpPartner(SelectedCharacter) ? "升级成功" : "材料不足"; Hub.Refresh(); }, null, 26);
        }

        private void BuildCardList(RectTransform list)
        {
            var cards = G.CardsOf(SelectedCharacter);
            Line(list, "持有 " + cards.Count + " 张   经验道具 " + G.State.Wallet.Get(ItemIds.CardExp) + "   突破材料 " + G.State.Wallet.Get(ItemIds.BreakthroughMat), 24, UIKit.TextDim);
            if (cards.Count == 0) Line(list, "还没有这位男主的卡牌，去抽卡吧。", 26, UIKit.TextDim);
            foreach (var inst in cards)
            {
                var def = G.Db.Card(inst.DefinitionId);
                var stats = inst.GetStats(def);
                var row = UIKit.Block(list, "Card", UIKit.PanelLight);
                row.Size(150f);
                var h = UIKit.HList(row.transform, 8f, 8);
                h.Stretch();
                var thumb = ArtPlaceholder(h, def.Rarity + "\n卡面", EmotionColor(def.Emotion) * 0.8f);
                thumb.Size(130f, 110f);
                UIKit.Label(h, string.Format("{0}  [{1}·{2}]{3}\nLv.{4}/{5}  突破 {6}  ★{7}{8}\n攻 {9:0} 生 {10:0} 防 {11:0}",
                    def.Name, def.Rarity, EmotionRules.DisplayName(def.Emotion), def.Limited ? " 限定" : "",
                    inst.Level, inst.LevelCap, inst.Breakthrough, inst.Stars, inst.Awakened ? " 已觉醒" : "",
                    stats.Attack, stats.MaxHp, stats.Defense), 22);
                var btns = UIKit.VList(h, 4f, 0);
                btns.Size(130f, 170f);
                string id = def.Id;
                UIKit.Button(btns, "放入", () =>
                {
                    if (_selectedSlot < 0) { _feedback = "先点左侧一个格子"; Hub.Refresh(); return; }
                    _feedback = G.PlaceCard(SelectedCharacter, _selectedSlot, id) ? "" : "这张卡已经在盘上了";
                    _selectedSlot = -1;
                    Hub.Refresh();
                }, null, 22).Size(40f);
                UIKit.Button(btns, "升级×5", () => { if (G.LevelUpCard(id, 5) < 0) { _feedback = "经验道具不足"; Hub.Refresh(); } }, null, 22).Size(40f);
                UIKit.Button(btns, inst.CanBreakthrough ? "突破" : "觉醒", () =>
                {
                    bool ok = inst.CanBreakthrough ? G.BreakthroughCard(id) : G.AwakenCard(id);
                    if (!ok) { _feedback = "条件或材料不足"; Hub.Refresh(); }
                }, null, 22).Size(40f);
            }
        }

        private static string Short(string s) { return s.Length > 5 ? s.Substring(0, 5) : s; }

        public static Color EmotionColor(Emotion e)
        {
            switch (e)
            {
                case Emotion.Joy: return UIKit.Hex("E7A33E");
                case Emotion.Sorrow: return UIKit.Hex("3E6FB0");
                case Emotion.Anger: return UIKit.Hex("B5403A");
                case Emotion.Fear: return UIKit.Hex("6B4E9B");
                default: return UIKit.Hex("3E9B85");
            }
        }
    }

    // ====================================================================== 抽卡

    public sealed class GachaPanel : HubPanel
    {
        private string _bannerId;
        private List<PullResult> _last;
        private string _feedback = "";

        public override string Title { get { return "抽卡"; } }

        public override void Build(RectTransform root)
        {
            if (_bannerId == null) _bannerId = G.Db.Banners[G.Db.Banners.Count - 1].Id;
            var col = UIKit.VList(root, 12f, 0);
            col.Stretch();

            var tabs = UIKit.HList(col, 10f);
            tabs.Size(70f);
            foreach (var b in G.Db.Banners)
            {
                string id = b.Id;
                UIKit.Button(tabs, b.Name, () => { _bannerId = id; _last = null; Hub.Refresh(); }, id == _bannerId ? UIKit.Accent * 0.8f : UIKit.PanelLight, 26);
            }

            var banner = G.Db.Banner(_bannerId);
            var pity = G.GetPity(banner.PityGroup);
            var body = UIKit.HList(col, 16f);
            body.Size(560f);
            ArtPlaceholder(body, banner.Name + "\n卡池 KV", UIKit.PanelLight);
            var side = UIKit.Block(body, "Info", UIKit.Panel);
            var info = UIKit.VList(side.transform, 10f, 20);
            info.Stretch();
            Line(info, string.Format("SSR {0:0.0}%  ·  SR {1:0}%  ·  {2} 抽必出 SSR", banner.SsrRate * 100, banner.SrRate * 100, banner.HardPity), 26);
            if (banner.Type == BannerType.Limited) Line(info, "出 SSR 时 50% 为限定；未中则下一张 SSR 必为限定。", 24, UIKit.TextDim);
            Line(info, "距保底还有 " + (banner.HardPity - pity.PullsSinceSsr) + " 抽" + (pity.FeaturedGuaranteed ? "   · 下次 SSR 必为限定" : ""), 28, UIKit.Accent);
            Line(info, "钻石 " + G.State.Wallet.Diamonds + "   单抽 " + banner.CostPerPull, 28);
            var row = UIKit.HList(info, 12f);
            row.Size(80f);
            UIKit.Button(row, "抽 1 次", () => DoPull(1), null, 30);
            UIKit.Button(row, "抽 10 次", () => DoPull(10), UIKit.Accent * 0.8f, 30);
            Line(info, _feedback, 26, UIKit.Danger);
            Line(info, "（概率公示与未成年人限制按上线地区法规接入）", 22, UIKit.TextDim);

            if (_last != null)
            {
                var res = UIKit.HList(col, 6f);
                res.Size(150f);
                foreach (var r in _last)
                {
                    var def = G.Db.Card(r.CardId);
                    Color c = r.Rarity == Rarity.SSR ? UIKit.Accent : r.Rarity == Rarity.SR ? UIKit.Hex("8E6BC8") : UIKit.PanelLight;
                    var img = UIKit.Block(res, "Result", c);
                    UIKit.Label(img.transform, r.Rarity + "\n" + G.Db.Character(def.CharacterId).Name + "\n" + def.Name + (r.IsNew ? "\nNEW" : r.StarUp ? "\n★+1" : ""), 20, TextAnchor.MiddleCenter, r.Rarity == Rarity.SSR ? Color.black : UIKit.TextColor).rectTransform.Stretch(4f);
                }
            }
        }

        private void DoPull(int count)
        {
            _last = G.Pull(_bannerId, count);
            _feedback = _last == null ? "钻石不足" : "";
            Hub.Refresh();
        }
    }

    // ====================================================================== 记忆组

    public sealed class MemoryPanel : HubPanel
    {
        public override string Title { get { return "记忆组"; } }

        public override void Build(RectTransform root)
        {
            var col = UIKit.VList(root, 14f, 0);
            col.Stretch();
            Line(col, "集齐同一记忆组的卡牌，可解锁「大退潮之夜」的隐藏记忆。", 28, UIKit.TextDim);
            foreach (var gid in G.Db.MemoryGroupIds())
            {
                var cards = G.Db.CardsInMemoryGroup(gid);
                var who = G.Db.Character(cards[0].CharacterId).Name;
                int owned = G.MemoryGroupOwned(gid);
                bool claimed = G.State.ClaimedMemoryGroups.Contains(gid);
                var sb = new StringBuilder();
                foreach (var c in cards) sb.Append(G.OwnsCard(c.Id) ? "● " : "○ ").Append(c.Name).Append("   ");

                var box = UIKit.Block(col, "Group", UIKit.Panel);
                box.Size(200f);
                var v = UIKit.VList(box.transform, 8f, 16);
                v.Stretch();
                Line(v, who + " · 记忆组 " + gid + "   " + owned + "/" + cards.Count, 30, UIKit.Accent);
                Line(v, sb.ToString(), 24);
                string id = gid;
                var btn = UIKit.Button(v, claimed ? "已解锁（隐藏剧情留白）" : owned == cards.Count ? "解锁隐藏记忆" : "未集齐", () => G.ClaimMemoryGroup(id));
                btn.Size(60f);
                btn.interactable = !claimed && owned == cards.Count;
            }
        }
    }

    // ====================================================================== 潮汐日历

    public sealed class CalendarPanel : HubPanel
    {
        public override string Title { get { return "潮汐日历"; } }

        public override void Build(RectTransform root)
        {
            var col = UIKit.VList(root, 12f, 0);
            col.Stretch();
            Line(col, "未来 7 天潮汐（提前公布，方便安排约会和陪伴）", 30, UIKit.Accent);
            var today = G.GameDate;
            string[] week = { "日", "一", "二", "三", "四", "五", "六" };
            for (int d = 0; d < 7; d++)
            {
                var date = today.AddDays(d);
                var tide = TideCalendar.GetTide(date);
                var sb = new StringBuilder();
                foreach (var c in G.Db.Characters)
                {
                    if (c.FavoredTide == tide) sb.Append(c.Name).Append(" 心情好  ");
                    if (c.TroubledTide == tide) sb.Append(c.Name).Append(" 低落  ");
                }
                var spots = new List<string>();
                foreach (var s in G.Db.DateSpots) if (s.Tides.Contains(tide)) spots.Add(s.Name);
                var row = UIKit.Block(col, "Day", d == 0 ? UIKit.PanelLight : UIKit.Panel);
                row.Size(92f);
                var t = UIKit.Label(row.transform, string.Format("{0:MM/dd} 周{1}   【{2}】   {3}\n可约会：{4}",
                    date, week[(int)date.DayOfWeek], TideCalendar.DisplayName(tide), sb, string.Join("、", spots.ToArray())), 26,
                    TextAnchor.MiddleLeft, tide == TideKind.StaticNight ? UIKit.Accent : UIKit.TextColor);
                t.rectTransform.Stretch(14f);
            }
        }
    }

    // ====================================================================== 设置

    public sealed class SettingsPanel : HubPanel
    {
        public override string Title { get { return "设置"; } }

        public override void Build(RectTransform root)
        {
            var col = UIKit.VList(root, 14f, 0);
            col.Stretch();
            Line(col, "开发版设置", 32, UIKit.Accent);
            Line(col, "版本 " + Application.version + "  ·  " + Application.platform, 26, UIKit.TextDim);
            UIKit.Button(col, "调试：+10000 钻石", () => { G.State.Wallet.Add(ItemIds.DiamondFree, 10000); G.MarkChanged(); }).Size(70f);
            UIKit.Button(col, "调试：+50 经验道具 / 突破材料 / 男主材料", () =>
            {
                G.State.Wallet.Add(ItemIds.CardExp, 50);
                G.State.Wallet.Add(ItemIds.BreakthroughMat, 50);
                G.State.Wallet.Add(ItemIds.PartnerMat, 50);
                G.State.Wallet.Add(ItemIds.Gold, 500000);
                G.MarkChanged();
            }).Size(70f);
            UIKit.Button(col, "调试：体力回满 / 行动点回满", () =>
            {
                G.State.Stamina.Current = StaminaState.Max;
                G.State.HavenActionPoints = HavenRules.DailyActionPoints;
                G.MarkChanged();
            }).Size(70f);
            UIKit.Button(col, "清空存档（重新开始）", () => Hub.App.ResetSave(), UIKit.Danger).Size(70f);
        }
    }
}
