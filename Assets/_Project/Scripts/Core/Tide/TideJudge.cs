using System;

namespace HearTheTide.Core
{
    /// <summary>敌人重攻击前的潮音提示类型。</summary>
    public enum TideCue
    {
        /// <summary>蓝潮：可格挡。</summary>
        Blue,
        /// <summary>白潮：可闪避，不可格挡。</summary>
        White,
        /// <summary>红潮：必须由男主协攻打断。</summary>
        Red,
    }

    public enum TideResponse
    {
        None,
        Parry,
        Dodge,
        Interrupt,
    }

    public enum TideOutcome
    {
        /// <summary>未应对或应对错误，吃满伤害。</summary>
        Hit,
        /// <summary>格挡住但不在判定窗口内：减伤，不触发回潮。</summary>
        Mitigated,
        /// <summary>在判定窗口内正确应对：触发回潮（子弹时间 + 反击 + 共鸣值）。</summary>
        Rebound,
        /// <summary>红潮被打断：攻击取消。</summary>
        Interrupted,
    }

    /// <summary>潮汐节奏判定。纯函数，不依赖引擎，便于测试与服务器校验。</summary>
    public static class TideJudge
    {
        /// <summary>默认判定窗口（秒），见玩法框架 4.3。</summary>
        public const float DefaultWindow = 0.25f;

        /// <param name="cue">潮音类型</param>
        /// <param name="response">玩家的应对</param>
        /// <param name="delta">应对时间减去攻击命中时间（秒）；负数表示提前</param>
        /// <param name="window">判定窗口（秒），心核盘加成会放宽它</param>
        public static TideOutcome Judge(TideCue cue, TideResponse response, float delta, float window)
        {
            bool inWindow = Math.Abs(delta) <= window;
            switch (cue)
            {
                case TideCue.Blue:
                    if (response == TideResponse.Parry)
                    {
                        if (inWindow) return TideOutcome.Rebound;
                        // 提前举盾且一直按住：挡住但不回潮
                        return delta < 0 ? TideOutcome.Mitigated : TideOutcome.Hit;
                    }
                    if (response == TideResponse.Dodge && inWindow) return TideOutcome.Rebound;
                    return TideOutcome.Hit;

                case TideCue.White:
                    if (response == TideResponse.Dodge && inWindow) return TideOutcome.Rebound;
                    return TideOutcome.Hit;

                case TideCue.Red:
                    if (response == TideResponse.Interrupt && delta <= 0f) return TideOutcome.Interrupted;
                    return TideOutcome.Hit;
            }
            return TideOutcome.Hit;
        }

        /// <summary>不同结果对伤害的倍率。</summary>
        public static float DamageMultiplier(TideOutcome outcome)
        {
            switch (outcome)
            {
                case TideOutcome.Mitigated: return 0.3f;
                case TideOutcome.Rebound:
                case TideOutcome.Interrupted: return 0f;
                default: return 1f;
            }
        }
    }
}
