using System;
using System.Collections.Generic;
using HearTheTide.Core;
using NUnit.Framework;

namespace HearTheTide.Tests
{
    /// <summary>固定序列的随机数，用于精确测试抽卡。</summary>
    internal sealed class FixedRandom : IRandom
    {
        private readonly Queue<double> _values;
        private readonly double _fallback;
        public FixedRandom(double fallback, params double[] values) { _fallback = fallback; _values = new Queue<double>(values); }
        public double NextDouble() { return _values.Count > 0 ? _values.Dequeue() : _fallback; }
        public int Next(int maxExclusive) { return 0; }
    }

    internal sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; }
        public FakeClock(DateTime now) { UtcNow = now; }
    }

    public class TideJudgeTests
    {
        [Test]
        public void BlueParryInWindowRebounds()
        {
            Assert.AreEqual(TideOutcome.Rebound, TideJudge.Judge(TideCue.Blue, TideResponse.Parry, 0.1f, 0.25f));
            Assert.AreEqual(TideOutcome.Rebound, TideJudge.Judge(TideCue.Blue, TideResponse.Parry, -0.25f, 0.25f));
        }

        [Test]
        public void BlueEarlyParryMitigatesLateParryHits()
        {
            Assert.AreEqual(TideOutcome.Mitigated, TideJudge.Judge(TideCue.Blue, TideResponse.Parry, -0.6f, 0.25f));
            Assert.AreEqual(TideOutcome.Hit, TideJudge.Judge(TideCue.Blue, TideResponse.Parry, 0.6f, 0.25f));
        }

        [Test]
        public void WhiteCannotBeParried()
        {
            Assert.AreEqual(TideOutcome.Hit, TideJudge.Judge(TideCue.White, TideResponse.Parry, 0f, 0.25f));
            Assert.AreEqual(TideOutcome.Rebound, TideJudge.Judge(TideCue.White, TideResponse.Dodge, 0f, 0.25f));
        }

        [Test]
        public void RedNeedsInterruptBeforeImpact()
        {
            Assert.AreEqual(TideOutcome.Interrupted, TideJudge.Judge(TideCue.Red, TideResponse.Interrupt, -1f, 0.25f));
            Assert.AreEqual(TideOutcome.Hit, TideJudge.Judge(TideCue.Red, TideResponse.Interrupt, 0.1f, 0.25f));
            Assert.AreEqual(TideOutcome.Hit, TideJudge.Judge(TideCue.Red, TideResponse.Parry, 0f, 0.25f));
        }

        [Test]
        public void WiderWindowFromBoardBonus()
        {
            Assert.AreEqual(TideOutcome.Mitigated, TideJudge.Judge(TideCue.Blue, TideResponse.Parry, -0.28f, 0.25f));
            Assert.AreEqual(TideOutcome.Rebound, TideJudge.Judge(TideCue.Blue, TideResponse.Parry, -0.28f, 0.30f));
        }
    }

    public class ResonanceTests
    {
        [Test]
        public void FiveReboundsFillGauge()
        {
            var g = new ResonanceGauge();
            for (int i = 0; i < 4; i++) g.Add(ResonanceGauge.ReboundGain);
            Assert.IsFalse(g.TryConsume());
            g.Add(ResonanceGauge.ReboundGain);
            Assert.IsTrue(g.TryConsume());
            Assert.AreEqual(0f, g.Value);
        }

        [Test]
        public void GaugeClampsAtMax()
        {
            var g = new ResonanceGauge();
            g.Add(500f);
            Assert.AreEqual(ResonanceGauge.Max, g.Value);
        }
    }

    public class CardTests
    {
        [Test]
        public void LevelStopsAtBreakthroughCap()
        {
            var c = new CardInstance();
            c.AddExp(1000000);
            Assert.AreEqual(20, c.Level);
            Assert.IsTrue(c.CanBreakthrough);
            c.Breakthrough = 1;
            c.AddExp(0);
            Assert.Greater(c.Level, 20);
        }

        [Test]
        public void StatsGrowWithLevelAndStars()
        {
            var def = new CardDefinition { BaseAttack = 100, BaseHp = 1000, BaseDefense = 10 };
            var c = new CardInstance();
            float a1 = c.GetStats(def).Attack;
            c.Level = 11;
            c.Stars = 2;
            Assert.Greater(c.GetStats(def).Attack, a1);
        }
    }

    public class HeartCoreBoardTests
    {
        private static CardDefinition C(string id, Emotion e) { return new CardDefinition { Id = id, CharacterId = "x", Emotion = e }; }

        [Test]
        public void EmotionCycle()
        {
            Assert.IsTrue(EmotionRules.CanLink(Emotion.Joy, Emotion.Longing));
            Assert.IsTrue(EmotionRules.CanLink(Emotion.Longing, Emotion.Joy));
            Assert.IsTrue(EmotionRules.CanLink(Emotion.Anger, Emotion.Joy));
            Assert.IsFalse(EmotionRules.CanLink(Emotion.Joy, Emotion.Sorrow));
        }

        [Test]
        public void ThreeChainGivesWindowBonus()
        {
            var cards = new Dictionary<string, CardDefinition>
            {
                { "a", C("a", Emotion.Joy) }, { "b", C("b", Emotion.Longing) }, { "c", C("c", Emotion.Joy) },
            };
            var board = new HeartCoreBoard("x");
            Assert.IsTrue(board.TryPlace(0, cards["a"]));
            Assert.IsTrue(board.TryPlace(1, cards["b"]));
            Assert.IsTrue(board.TryPlace(2, cards["c"]));
            var bonus = board.Evaluate(id => cards.ContainsKey(id) ? cards[id] : null);
            Assert.AreEqual(3, bonus.LongestChain);
            Assert.AreEqual(0.05f, bonus.TideWindowBonus, 1e-6);
            Assert.IsFalse(bonus.HeartOverflow);
        }

        [Test]
        public void UnlinkedCardsGiveNoBonus()
        {
            var cards = new Dictionary<string, CardDefinition> { { "a", C("a", Emotion.Joy) }, { "b", C("b", Emotion.Sorrow) } };
            var board = new HeartCoreBoard("x");
            board.TryPlace(0, cards["a"]);
            board.TryPlace(1, cards["b"]);
            var bonus = board.Evaluate(id => cards[id]);
            Assert.AreEqual(0, bonus.LongestChain);
            Assert.AreEqual(0f, bonus.AttackBonus);
        }

        [Test]
        public void FiveEmotionsTriggerOverflow()
        {
            var cards = new Dictionary<string, CardDefinition>();
            var board = new HeartCoreBoard("x") { UnlockedSlots = 7 };
            int i = 0;
            foreach (Emotion e in Enum.GetValues(typeof(Emotion)))
            {
                var c = C("e" + i, e);
                cards[c.Id] = c;
                Assert.IsTrue(board.TryPlace(i, c));
                i++;
            }
            Assert.IsTrue(board.Evaluate(id => cards[id]).HeartOverflow);
        }

        [Test]
        public void RejectsLockedSlotWrongCharacterAndDuplicates()
        {
            var board = new HeartCoreBoard("x");
            var a = C("a", Emotion.Joy);
            Assert.IsFalse(board.TryPlace(5, a), "格子未解锁");
            Assert.IsFalse(board.TryPlace(0, new CardDefinition { Id = "z", CharacterId = "y" }), "别的男主的卡");
            Assert.IsTrue(board.TryPlace(0, a));
            Assert.IsFalse(board.TryPlace(1, a), "同一张卡不能放两格");
        }

        [Test]
        public void SlotsGrowWithPartnerLevel()
        {
            Assert.AreEqual(3, HeartCoreBoard.SlotsForPartnerLevel(1));
            Assert.AreEqual(4, HeartCoreBoard.SlotsForPartnerLevel(11));
            Assert.AreEqual(7, HeartCoreBoard.SlotsForPartnerLevel(60));
        }
    }

    public class GachaTests
    {
        private static BannerDefinition Limited()
        {
            return new BannerDefinition
            {
                Id = "l", Type = BannerType.Limited, PityGroup = "l",
                FeaturedSsrIds = new List<string> { "lim" }, SsrPool = new List<string> { "std" },
                SrPool = new List<string> { "sr" }, RPool = new List<string> { "r" },
            };
        }

        [Test]
        public void HardPityGuaranteesSsr()
        {
            var banner = Limited();
            var pity = new PityState();
            var rng = new FixedRandom(0.99); // 永远不中
            PullResult last = default(PullResult);
            for (int i = 0; i < banner.HardPity; i++) last = GachaLogic.PullOne(banner, pity, rng);
            Assert.AreEqual(Rarity.SSR, last.Rarity);
            Assert.AreEqual(0, pity.PullsSinceSsr);
        }

        [Test]
        public void LosingFiftyFiftyGuaranteesNextFeatured()
        {
            var banner = Limited();
            var pity = new PityState();
            // 第一抽：0.001 中 SSR，0.9 歪了
            var first = GachaLogic.PullOne(banner, pity, new FixedRandom(0.99, 0.001, 0.9));
            Assert.AreEqual("std", first.CardId);
            Assert.IsTrue(pity.FeaturedGuaranteed);
            // 第二次 SSR：即使 0.9 也必中限定
            var second = GachaLogic.PullOne(banner, pity, new FixedRandom(0.99, 0.001, 0.9));
            Assert.AreEqual("lim", second.CardId);
            Assert.IsFalse(pity.FeaturedGuaranteed);
        }

        [Test]
        public void TenPullsAlwaysContainSrOrBetter()
        {
            var banner = Limited();
            var pity = new PityState();
            var rng = new FixedRandom(0.99);
            bool hasSr = false;
            for (int i = 0; i < 10; i++) if (GachaLogic.PullOne(banner, pity, rng).Rarity != Rarity.R) hasSr = true;
            Assert.IsTrue(hasSr);
        }

        [Test]
        public void RatesRoughlyMatchOverManyPulls()
        {
            var banner = Limited();
            var pity = new PityState();
            var rng = new SystemRandom(42);
            int ssr = 0, n = 200000;
            for (int i = 0; i < n; i++) if (GachaLogic.PullOne(banner, pity, rng).Rarity == Rarity.SSR) ssr++;
            double rate = (double)ssr / n;
            // 1.5% 基础 + 70 抽保底，综合概率应在 1.5%–3% 之间
            Assert.Greater(rate, 0.015);
            Assert.Less(rate, 0.03);
        }
    }

    public class EconomyTests
    {
        [Test]
        public void StaminaRegenerates()
        {
            var t0 = new DateTime(2026, 11, 6, 0, 0, 0, DateTimeKind.Utc);
            var s = new StaminaState { Current = 120 };
            s.Refresh(t0);
            Assert.IsTrue(s.TrySpend(60, t0));
            s.Refresh(t0.AddMinutes(61));
            Assert.AreEqual(70, s.Current);
            s.Refresh(t0.AddDays(1));
            Assert.AreEqual(StaminaState.Max, s.Current);
        }

        [Test]
        public void WalletSpendsFreeDiamondsFirst()
        {
            var w = new Wallet();
            w.Add(ItemIds.DiamondFree, 100);
            w.Add(ItemIds.DiamondPaid, 100);
            Assert.IsTrue(w.TrySpendDiamonds(150));
            Assert.AreEqual(0, w.Get(ItemIds.DiamondFree));
            Assert.AreEqual(50, w.Get(ItemIds.DiamondPaid));
            Assert.IsFalse(w.TrySpendDiamonds(51));
        }
    }

    public class CalendarTests
    {
        [Test]
        public void ExactlyOneStaticNightPerWeek()
        {
            var start = new DateTime(2026, 11, 2); // 周一
            for (int w = 0; w < 20; w++)
            {
                int count = 0;
                for (int d = 0; d < 7; d++)
                    if (TideCalendar.GetTide(start.AddDays(w * 7 + d)) == TideKind.StaticNight) count++;
                Assert.AreEqual(1, count, "第 " + w + " 周");
            }
        }

        [Test]
        public void TideIsDeterministic()
        {
            var d = new DateTime(2026, 12, 24);
            Assert.AreEqual(TideCalendar.GetTide(d), TideCalendar.GetTide(d.AddHours(13)));
        }
    }

    public class GameServiceTests
    {
        private static GameService NewService(IRandom rng = null)
        {
            var db = DefaultContent.Create();
            var clock = new FakeClock(new DateTime(2026, 11, 6, 4, 0, 0, DateTimeKind.Utc));
            var svc = new GameService(db, GameService.CreateNewState(db), clock, rng ?? new SystemRandom(1));
            svc.RefreshDaily();
            return svc;
        }

        [Test]
        public void NewGameHasBoardsWithStarterCards()
        {
            var svc = NewService();
            Assert.AreEqual(4, svc.State.Boards.Count);
            Assert.AreEqual("shen_sr_01", svc.GetBoard(DefaultContent.Shen).CenterCardId);
            var loadout = svc.ComputeLoadout(DefaultContent.Shen);
            Assert.Greater(loadout.PlayerStats.Attack, GameService.PlayerBaseStats.Attack);
        }

        [Test]
        public void DefaultContentIsConsistent()
        {
            var db = DefaultContent.Create();
            foreach (var b in db.Banners)
            {
                foreach (var id in b.FeaturedSsrIds) Assert.AreEqual(Rarity.SSR, db.Card(id).Rarity, id);
                foreach (var id in b.SsrPool) Assert.AreEqual(Rarity.SSR, db.Card(id).Rarity, id);
                foreach (var id in b.SrPool) Assert.AreEqual(Rarity.SR, db.Card(id).Rarity, id);
                foreach (var id in b.RPool) Assert.AreEqual(Rarity.R, db.Card(id).Rarity, id);
            }
            foreach (var c in db.Cards) Assert.IsNotNull(db.Character(c.CharacterId), c.Id);
            foreach (var g in db.MemoryGroupIds())
                Assert.IsTrue(db.CardsInMemoryGroup(g).Exists(c => !c.Limited), "记忆组必须包含非限定卡: " + g);
        }

        [Test]
        public void PullSpendsDiamondsAndGrantsCards()
        {
            var svc = NewService();
            int before = svc.State.Wallet.Diamonds;
            var results = svc.Pull("limited_shen_01", 10);
            Assert.AreEqual(10, results.Count);
            Assert.AreEqual(before - 1500, svc.State.Wallet.Diamonds);
            Assert.Greater(svc.State.Wallet.Get(ItemIds.EchoCoin), 0);
        }

        [Test]
        public void PullFailsWithoutDiamonds()
        {
            var svc = NewService();
            svc.State.Wallet.TrySpendDiamonds(svc.State.Wallet.Diamonds);
            Assert.IsNull(svc.Pull("standard", 1));
        }

        [Test]
        public void DuplicateCardStarsUp()
        {
            var svc = NewService();
            var r = svc.GrantCard("shen_r_01");
            Assert.IsTrue(r.StarUp);
            Assert.AreEqual(2, svc.GetCard("shen_r_01").Stars);
        }

        [Test]
        public void HavenUsesActionPointsAndComfortDoubles()
        {
            var svc = NewService();
            var a = svc.GetAffection(DefaultContent.Lu);
            a.Mood = 10;
            var r = svc.DoHaven(DefaultContent.Lu, HavenAction.Chat);
            Assert.IsTrue(r.ComfortTriggered);
            Assert.AreEqual(HavenRules.AffectionGain(HavenAction.Chat) * 2, r.AffectionGained);
            for (int i = 0; i < 4; i++) svc.DoHaven(DefaultContent.Lu, HavenAction.Chat);
            Assert.AreEqual(HavenResultCode.NoActionPoints, svc.DoHaven(DefaultContent.Lu, HavenAction.Chat).Code);
        }

        [Test]
        public void ChatAnswerRaisesAffectionOnce()
        {
            var svc = NewService();
            var t = svc.NextChat(DefaultContent.Shen);
            Assert.IsNotNull(t);
            Assert.IsNotNull(svc.AnswerChat(t.Id, 1));
            Assert.IsNull(svc.AnswerChat(t.Id, 1));
            var next = svc.NextChat(DefaultContent.Shen);
            Assert.IsTrue(next == null || next.Id != t.Id, "回复过的消息不会再出现");
        }

        [Test]
        public void StageProgressionUnlocksInOrder()
        {
            var svc = NewService();
            Assert.IsTrue(svc.IsStageUnlocked("main_1_1"));
            Assert.IsFalse(svc.IsStageUnlocked("main_1_2"));
            Assert.IsTrue(svc.TryStartStage("main_1_1"));
            var reward = svc.CompleteStage("main_1_1", DefaultContent.Shen);
            Assert.IsTrue(reward.FirstClear);
            Assert.IsTrue(svc.IsStageUnlocked("main_1_2"));
        }

        [Test]
        public void BreakthroughConsumesMaterials()
        {
            var svc = NewService();
            var card = svc.GetCard("shen_sr_01");
            card.AddExp(1000000);
            int mats = svc.State.Wallet.Get(ItemIds.BreakthroughMat);
            Assert.IsTrue(svc.BreakthroughCard("shen_sr_01"));
            Assert.AreEqual(1, card.Breakthrough);
            Assert.Less(svc.State.Wallet.Get(ItemIds.BreakthroughMat), mats);
        }

        [Test]
        public void PartnerLevelUnlocksSlots()
        {
            var svc = NewService();
            svc.State.Wallet.Add(ItemIds.Gold, 10000000);
            svc.State.Wallet.Add(ItemIds.PartnerMat, 1000);
            for (int i = 0; i < 10; i++) Assert.IsTrue(svc.LevelUpPartner(DefaultContent.Shen));
            Assert.AreEqual(4, svc.GetBoard(DefaultContent.Shen).UnlockedSlots);
        }

        [Test]
        public void DailyResetRestoresActionPoints()
        {
            var db = DefaultContent.Create();
            var clock = new FakeClock(new DateTime(2026, 11, 6, 4, 0, 0, DateTimeKind.Utc));
            var svc = new GameService(db, GameService.CreateNewState(db), clock, new SystemRandom(1));
            svc.RefreshDaily();
            svc.DoHaven(DefaultContent.Shen, HavenAction.Chat);
            Assert.AreEqual(4, svc.State.HavenActionPoints);
            Assert.IsFalse(svc.RefreshDaily());
            clock.UtcNow = clock.UtcNow.AddDays(1);
            Assert.IsTrue(svc.RefreshDaily());
            Assert.AreEqual(5, svc.State.HavenActionPoints);
        }
    }
}
