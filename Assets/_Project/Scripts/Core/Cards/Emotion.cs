namespace HearTheTide.Core
{
    /// <summary>卡牌情绪属性：喜 / 哀 / 怒 / 惧 / 念。</summary>
    public enum Emotion
    {
        Joy,      // 喜
        Sorrow,   // 哀
        Anger,    // 怒
        Fear,     // 惧
        Longing,  // 念
    }

    public static class EmotionRules
    {
        public const int Count = 5;

        /// <summary>相生链：喜→念→哀→惧→怒→喜。</summary>
        public static Emotion Generates(Emotion e)
        {
            switch (e)
            {
                case Emotion.Joy: return Emotion.Longing;
                case Emotion.Longing: return Emotion.Sorrow;
                case Emotion.Sorrow: return Emotion.Fear;
                case Emotion.Fear: return Emotion.Anger;
                default: return Emotion.Joy; // Anger → Joy
            }
        }

        /// <summary>两种情绪是否能在心核盘上连线（相同或相生）。</summary>
        public static bool CanLink(Emotion a, Emotion b)
        {
            return a == b || Generates(a) == b || Generates(b) == a;
        }

        public static string DisplayName(Emotion e)
        {
            switch (e)
            {
                case Emotion.Joy: return "喜";
                case Emotion.Sorrow: return "哀";
                case Emotion.Anger: return "怒";
                case Emotion.Fear: return "惧";
                default: return "念";
            }
        }
    }
}
