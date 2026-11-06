using System;
using System.Collections.Generic;

namespace HearTheTide.Core
{
    public enum BannerType { Standard, Limited }

    [Serializable]
    public sealed class BannerDefinition
    {
        public string Id;
        public string Name;
        public BannerType Type;
        /// <summary>保底计数分组：同组卡池共享保底（例如所有限定池共用一组）。</summary>
        public string PityGroup;
        public double SsrRate = 0.015;
        public double SrRate = 0.10;
        public int HardPity = 70;
        /// <summary>每 N 抽必出 SR 及以上。</summary>
        public int SrPity = 10;
        public List<string> FeaturedSsrIds = new List<string>();
        public List<string> SsrPool = new List<string>();
        public List<string> SrPool = new List<string>();
        public List<string> RPool = new List<string>();
        /// <summary>单抽价格（钻石）。</summary>
        public int CostPerPull = 150;
    }

    [Serializable]
    public sealed class PityState
    {
        public string Group;
        public int PullsSinceSsr;
        public int PullsSinceSrOrBetter;
        /// <summary>大保底：上一次 SSR 未中限定，下次 SSR 必为限定。</summary>
        public bool FeaturedGuaranteed;
        public int TotalPulls;
    }

    public struct PullResult
    {
        public string CardId;
        public Rarity Rarity;
        public bool IsFeatured;
        public bool IsNew;
        /// <summary>重复卡转化的星级提升（已满星则转化为回响币）。</summary>
        public bool StarUp;
        public int EchoCoins;
    }

    /// <summary>
    /// 抽卡逻辑。见玩法框架 5.6。
    /// 注意：正式版必须在服务器执行，客户端仅做展示，避免篡改。
    /// </summary>
    public static class GachaLogic
    {
        public const int EchoCoinR = 1;
        public const int EchoCoinSR = 5;
        public const int EchoCoinSSR = 20;
        public const int EchoCoinMaxStarDuplicate = 50;

        public static PullResult PullOne(BannerDefinition banner, PityState pity, IRandom rng)
        {
            pity.PullsSinceSsr++;
            pity.PullsSinceSrOrBetter++;
            pity.TotalPulls++;

            var result = new PullResult();
            bool ssr = pity.PullsSinceSsr >= banner.HardPity || rng.NextDouble() < banner.SsrRate;
            if (ssr)
            {
                result.Rarity = Rarity.SSR;
                pity.PullsSinceSsr = 0;
                pity.PullsSinceSrOrBetter = 0;
                if (banner.Type == BannerType.Limited && banner.FeaturedSsrIds.Count > 0)
                {
                    bool featured = pity.FeaturedGuaranteed || rng.NextDouble() < 0.5 || banner.SsrPool.Count == 0;
                    if (featured)
                    {
                        result.CardId = banner.FeaturedSsrIds[rng.Next(banner.FeaturedSsrIds.Count)];
                        result.IsFeatured = true;
                        pity.FeaturedGuaranteed = false;
                    }
                    else
                    {
                        result.CardId = banner.SsrPool[rng.Next(banner.SsrPool.Count)];
                        pity.FeaturedGuaranteed = true;
                    }
                }
                else
                {
                    result.CardId = banner.SsrPool[rng.Next(banner.SsrPool.Count)];
                }
                return result;
            }

            bool sr = pity.PullsSinceSrOrBetter >= banner.SrPity || rng.NextDouble() < banner.SrRate;
            if (sr && banner.SrPool.Count > 0)
            {
                result.Rarity = Rarity.SR;
                result.CardId = banner.SrPool[rng.Next(banner.SrPool.Count)];
                pity.PullsSinceSrOrBetter = 0;
                return result;
            }

            result.Rarity = Rarity.R;
            result.CardId = banner.RPool[rng.Next(banner.RPool.Count)];
            return result;
        }

        public static int BaseEchoCoins(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SSR: return EchoCoinSSR;
                case Rarity.SR: return EchoCoinSR;
                default: return EchoCoinR;
            }
        }
    }
}
