using System;
using System.Collections.Generic;

namespace HearTheTide.Core
{
    public enum CombatRole { Control, Burst, Support, Breaker }

    [Serializable]
    public sealed class CharacterDefinition
    {
        public string Id;
        public string Name;
        public string Title;
        /// <summary>占位色（美术未到位前用于胶囊体和 UI），RGB 十六进制。</summary>
        public string PlaceholderColorHex;
        public CombatRole Role;
        public string AssistSkillName;
        public string ResonanceSkillName;
        /// <summary>喜欢的潮汐：当天心情 +。</summary>
        public TideKind FavoredTide;
        /// <summary>低落的潮汐：当天心情 -，但陪伴可触发特殊剧情。</summary>
        public TideKind TroubledTide;
        public CombatStats BaseStats;
        /// <summary>是否在 MVP 中开放。</summary>
        public bool AvailableInMvp;
    }

    [Serializable]
    public sealed class StageDefinition
    {
        public string Id;
        public string Name;
        public int StaminaCost;
        public int Waves;
        public int EnemiesPerWave;
        public float EnemyHpMultiplier = 1f;
        public float EnemyAttackMultiplier = 1f;
        public bool HasBoss;
        public int RewardGold;
        public int RewardDiamond;
        public int RewardCardExp;
        public int RewardBreakthroughMat;
    }

    [Serializable]
    public sealed class DateSpotDefinition
    {
        public string Id;
        public string Name;
        public List<TideKind> Tides = new List<TideKind>();
    }
}
