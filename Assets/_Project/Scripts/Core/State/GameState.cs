using System;
using System.Collections.Generic;

namespace HearTheTide.Core
{
    /// <summary>
    /// 玩家存档。只用 public 字段 + List，以便 Unity JsonUtility 直接序列化。
    /// 正式上线后以服务器数据为准，本地存档仅作缓存。
    /// </summary>
    [Serializable]
    public sealed class GameState
    {
        public const int CurrentVersion = 1;

        public int SaveVersion = CurrentVersion;
        public string PlayerName = "调律师";
        public Wallet Wallet = new Wallet();
        public StaminaState Stamina = new StaminaState();
        public List<CardInstance> Cards = new List<CardInstance>();
        public List<HeartCoreBoard> Boards = new List<HeartCoreBoard>();
        public List<AffectionState> Affections = new List<AffectionState>();
        public List<PityState> Pity = new List<PityState>();
        public List<string> CompletedChats = new List<string>();
        public List<string> ClearedStages = new List<string>();
        public List<string> ClaimedMemoryGroups = new List<string>();
        public int HavenActionPoints = HavenRules.DailyActionPoints;
        /// <summary>上次日常重置的日期（yyyyMMdd，本地游戏日）。</summary>
        public int LastDailyResetDate;
        /// <summary>上次周常重置的周一日期（yyyyMMdd）。</summary>
        public int LastWeeklyResetDate;
        public string SelectedPartnerId;
    }

    /// <summary>出战配置计算结果。</summary>
    public struct Loadout
    {
        public string PartnerId;
        public CombatStats PlayerStats;
        public CombatStats PartnerStats;
        public float TideWindow;
        public float ResonanceDamageMultiplier;
        public float SkillMultiplier;
        public string ResonanceSkillName;
        public BoardBonus Board;
    }

    public enum HavenResultCode { Ok, NoActionPoints, NoGift }

    public struct HavenResult
    {
        public HavenResultCode Code;
        public int AffectionGained;
        public int LevelsGained;
        public bool ComfortTriggered;
    }

    public struct StageReward
    {
        public int Gold;
        public int Diamonds;
        public int CardExpItems;
        public int BreakthroughMats;
        public bool FirstClear;
        public int AffectionGained;
    }
}
