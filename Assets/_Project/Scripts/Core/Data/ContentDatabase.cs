using System;
using System.Collections.Generic;

namespace HearTheTide.Core
{
    /// <summary>静态配置的只读索引。后续可改为从配置表（Excel → JSON）加载。</summary>
    public sealed class ContentDatabase
    {
        public readonly List<CharacterDefinition> Characters = new List<CharacterDefinition>();
        public readonly List<CardDefinition> Cards = new List<CardDefinition>();
        public readonly List<BannerDefinition> Banners = new List<BannerDefinition>();
        public readonly List<StageDefinition> Stages = new List<StageDefinition>();
        public readonly List<ChatThread> ChatThreads = new List<ChatThread>();
        public readonly List<DateSpotDefinition> DateSpots = new List<DateSpotDefinition>();

        private Dictionary<string, CharacterDefinition> _characters;
        private Dictionary<string, CardDefinition> _cards;
        private Dictionary<string, BannerDefinition> _banners;
        private Dictionary<string, StageDefinition> _stages;

        public void BuildIndex()
        {
            _characters = Index(Characters, c => c.Id);
            _cards = Index(Cards, c => c.Id);
            _banners = Index(Banners, b => b.Id);
            _stages = Index(Stages, s => s.Id);
        }

        private static Dictionary<string, T> Index<T>(List<T> list, Func<T, string> key)
        {
            var d = new Dictionary<string, T>();
            foreach (var item in list)
            {
                string k = key(item);
                if (d.ContainsKey(k)) throw new InvalidOperationException("配置 Id 重复: " + k);
                d[k] = item;
            }
            return d;
        }

        public CharacterDefinition Character(string id) { CharacterDefinition v; return id != null && _characters.TryGetValue(id, out v) ? v : null; }
        public CardDefinition Card(string id) { CardDefinition v; return id != null && _cards.TryGetValue(id, out v) ? v : null; }
        public BannerDefinition Banner(string id) { BannerDefinition v; return id != null && _banners.TryGetValue(id, out v) ? v : null; }
        public StageDefinition Stage(string id) { StageDefinition v; return id != null && _stages.TryGetValue(id, out v) ? v : null; }

        public List<CardDefinition> CardsInMemoryGroup(string groupId)
        {
            return Cards.FindAll(c => c.MemoryGroupId == groupId);
        }

        public List<string> MemoryGroupIds()
        {
            var ids = new List<string>();
            foreach (var c in Cards)
                if (!string.IsNullOrEmpty(c.MemoryGroupId) && !ids.Contains(c.MemoryGroupId)) ids.Add(c.MemoryGroupId);
            return ids;
        }
    }
}
