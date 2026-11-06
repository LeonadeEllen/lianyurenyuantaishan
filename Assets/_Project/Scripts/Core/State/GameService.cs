using System;
using System.Collections.Generic;

namespace HearTheTide.Core
{
    /// <summary>
    /// 游戏逻辑入口：所有改动存档的操作都走这里，表现层只读状态、调用方法。
    /// 正式版这些方法会对应服务器接口。
    /// </summary>
    public sealed class GameService
    {
        public static readonly CombatStats PlayerBaseStats = new CombatStats(100, 1000, 50);
        public const int ExpPerCardExpItem = 100;
        public const int PartnerLevelCap = 60;

        public readonly ContentDatabase Db;
        public readonly GameState State;
        private readonly IClock _clock;
        private readonly IRandom _rng;

        public event Action Changed;

        public GameService(ContentDatabase db, GameState state, IClock clock, IRandom rng)
        {
            Db = db;
            State = state;
            _clock = clock;
            _rng = rng;
            Normalize();
        }

        public DateTime Now { get { return _clock.UtcNow; } }
        /// <summary>游戏日：按 UTC+8，凌晨 5 点换日（国内常见做法）。</summary>
        public DateTime GameDate { get { return _clock.UtcNow.AddHours(8).AddHours(-5).Date; } }

        private void NotifyChanged() { var h = Changed; if (h != null) h(); }

        /// <summary>外部直接改了 State（调试工具等）后调用，触发存档与界面刷新。</summary>
        public void MarkChanged() { NotifyChanged(); }

        // ------------------------------------------------------------ 初始化

        public static GameState CreateNewState(ContentDatabase db)
        {
            var s = new GameState();
            s.Wallet.Add(ItemIds.DiamondFree, 3000);
            s.Wallet.Add(ItemIds.Gold, 50000);
            s.Wallet.Add(ItemIds.CardExp, 30);
            s.Wallet.Add(ItemIds.BreakthroughMat, 15);
            s.Wallet.Add(ItemIds.PartnerMat, 20);
            s.Wallet.Add(ItemIds.Gift, 5);
            s.Wallet.Add(ItemIds.DateTicket, 2);
            foreach (var id in new[] { "shen_r_01", "shen_sr_01", "lu_r_01", "lu_sr_01" })
                s.Cards.Add(new CardInstance { DefinitionId = id });
            s.SelectedPartnerId = DefaultContent.Shen;
            return s;
        }

        /// <summary>补齐缺失结构（新增角色、旧存档升级）。</summary>
        private void Normalize()
        {
            foreach (var c in Db.Characters)
            {
                if (GetAffection(c.Id) == null) State.Affections.Add(new AffectionState { CharacterId = c.Id });
                var board = GetBoard(c.Id);
                if (board == null)
                {
                    board = new HeartCoreBoard(c.Id);
                    State.Boards.Add(board);
                    AutoArrange(c.Id, false);
                }
                board.EnsureSize();
                board.UnlockedSlots = HeartCoreBoard.SlotsForPartnerLevel(GetAffection(c.Id).PartnerLevel);
            }
            if (Db.Character(State.SelectedPartnerId) == null) State.SelectedPartnerId = Db.Characters[0].Id;
            State.SaveVersion = GameState.CurrentVersion;
        }

        // ------------------------------------------------------------ 日常 / 周常

        private static int DateKey(DateTime d) { return d.Year * 10000 + d.Month * 100 + d.Day; }

        public TideKind TodayTide { get { return TideCalendar.GetTide(GameDate); } }

        /// <summary>登录与每次回到主界面时调用。返回是否发生了重置。</summary>
        public bool RefreshDaily()
        {
            State.Stamina.Refresh(Now);
            bool changed = false;
            int today = DateKey(GameDate);
            if (State.LastDailyResetDate != today)
            {
                State.LastDailyResetDate = today;
                State.HavenActionPoints = HavenRules.DailyActionPoints;
                var tide = TodayTide;
                foreach (var a in State.Affections)
                {
                    var def = Db.Character(a.CharacterId);
                    int mood = 60;
                    if (def != null && def.FavoredTide == tide) mood += 20;
                    if (def != null && def.TroubledTide == tide) mood -= 35;
                    a.Mood = mood;
                }
                changed = true;
            }
            int week = DateKey(TideCalendar.WeekStart(GameDate));
            if (State.LastWeeklyResetDate != week)
            {
                State.LastWeeklyResetDate = week;
                State.Wallet.Add(ItemIds.DateTicket, 2);
                changed = true;
            }
            if (changed) NotifyChanged();
            return changed;
        }

        // ------------------------------------------------------------ 查询

        public AffectionState GetAffection(string characterId) { return State.Affections.Find(a => a.CharacterId == characterId); }
        public HeartCoreBoard GetBoard(string characterId) { return State.Boards.Find(b => b.CharacterId == characterId); }
        public CardInstance GetCard(string defId) { return State.Cards.Find(c => c.DefinitionId == defId); }
        public bool OwnsCard(string defId) { return GetCard(defId) != null; }

        public List<CardInstance> CardsOf(string characterId)
        {
            return State.Cards.FindAll(c => { var d = Db.Card(c.DefinitionId); return d != null && d.CharacterId == characterId; });
        }

        // ------------------------------------------------------------ 卡牌

        /// <summary>获得一张卡：新卡入库；重复卡升星，满星转回响币。</summary>
        public PullResult GrantCard(string defId)
        {
            var def = Db.Card(defId);
            if (def == null) throw new ArgumentException("未知卡牌: " + defId);
            var result = new PullResult { CardId = defId, Rarity = def.Rarity };
            result.EchoCoins = GachaLogic.BaseEchoCoins(def.Rarity);
            var owned = GetCard(defId);
            if (owned == null)
            {
                State.Cards.Add(new CardInstance { DefinitionId = defId });
                result.IsNew = true;
            }
            else if (owned.Stars < CardInstance.MaxStars)
            {
                owned.Stars++;
                result.StarUp = true;
            }
            else
            {
                result.EchoCoins += GachaLogic.EchoCoinMaxStarDuplicate;
            }
            State.Wallet.Add(ItemIds.EchoCoin, result.EchoCoins);
            return result;
        }

        /// <summary>消耗经验道具升级。返回提升的等级数，材料不足返回 -1。</summary>
        public int LevelUpCard(string defId, int expItems)
        {
            var card = GetCard(defId);
            if (card == null || expItems <= 0) return -1;
            if (!State.Wallet.TrySpend(ItemIds.CardExp, expItems)) return -1;
            int gained = card.AddExp(expItems * ExpPerCardExpItem);
            NotifyChanged();
            return gained;
        }

        public bool BreakthroughCard(string defId)
        {
            var card = GetCard(defId);
            if (card == null || !card.CanBreakthrough) return false;
            int mat = CardCosts.BreakthroughMaterial(card.Breakthrough);
            int gold = CardCosts.BreakthroughGold(card.Breakthrough);
            if (State.Wallet.Get(ItemIds.BreakthroughMat) < mat || State.Wallet.Get(ItemIds.Gold) < gold) return false;
            State.Wallet.TrySpend(ItemIds.BreakthroughMat, mat);
            State.Wallet.TrySpend(ItemIds.Gold, gold);
            card.Breakthrough++;
            card.AddExp(0);
            NotifyChanged();
            return true;
        }

        public bool AwakenCard(string defId)
        {
            var card = GetCard(defId);
            if (card == null || !card.CanAwaken) return false;
            if (!State.Wallet.TrySpend(ItemIds.AwakenMat, CardCosts.AwakenMaterial)) return false;
            card.Awakened = true;
            NotifyChanged();
            return true;
        }

        // ------------------------------------------------------------ 心核盘

        public bool PlaceCard(string characterId, int slot, string defId)
        {
            var board = GetBoard(characterId);
            if (board == null || !OwnsCard(defId)) return false;
            bool ok = board.TryPlace(slot, Db.Card(defId));
            if (ok) NotifyChanged();
            return ok;
        }

        /// <summary>一键镶嵌：最强卡放中心，其余按稀有度、等级填充。</summary>
        public void AutoArrange(string characterId, bool notify = true)
        {
            var board = GetBoard(characterId);
            if (board == null) return;
            var cards = CardsOf(characterId);
            cards.Sort((a, b) =>
            {
                var da = Db.Card(a.DefinitionId);
                var dbb = Db.Card(b.DefinitionId);
                int r = dbb.Rarity.CompareTo(da.Rarity);
                return r != 0 ? r : b.Level.CompareTo(a.Level);
            });
            for (int i = 0; i < HeartCoreBoard.MaxSlots; i++) board.Clear(i);
            for (int i = 0; i < cards.Count && i < board.UnlockedSlots; i++)
                board.TryPlace(i, Db.Card(cards[i].DefinitionId));
            if (notify) NotifyChanged();
        }

        public bool LevelUpPartner(string characterId)
        {
            var a = GetAffection(characterId);
            if (a == null || a.PartnerLevel >= PartnerLevelCap) return false;
            int mat = 1 + a.PartnerLevel / 10;
            int gold = 1000 * a.PartnerLevel;
            if (State.Wallet.Get(ItemIds.PartnerMat) < mat || State.Wallet.Get(ItemIds.Gold) < gold) return false;
            State.Wallet.TrySpend(ItemIds.PartnerMat, mat);
            State.Wallet.TrySpend(ItemIds.Gold, gold);
            a.PartnerLevel++;
            GetBoard(characterId).UnlockedSlots = HeartCoreBoard.SlotsForPartnerLevel(a.PartnerLevel);
            NotifyChanged();
            return true;
        }

        public Loadout ComputeLoadout(string partnerId)
        {
            var def = Db.Character(partnerId);
            var board = GetBoard(partnerId);
            var affection = GetAffection(partnerId);
            var bonus = board.Evaluate(Db.Card);

            var cardStats = new CombatStats();
            foreach (var id in board.PlacedCardIds())
            {
                var inst = GetCard(id);
                var cdef = Db.Card(id);
                if (inst != null && cdef != null) cardStats = cardStats + inst.GetStats(cdef);
            }

            float atkMul = 1f + bonus.AttackBonus;
            float partnerLevelMul = 1f + (affection.PartnerLevel - 1) * 0.03f;
            var center = Db.Card(board.CenterCardId);

            var loadout = new Loadout
            {
                PartnerId = partnerId,
                PlayerStats = (PlayerBaseStats + cardStats).Scale(atkMul, 1f, 1f),
                PartnerStats = def.BaseStats.Scale(partnerLevelMul * atkMul, partnerLevelMul, partnerLevelMul),
                TideWindow = TideJudge.DefaultWindow + bonus.TideWindowBonus,
                ResonanceDamageMultiplier = 8f * (1f + bonus.ResonanceDamageBonus),
                SkillMultiplier = center != null ? center.SkillMultiplier : 1.5f,
                ResonanceSkillName = center != null && center.Rarity == Rarity.SSR ? center.Name : def.ResonanceSkillName,
                Board = bonus,
            };
            return loadout;
        }

        // ------------------------------------------------------------ 抽卡

        public PityState GetPity(string group)
        {
            var p = State.Pity.Find(x => x.Group == group);
            if (p == null) { p = new PityState { Group = group }; State.Pity.Add(p); }
            return p;
        }

        /// <summary>抽 count 次。钻石不足返回 null。</summary>
        public List<PullResult> Pull(string bannerId, int count)
        {
            var banner = Db.Banner(bannerId);
            if (banner == null || count <= 0) return null;
            if (!State.Wallet.TrySpendDiamonds(banner.CostPerPull * count)) return null;
            var pity = GetPity(banner.PityGroup);
            var results = new List<PullResult>();
            for (int i = 0; i < count; i++)
            {
                var roll = GachaLogic.PullOne(banner, pity, _rng);
                var grant = GrantCard(roll.CardId);
                grant.IsFeatured = roll.IsFeatured;
                results.Add(grant);
            }
            // 新卡自动补进空格
            foreach (var r in results)
            {
                if (!r.IsNew) continue;
                var cdef = Db.Card(r.CardId);
                var board = GetBoard(cdef.CharacterId);
                for (int s = 0; s < board.UnlockedSlots; s++)
                    if (string.IsNullOrEmpty(board.SlotCardIds[s])) { board.TryPlace(s, cdef); break; }
            }
            NotifyChanged();
            return results;
        }

        // ------------------------------------------------------------ 记忆组

        public int MemoryGroupOwned(string groupId)
        {
            int n = 0;
            foreach (var c in Db.CardsInMemoryGroup(groupId)) if (OwnsCard(c.Id)) n++;
            return n;
        }

        public bool IsMemoryGroupComplete(string groupId)
        {
            return MemoryGroupOwned(groupId) == Db.CardsInMemoryGroup(groupId).Count;
        }

        /// <summary>集齐记忆组后领取：解锁隐藏剧情 + 奖励。</summary>
        public bool ClaimMemoryGroup(string groupId)
        {
            if (State.ClaimedMemoryGroups.Contains(groupId) || !IsMemoryGroupComplete(groupId)) return false;
            State.ClaimedMemoryGroups.Add(groupId);
            State.Wallet.Add(ItemIds.DiamondFree, 300);
            NotifyChanged();
            return true;
        }

        // ------------------------------------------------------------ 心语

        public ChatThread NextChat(string characterId)
        {
            var a = GetAffection(characterId);
            return Db.ChatThreads.Find(t => t.CharacterId == characterId
                                            && !State.CompletedChats.Contains(t.Id)
                                            && a.Level >= t.MinAffectionLevel);
        }

        /// <summary>回复消息。返回男主的回复文本，无效时返回 null。</summary>
        public string AnswerChat(string threadId, int optionIndex)
        {
            var t = Db.ChatThreads.Find(x => x.Id == threadId);
            if (t == null || State.CompletedChats.Contains(threadId)) return null;
            if (optionIndex < 0 || optionIndex >= t.Options.Count) return null;
            var opt = t.Options[optionIndex];
            State.CompletedChats.Add(threadId);
            AffectionRules.AddExp(GetAffection(t.CharacterId), opt.AffectionDelta * 10);
            NotifyChanged();
            return opt.Reply;
        }

        // ------------------------------------------------------------ 栖所

        public HavenResult DoHaven(string characterId, HavenAction action)
        {
            var result = new HavenResult();
            if (State.HavenActionPoints <= 0) { result.Code = HavenResultCode.NoActionPoints; return result; }
            if (action == HavenAction.Gift && !State.Wallet.TrySpend(ItemIds.Gift, 1)) { result.Code = HavenResultCode.NoGift; return result; }

            var a = GetAffection(characterId);
            State.HavenActionPoints--;
            int gain = HavenRules.AffectionGain(action);
            if (a.Mood < HavenRules.ComfortThreshold)
            {
                result.ComfortTriggered = true;
                gain *= 2;
            }
            result.AffectionGained = gain;
            result.LevelsGained = AffectionRules.AddExp(a, gain);
            AffectionRules.AddMood(a, HavenRules.MoodGain(action));
            NotifyChanged();
            return result;
        }

        // ------------------------------------------------------------ 约会

        public List<DateSpotDefinition> TodayDateSpots()
        {
            var tide = TodayTide;
            return Db.DateSpots.FindAll(s => s.Tides.Contains(tide));
        }

        public bool GoOnDate(string characterId, string spotId)
        {
            var a = GetAffection(characterId);
            if (!AffectionRules.IsUnlocked(a, RomanceFeature.Dating)) return false;
            if (TodayDateSpots().Find(s => s.Id == spotId) == null) return false;
            if (!State.Wallet.TrySpend(ItemIds.DateTicket, 1)) return false;
            AffectionRules.AddExp(a, 60);
            AffectionRules.AddMood(a, 15);
            NotifyChanged();
            return true;
        }

        // ------------------------------------------------------------ 关卡

        public bool TryStartStage(string stageId)
        {
            var stage = Db.Stage(stageId);
            if (stage == null) return false;
            bool ok = State.Stamina.TrySpend(stage.StaminaCost, Now);
            if (ok) NotifyChanged();
            return ok;
        }

        public StageReward CompleteStage(string stageId, string partnerId)
        {
            var stage = Db.Stage(stageId);
            var reward = new StageReward
            {
                Gold = stage.RewardGold,
                Diamonds = stage.RewardDiamond,
                CardExpItems = stage.RewardCardExp,
                BreakthroughMats = stage.RewardBreakthroughMat,
                FirstClear = !State.ClearedStages.Contains(stageId),
                AffectionGained = 5,
            };
            if (reward.FirstClear)
            {
                State.ClearedStages.Add(stageId);
                reward.Diamonds += 50;
            }
            State.Wallet.Add(ItemIds.Gold, reward.Gold);
            State.Wallet.Add(ItemIds.DiamondFree, reward.Diamonds);
            State.Wallet.Add(ItemIds.CardExp, reward.CardExpItems);
            State.Wallet.Add(ItemIds.BreakthroughMat, reward.BreakthroughMats);
            AffectionRules.AddExp(GetAffection(partnerId), reward.AffectionGained);
            NotifyChanged();
            return reward;
        }

        public bool IsStageUnlocked(string stageId)
        {
            int idx = Db.Stages.FindIndex(s => s.Id == stageId);
            if (idx < 0) return false;
            if (!stageId.StartsWith("main_")) return State.ClearedStages.Contains("main_1_2");
            if (idx == 0) return true;
            return State.ClearedStages.Contains(Db.Stages[idx - 1].Id);
        }
    }
}
