using System.Collections.Generic;

namespace HearTheTide.Core
{
    /// <summary>
    /// MVP 用的默认配置（占位文本）。正式开发时由策划配置表导出替换。
    /// 角色设定见《与你听潮_策划案_v0.2》第 3 节。
    /// </summary>
    public static class DefaultContent
    {
        public const string Shen = "shen_yanzhou"; // 沈砚舟
        public const string Lu = "lu_yao";         // 陆曜
        public const string Wen = "wen_xi";        // 闻溪
        public const string Ji = "ji_su";          // 季夙

        public static ContentDatabase Create()
        {
            var db = new ContentDatabase();
            AddCharacters(db);
            AddCards(db);
            AddBanners(db);
            AddStages(db);
            AddChats(db);
            AddDateSpots(db);
            db.BuildIndex();
            return db;
        }

        private static void AddCharacters(ContentDatabase db)
        {
            db.Characters.Add(new CharacterDefinition
            {
                Id = Shen, Name = "沈砚舟", Title = "海事署第七科科长", PlaceholderColorHex = "5B6670",
                Role = CombatRole.Control, AssistSkillName = "锁链束缚", ResonanceSkillName = "缚潮·终章",
                FavoredTide = TideKind.Neap, TroubledTide = TideKind.StaticNight,
                BaseStats = new CombatStats(110, 1100, 70), AvailableInMvp = true,
            });
            db.Characters.Add(new CharacterDefinition
            {
                Id = Lu, Name = "陆曜", Title = "地下赛车手 / 机修师", PlaceholderColorHex = "E2572B",
                Role = CombatRole.Burst, AssistSkillName = "引擎冲刺", ResonanceSkillName = "背靠背·全速",
                FavoredTide = TideKind.Spring, TroubledTide = TideKind.Neap,
                BaseStats = new CombatStats(130, 950, 50), AvailableInMvp = true,
            });
            db.Characters.Add(new CharacterDefinition
            {
                Id = Wen, Name = "闻溪", Title = "浮岛古籍修复师", PlaceholderColorHex = "E8E4D6",
                Role = CombatRole.Support, AssistSkillName = "潮灯守护", ResonanceSkillName = "长明·照海",
                FavoredTide = TideKind.Mid, TroubledTide = TideKind.Spring,
                BaseStats = new CombatStats(95, 1200, 65), AvailableInMvp = false,
            });
            db.Characters.Add(new CharacterDefinition
            {
                Id = Ji, Name = "季夙", Title = "深蓝重工继承人", PlaceholderColorHex = "2456C8",
                Role = CombatRole.Breaker, AssistSkillName = "剑舞破甲", ResonanceSkillName = "双人圆舞",
                FavoredTide = TideKind.StaticNight, TroubledTide = TideKind.Mid,
                BaseStats = new CombatStats(120, 1000, 55), AvailableInMvp = false,
            });
        }

        private static CardDefinition Card(string id, string name, string ch, Rarity r, Emotion e, string group, bool limited = false)
        {
            float mul = r == Rarity.SSR ? 3f : r == Rarity.SR ? 1.8f : 1f;
            return new CardDefinition
            {
                Id = id, Name = name, CharacterId = ch, Rarity = r, Emotion = e,
                BaseAttack = 20 * mul, BaseHp = 200 * mul, BaseDefense = 10 * mul,
                SkillMultiplier = r == Rarity.SSR ? 3.5f : r == Rarity.SR ? 2.5f : 1.8f,
                MemoryGroupId = group, Limited = limited,
                StoryChapters = r == Rarity.SSR ? 3 : r == Rarity.SR ? 2 : 1,
            };
        }

        private static void AddCards(ContentDatabase db)
        {
            // 沈砚舟
            db.Cards.Add(Card("shen_r_01", "加班后的咖啡", Shen, Rarity.R, Emotion.Joy, "shen_mg_01"));
            db.Cards.Add(Card("shen_r_02", "未签收的报告", Shen, Rarity.R, Emotion.Fear, null));
            db.Cards.Add(Card("shen_sr_01", "雨夜的伞", Shen, Rarity.SR, Emotion.Longing, "shen_mg_01"));
            db.Cards.Add(Card("shen_sr_02", "黑手套之下", Shen, Rarity.SR, Emotion.Sorrow, null));
            db.Cards.Add(Card("shen_sr_03", "第七科的灯", Shen, Rarity.SR, Emotion.Anger, null));
            db.Cards.Add(Card("shen_ssr_01", "缚潮之誓", Shen, Rarity.SSR, Emotion.Sorrow, "shen_mg_01"));
            db.Cards.Add(Card("shen_ssr_lim_01", "静潮夜的告白", Shen, Rarity.SSR, Emotion.Longing, null, true));
            // 陆曜
            db.Cards.Add(Card("lu_r_01", "机油味的早餐", Lu, Rarity.R, Emotion.Joy, "lu_mg_01"));
            db.Cards.Add(Card("lu_r_02", "改装车库", Lu, Rarity.R, Emotion.Anger, null));
            db.Cards.Add(Card("lu_sr_01", "副驾驶的位置", Lu, Rarity.SR, Emotion.Joy, null));
            db.Cards.Add(Card("lu_sr_02", "旧伤疤", Lu, Rarity.SR, Emotion.Sorrow, "lu_mg_01"));
            db.Cards.Add(Card("lu_sr_03", "终点线", Lu, Rarity.SR, Emotion.Fear, null));
            db.Cards.Add(Card("lu_ssr_01", "这次换我记住你", Lu, Rarity.SSR, Emotion.Longing, "lu_mg_01"));
            db.Cards.Add(Card("lu_ssr_lim_01", "大潮夜狂飙", Lu, Rarity.SSR, Emotion.Anger, null, true));
            // 闻溪（首发角色，先放少量卡）
            db.Cards.Add(Card("wen_r_01", "修复中的古籍", Wen, Rarity.R, Emotion.Longing, null));
            db.Cards.Add(Card("wen_sr_01", "灯塔上的茶", Wen, Rarity.SR, Emotion.Joy, null));
            db.Cards.Add(Card("wen_ssr_01", "长明", Wen, Rarity.SSR, Emotion.Sorrow, null));
            // 季夙
            db.Cards.Add(Card("ji_r_01", "慈善晚宴", Ji, Rarity.R, Emotion.Fear, null));
            db.Cards.Add(Card("ji_sr_01", "蓝宝石袖扣", Ji, Rarity.SR, Emotion.Anger, null));
            db.Cards.Add(Card("ji_ssr_01", "整座城都可以不要", Ji, Rarity.SSR, Emotion.Longing, null));
        }

        private static void AddBanners(ContentDatabase db)
        {
            var standardSsr = new List<string> { "shen_ssr_01", "lu_ssr_01", "wen_ssr_01", "ji_ssr_01" };
            var sr = new List<string>();
            var r = new List<string>();
            foreach (var c in db.Cards)
            {
                if (c.Limited) continue;
                if (c.Rarity == Rarity.SR) sr.Add(c.Id);
                else if (c.Rarity == Rarity.R) r.Add(c.Id);
            }

            db.Banners.Add(new BannerDefinition
            {
                Id = "standard", Name = "常驻·潮声", Type = BannerType.Standard, PityGroup = "standard",
                SsrPool = standardSsr, SrPool = new List<string>(sr), RPool = new List<string>(r),
            });
            db.Banners.Add(new BannerDefinition
            {
                Id = "limited_shen_01", Name = "限定·静潮夜的告白（沈砚舟）", Type = BannerType.Limited, PityGroup = "limited",
                FeaturedSsrIds = new List<string> { "shen_ssr_lim_01" },
                SsrPool = new List<string>(standardSsr), SrPool = new List<string>(sr), RPool = new List<string>(r),
            });
        }

        private static void AddStages(ContentDatabase db)
        {
            for (int i = 1; i <= 5; i++)
            {
                db.Stages.Add(new StageDefinition
                {
                    Id = "main_1_" + i, Name = "第一章 雾港初遇 1-" + i, StaminaCost = 6,
                    Waves = i == 5 ? 2 : 1 + i / 2, EnemiesPerWave = 2 + i / 2,
                    EnemyHpMultiplier = 1f + 0.15f * (i - 1), EnemyAttackMultiplier = 1f + 0.1f * (i - 1),
                    HasBoss = i == 5, RewardGold = 800 + 200 * i, RewardDiamond = i == 5 ? 100 : 20,
                    RewardCardExp = 3, RewardBreakthroughMat = i == 5 ? 3 : 0,
                });
            }
            db.Stages.Add(new StageDefinition
            {
                Id = "res_exp", Name = "资源副本·记忆回廊", StaminaCost = 10, Waves = 2, EnemiesPerWave = 4,
                RewardGold = 500, RewardCardExp = 12,
            });
            db.Stages.Add(new StageDefinition
            {
                Id = "res_bt", Name = "资源副本·心核矿脉", StaminaCost = 10, Waves = 2, EnemiesPerWave = 3,
                EnemyHpMultiplier = 1.3f, RewardGold = 500, RewardBreakthroughMat = 4,
            });
        }

        private static ChatThread Chat(string id, string ch, int minLv, string[] opening, params ChatOption[] options)
        {
            return new ChatThread { Id = id, CharacterId = ch, MinAffectionLevel = minLv, Opening = new List<string>(opening), Options = new List<ChatOption>(options) };
        }

        private static ChatOption Opt(string text, string reply, int delta)
        {
            return new ChatOption { Text = text, Reply = reply, AffectionDelta = delta };
        }

        private static void AddChats(ContentDatabase db)
        {
            // 以下为占位文案，正式文案由剧情组撰写
            db.ChatThreads.Add(Chat("shen_c01", Shen, 1, new[] { "明早八点，第七科例会。", "别迟到。" },
                Opt("收到，科长。", "嗯。", 2),
                Opt("您也早点休息。", "……知道了。你也是。", 3),
                Opt("八点也太早了吧！", "七点五十，我在楼下等你。", 3)));
            db.ChatThreads.Add(Chat("shen_c02", Shen, 2, new[] { "今晚是大潮。", "外勤结束后直接回家，不要在码头停留。" },
                Opt("你是在担心我吗？", "这是命令。……也是。", 3),
                Opt("好的，我会注意。", "到家发消息。", 2)));
            db.ChatThreads.Add(Chat("lu_c01", Lu, 1, new[] { "喂，小古板。", "你那辆破车我修好了，什么时候来拿？" },
                Opt("现在就去！", "这么急？行，我等你。", 3),
                Opt("谁是小古板？", "谁应就是谁。", 2),
                Opt("修车费多少？", "请我吃夜宵就行。", 3)));
            db.ChatThreads.Add(Chat("lu_c02", Lu, 2, new[] { "今晚旧城有比赛。", "来不来？给你留了副驾驶。" },
                Opt("来！", "抓稳了，我可不减速。", 3),
                Opt("太危险了。", "有我在，你怕什么。", 2)));
            db.ChatThreads.Add(Chat("wen_c01", Wen, 1, new[] { "今天的海雾很温柔。", "像有人在轻声念一首旧诗。" },
                Opt("是哪一首？", "下次见面，念给你听。", 3),
                Opt("你总是这么说话吗？", "只对愿意听的人。", 2)));
            db.ChatThreads.Add(Chat("ji_c01", Ji, 1, new[] { "调律师小姐。", "今晚的慈善晚宴，缺一位女伴。" },
                Opt("我为什么要去？", "因为你想知道深蓝重工在做什么。", 3),
                Opt("不去。", "真遗憾。那我只好去第七科找你了。", 2)));
        }

        private static void AddDateSpots(ContentDatabase db)
        {
            db.DateSpots.Add(new DateSpotDefinition { Id = "night_market", Name = "旧城夜市", Tides = new List<TideKind> { TideKind.Neap, TideKind.Mid } });
            db.DateSpots.Add(new DateSpotDefinition { Id = "pier", Name = "海边栈道", Tides = new List<TideKind> { TideKind.Neap, TideKind.Spring } });
            db.DateSpots.Add(new DateSpotDefinition { Id = "rooftop", Name = "骑楼天台", Tides = new List<TideKind> { TideKind.Mid, TideKind.StaticNight } });
            db.DateSpots.Add(new DateSpotDefinition { Id = "aquarium", Name = "浮岛水族馆", Tides = new List<TideKind> { TideKind.Spring, TideKind.Mid } });
            db.DateSpots.Add(new DateSpotDefinition { Id = "lighthouse", Name = "灯塔下", Tides = new List<TideKind> { TideKind.StaticNight } });
        }
    }
}
