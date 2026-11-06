using System;

namespace HearTheTide.Core
{
    /// <summary>共鸣值：回潮、命中积累，满值释放双人共鸣技。</summary>
    [Serializable]
    public sealed class ResonanceGauge
    {
        public const float Max = 100f;
        public const float ReboundGain = 20f;
        public const float HitGain = 2f;
        public const float SkillGain = 5f;

        public float Value;
        /// <summary>获取倍率（卡牌被动等可提高）。</summary>
        public float GainMultiplier = 1f;

        public bool IsFull { get { return Value >= Max; } }
        public float Ratio { get { return Value / Max; } }

        public void Add(float amount)
        {
            if (amount <= 0f) return;
            Value = Math.Min(Max, Value + amount * GainMultiplier);
        }

        /// <summary>满值时消耗并返回 true。</summary>
        public bool TryConsume()
        {
            if (!IsFull) return false;
            Value = 0f;
            return true;
        }
    }
}
