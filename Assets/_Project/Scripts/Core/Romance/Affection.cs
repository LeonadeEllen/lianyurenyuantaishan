using System;
using System.Collections.Generic;

namespace HearTheTide.Core
{
    [Serializable]
    public sealed class AffectionState
    {
        public string CharacterId;
        public int Level = 1;
        public int Exp;
        /// <summary>心情值 0–100，受潮汐与互动影响。</summary>
        public int Mood = 60;
        /// <summary>男主战斗等级（与好感分开）。</summary>
        public int PartnerLevel = 1;
    }

    /// <summary>好感等级解锁的功能，见玩法框架 3.5。</summary>
    public enum RomanceFeature
    {
        BasicChat,
        HavenChat,
        Dating,
        NicknameCall,
        PersonalStoryEarly,
        FlirtTopics,
        HavenSpecial,
        PersonalStoryMid,
        VoicePack,
        HiddenStory,
    }

    public static class AffectionRules
    {
        public const int MaxLevel = 15;

        /// <summary>升到下一级所需好感经验。</summary>
        public static int ExpToNext(int level) { return 50 + level * 30; }

        public static int AddExp(AffectionState state, int amount)
        {
            if (amount <= 0) return 0;
            state.Exp += amount;
            int gained = 0;
            while (state.Level < MaxLevel && state.Exp >= ExpToNext(state.Level))
            {
                state.Exp -= ExpToNext(state.Level);
                state.Level++;
                gained++;
            }
            if (state.Level >= MaxLevel) state.Exp = 0;
            return gained;
        }

        public static int RequiredLevel(RomanceFeature feature)
        {
            switch (feature)
            {
                case RomanceFeature.BasicChat:
                case RomanceFeature.HavenChat: return 1;
                case RomanceFeature.Dating: return 3;
                case RomanceFeature.NicknameCall:
                case RomanceFeature.PersonalStoryEarly: return 4;
                case RomanceFeature.FlirtTopics:
                case RomanceFeature.HavenSpecial:
                case RomanceFeature.PersonalStoryMid: return 7;
                default: return 10;
            }
        }

        public static bool IsUnlocked(AffectionState state, RomanceFeature feature)
        {
            return state.Level >= RequiredLevel(feature);
        }

        public static void AddMood(AffectionState state, int delta)
        {
            state.Mood = Math.Max(0, Math.Min(100, state.Mood + delta));
        }
    }

    // ---------------- 心语（手机消息） ----------------

    [Serializable]
    public sealed class ChatOption
    {
        public string Text;
        public string Reply;
        public int AffectionDelta;
    }

    [Serializable]
    public sealed class ChatThread
    {
        public string Id;
        public string CharacterId;
        public int MinAffectionLevel = 1;
        /// <summary>男主先发来的消息。</summary>
        public List<string> Opening = new List<string>();
        public List<ChatOption> Options = new List<ChatOption>();
    }

    // ---------------- 栖所（3D 陪伴） ----------------

    public enum HavenAction { Chat, Work, Touch, Gift }

    public static class HavenRules
    {
        public const int DailyActionPoints = 5;

        public static int AffectionGain(HavenAction action)
        {
            switch (action)
            {
                case HavenAction.Work: return 15;
                case HavenAction.Gift: return 20;
                case HavenAction.Touch: return 8;
                default: return 10;
            }
        }

        public static int MoodGain(HavenAction action)
        {
            switch (action)
            {
                case HavenAction.Chat: return 8;
                case HavenAction.Work: return 5;
                case HavenAction.Touch: return 6;
                default: return 10;
            }
        }

        /// <summary>心情低于该值时，互动会触发"安慰"事件（双倍好感）。</summary>
        public const int ComfortThreshold = 30;
    }
}
