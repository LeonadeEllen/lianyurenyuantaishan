using System;

namespace HearTheTide.Core
{
    /// <summary>每日潮汐，见玩法框架 3.3。</summary>
    public enum TideKind
    {
        Neap,        // 小潮
        Mid,         // 中潮
        Spring,      // 大潮
        StaticNight, // 静潮夜（每周一次）
    }

    public static class TideCalendar
    {
        /// <summary>
        /// 由日期确定当天潮汐。纯确定性：同一天所有玩家看到的一样，可提前公布 7 天。
        /// 以本地游戏日期（已按服务器时区换算）为准。
        /// </summary>
        public static TideKind GetTide(DateTime date)
        {
            date = date.Date;
            if (IsStaticNight(date)) return TideKind.StaticNight;
            uint h = StableHash.Fnv1a("tide:" + date.ToString("yyyyMMdd"));
            switch (h % 3)
            {
                case 0: return TideKind.Neap;
                case 1: return TideKind.Mid;
                default: return TideKind.Spring;
            }
        }

        /// <summary>每周（周一开始）随机一天为静潮夜。</summary>
        public static bool IsStaticNight(DateTime date)
        {
            DateTime weekStart = WeekStart(date);
            uint h = StableHash.Fnv1a("static:" + weekStart.ToString("yyyyMMdd"));
            int day = (int)(h % 7);
            return (date.Date - weekStart).Days == day;
        }

        public static DateTime WeekStart(DateTime date)
        {
            int offset = ((int)date.DayOfWeek + 6) % 7; // 周一 = 0
            return date.Date.AddDays(-offset);
        }

        public static string DisplayName(TideKind tide)
        {
            switch (tide)
            {
                case TideKind.Neap: return "小潮";
                case TideKind.Mid: return "中潮";
                case TideKind.Spring: return "大潮";
                default: return "静潮夜";
            }
        }
    }
}
