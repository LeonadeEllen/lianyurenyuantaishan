using System;

namespace HearTheTide.Core
{
    /// <summary>随机数抽象，便于测试与服务器校验。</summary>
    public interface IRandom
    {
        /// <summary>返回 [0, 1) 的浮点数。</summary>
        double NextDouble();
        /// <summary>返回 [0, maxExclusive) 的整数。</summary>
        int Next(int maxExclusive);
    }

    public sealed class SystemRandom : IRandom
    {
        private readonly Random _random;
        public SystemRandom() { _random = new Random(); }
        public SystemRandom(int seed) { _random = new Random(seed); }
        public double NextDouble() { return _random.NextDouble(); }
        public int Next(int maxExclusive) { return _random.Next(maxExclusive); }
    }

    /// <summary>时钟抽象。正式上线时应改为服务器时间。</summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class UtcClock : IClock
    {
        public DateTime UtcNow { get { return DateTime.UtcNow; } }
    }

    public static class StableHash
    {
        /// <summary>跨平台稳定的整数哈希（不能用 string.GetHashCode，它每次进程启动都不同）。</summary>
        public static uint Fnv1a(string text)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619;
                }
                return hash;
            }
        }
    }
}
