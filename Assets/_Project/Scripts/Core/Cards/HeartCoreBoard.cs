using System;
using System.Collections.Generic;

namespace HearTheTide.Core
{
    /// <summary>六角网格坐标（轴向坐标 q, r）。</summary>
    [Serializable]
    public struct HexCoord : IEquatable<HexCoord>
    {
        public int Q;
        public int R;
        public HexCoord(int q, int r) { Q = q; R = r; }

        public static readonly HexCoord[] Directions =
        {
            new HexCoord(1, 0), new HexCoord(1, -1), new HexCoord(0, -1),
            new HexCoord(-1, 0), new HexCoord(-1, 1), new HexCoord(0, 1),
        };

        public HexCoord Neighbor(int dir) { return new HexCoord(Q + Directions[dir].Q, R + Directions[dir].R); }

        public bool IsAdjacent(HexCoord other)
        {
            int dq = other.Q - Q, dr = other.R - R;
            for (int i = 0; i < 6; i++)
                if (Directions[i].Q == dq && Directions[i].R == dr) return true;
            return false;
        }

        public bool Equals(HexCoord other) { return Q == other.Q && R == other.R; }
        public override bool Equals(object obj) { return obj is HexCoord && Equals((HexCoord)obj); }
        public override int GetHashCode() { return Q * 397 ^ R; }
        public override string ToString() { return "(" + Q + "," + R + ")"; }
    }

    /// <summary>心核盘加成结果。</summary>
    public struct BoardBonus
    {
        /// <summary>最长情绪连线包含的卡数。</summary>
        public int LongestChain;
        public float AttackBonus;          // 0.05 = +5%
        public float TideWindowBonus;      // 秒
        public bool HeartOverflow;         // 心潮满溢：五色齐全
        public float ResonanceDamageBonus; // 共鸣技伤害加成
        public List<KeyValuePair<HexCoord, HexCoord>> Links;
    }

    /// <summary>
    /// 心核盘：每位男主一个，中心格为主卡（决定共鸣技演出），
    /// 初始 3 格，随男主等级扩展到 7 格。见玩法框架 5.3。
    /// </summary>
    [Serializable]
    public sealed class HeartCoreBoard
    {
        public const int MinSlots = 3;
        public const int MaxSlots = 7;

        /// <summary>格子解锁顺序：中心、然后环形。</summary>
        public static readonly HexCoord[] SlotOrder =
        {
            new HexCoord(0, 0),
            new HexCoord(1, 0), new HexCoord(1, -1), new HexCoord(0, -1),
            new HexCoord(-1, 0), new HexCoord(-1, 1), new HexCoord(0, 1),
        };

        public string CharacterId;
        public int UnlockedSlots = MinSlots;
        /// <summary>按 SlotOrder 索引存放卡牌定义 Id，空字符串表示空格。</summary>
        public List<string> SlotCardIds = new List<string>();

        public HeartCoreBoard() { }

        public HeartCoreBoard(string characterId)
        {
            CharacterId = characterId;
            EnsureSize();
        }

        public void EnsureSize()
        {
            while (SlotCardIds.Count < MaxSlots) SlotCardIds.Add(string.Empty);
        }

        /// <summary>男主等级 → 解锁格数：1 级 3 格，每 10 级 +1，最多 7 格。</summary>
        public static int SlotsForPartnerLevel(int partnerLevel)
        {
            return Math.Min(MaxSlots, MinSlots + Math.Max(0, partnerLevel - 1) / 10);
        }

        public string CenterCardId { get { EnsureSize(); return SlotCardIds[0]; } }

        /// <summary>镶嵌。卡牌必须属于该男主；同一张卡只能放一格。</summary>
        public bool TryPlace(int slotIndex, CardDefinition card)
        {
            EnsureSize();
            if (slotIndex < 0 || slotIndex >= UnlockedSlots) return false;
            if (card == null || card.CharacterId != CharacterId) return false;
            for (int i = 0; i < SlotCardIds.Count; i++)
                if (i != slotIndex && SlotCardIds[i] == card.Id) return false;
            SlotCardIds[slotIndex] = card.Id;
            return true;
        }

        public void Clear(int slotIndex)
        {
            EnsureSize();
            if (slotIndex >= 0 && slotIndex < SlotCardIds.Count) SlotCardIds[slotIndex] = string.Empty;
        }

        public IEnumerable<string> PlacedCardIds()
        {
            EnsureSize();
            for (int i = 0; i < UnlockedSlots; i++)
                if (!string.IsNullOrEmpty(SlotCardIds[i])) yield return SlotCardIds[i];
        }

        public BoardBonus Evaluate(Func<string, CardDefinition> lookup)
        {
            EnsureSize();
            var placed = new List<KeyValuePair<HexCoord, Emotion>>();
            var emotions = new HashSet<Emotion>();
            for (int i = 0; i < UnlockedSlots; i++)
            {
                if (string.IsNullOrEmpty(SlotCardIds[i])) continue;
                var def = lookup(SlotCardIds[i]);
                if (def == null) continue;
                placed.Add(new KeyValuePair<HexCoord, Emotion>(SlotOrder[i], def.Emotion));
                emotions.Add(def.Emotion);
            }

            var links = new List<KeyValuePair<HexCoord, HexCoord>>();
            // 并查集求最大连通分量
            var parent = new int[placed.Count];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;
            Func<int, int> find = null;
            find = x => parent[x] == x ? x : (parent[x] = find(parent[x]));

            for (int i = 0; i < placed.Count; i++)
                for (int j = i + 1; j < placed.Count; j++)
                {
                    if (!placed[i].Key.IsAdjacent(placed[j].Key)) continue;
                    if (!EmotionRules.CanLink(placed[i].Value, placed[j].Value)) continue;
                    links.Add(new KeyValuePair<HexCoord, HexCoord>(placed[i].Key, placed[j].Key));
                    parent[find(i)] = find(j);
                }

            var sizes = new Dictionary<int, int>();
            int longest = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                int root = find(i);
                int s;
                sizes.TryGetValue(root, out s);
                sizes[root] = s + 1;
            }
            foreach (var kv in sizes)
                if (kv.Value >= 2 && kv.Value > longest) longest = kv.Value;

            var bonus = new BoardBonus { LongestChain = longest, Links = links };
            if (longest >= 2) bonus.AttackBonus = 0.05f;
            if (longest >= 3) { bonus.AttackBonus = 0.10f; bonus.TideWindowBonus = 0.05f; }
            if (emotions.Count >= EmotionRules.Count)
            {
                bonus.HeartOverflow = true;
                bonus.ResonanceDamageBonus = 0.5f;
            }
            return bonus;
        }
    }
}
