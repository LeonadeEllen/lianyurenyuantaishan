using System;
using System.Collections.Generic;

namespace HearTheTide.Core
{
    public enum Rarity { R, SR, SSR }

    /// <summary>回响卡静态配置。</summary>
    [Serializable]
    public sealed class CardDefinition
    {
        public string Id;
        public string Name;
        public string CharacterId;
        public Rarity Rarity;
        public Emotion Emotion;
        public float BaseAttack;
        public float BaseHp;
        public float BaseDefense;
        /// <summary>主动技能倍率（相对攻击力）。</summary>
        public float SkillMultiplier;
        /// <summary>所属记忆组，空表示不属于任何组。</summary>
        public string MemoryGroupId;
        /// <summary>是否为限定卡。</summary>
        public bool Limited;
        /// <summary>卡面剧情段落数（R=1, SR=2, SSR=3）。</summary>
        public int StoryChapters;
    }

    /// <summary>玩家持有的一张卡。</summary>
    [Serializable]
    public sealed class CardInstance
    {
        public const int LevelsPerBreakthrough = 20;
        public const int MaxBreakthrough = 4;
        public const int MaxStars = 5;

        public string DefinitionId;
        public int Level = 1;
        public int Exp;
        public int Breakthrough;
        public int Stars = 1;
        public bool Awakened;

        public int LevelCap { get { return LevelsPerBreakthrough * (Breakthrough + 1); } }

        /// <summary>升到下一级所需经验。</summary>
        public static int ExpToNext(int level) { return 100 + (level - 1) * 40; }

        /// <summary>加经验，返回提升的等级数。等级被突破上限卡住时多余经验保留。</summary>
        public int AddExp(int amount)
        {
            if (amount < 0) return 0;
            Exp += amount;
            int gained = 0;
            while (Level < LevelCap && Exp >= ExpToNext(Level))
            {
                Exp -= ExpToNext(Level);
                Level++;
                gained++;
            }
            return gained;
        }

        public bool CanBreakthrough { get { return Breakthrough < MaxBreakthrough && Level >= LevelCap; } }

        public bool CanAwaken { get { return !Awakened && Breakthrough >= MaxBreakthrough; } }

        public CombatStats GetStats(CardDefinition def)
        {
            float levelMul = 1f + (Level - 1) * 0.05f;
            float starMul = 1f + (Stars - 1) * 0.08f;
            float awakenMul = Awakened ? 1.1f : 1f;
            float m = levelMul * starMul * awakenMul;
            return new CombatStats(def.BaseAttack * m, def.BaseHp * m, def.BaseDefense * m);
        }
    }

    public static class CardCosts
    {
        /// <summary>第 n 次突破（从 0 开始）需要的突破材料数。</summary>
        public static int BreakthroughMaterial(int currentBreakthrough) { return 5 + currentBreakthrough * 5; }
        public static int BreakthroughGold(int currentBreakthrough) { return 5000 * (currentBreakthrough + 1); }
        public const int AwakenMaterial = 10;
    }
}
