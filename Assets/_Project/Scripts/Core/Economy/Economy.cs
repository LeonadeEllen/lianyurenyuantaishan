using System;
using System.Collections.Generic;

namespace HearTheTide.Core
{
    public static class ItemIds
    {
        public const string Gold = "gold";
        public const string DiamondFree = "diamond_free";
        public const string DiamondPaid = "diamond_paid";
        public const string EchoCoin = "echo_coin";
        public const string DateTicket = "date_ticket";
        public const string CardExp = "card_exp";             // 卡牌经验道具（每个 = 100 经验）
        public const string BreakthroughMat = "bt_mat";       // 突破材料
        public const string AwakenMat = "awaken_mat";         // 觉醒材料
        public const string PartnerMat = "partner_mat";       // 男主专属养成材料
        public const string Gift = "gift";                    // 通用礼物
    }

    [Serializable]
    public sealed class ItemStack
    {
        public string Id;
        public int Count;
    }

    /// <summary>背包 / 钱包。用 List 存储以兼容 Unity JsonUtility。</summary>
    [Serializable]
    public sealed class Wallet
    {
        public List<ItemStack> Items = new List<ItemStack>();

        public int Get(string id)
        {
            for (int i = 0; i < Items.Count; i++)
                if (Items[i].Id == id) return Items[i].Count;
            return 0;
        }

        public void Add(string id, int amount)
        {
            if (amount == 0) return;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Id != id) continue;
                Items[i].Count = Math.Max(0, Items[i].Count + amount);
                return;
            }
            if (amount > 0) Items.Add(new ItemStack { Id = id, Count = amount });
        }

        public bool TrySpend(string id, int amount)
        {
            if (amount < 0 || Get(id) < amount) return false;
            Add(id, -amount);
            return true;
        }

        public int Diamonds { get { return Get(ItemIds.DiamondFree) + Get(ItemIds.DiamondPaid); } }

        /// <summary>扣钻石：优先扣免费钻（实际规则按合规要求和发行策略确定）。</summary>
        public bool TrySpendDiamonds(int amount)
        {
            if (Diamonds < amount) return false;
            int free = Math.Min(Get(ItemIds.DiamondFree), amount);
            Add(ItemIds.DiamondFree, -free);
            Add(ItemIds.DiamondPaid, -(amount - free));
            return true;
        }
    }

    /// <summary>体力：每 6 分钟恢复 1 点，自然恢复上限 120（可通过道具超上限）。</summary>
    [Serializable]
    public sealed class StaminaState
    {
        public const int Max = 120;
        public static readonly TimeSpan RegenInterval = TimeSpan.FromMinutes(6);

        public int Current = Max;
        public long LastRegenUtcTicks;

        public void Refresh(DateTime utcNow)
        {
            if (LastRegenUtcTicks == 0) { LastRegenUtcTicks = utcNow.Ticks; return; }
            if (Current >= Max) { LastRegenUtcTicks = utcNow.Ticks; return; }
            long elapsed = utcNow.Ticks - LastRegenUtcTicks;
            if (elapsed <= 0) return;
            long points = elapsed / RegenInterval.Ticks;
            if (points <= 0) return;
            long newValue = Current + points;
            if (newValue >= Max)
            {
                Current = Max;
                LastRegenUtcTicks = utcNow.Ticks;
            }
            else
            {
                Current = (int)newValue;
                LastRegenUtcTicks += points * RegenInterval.Ticks;
            }
        }

        public bool TrySpend(int amount, DateTime utcNow)
        {
            Refresh(utcNow);
            if (Current < amount) return false;
            bool wasFull = Current >= Max;
            Current -= amount;
            if (wasFull) LastRegenUtcTicks = utcNow.Ticks;
            return true;
        }
    }
}
