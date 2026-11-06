using System;
using System.IO;
using HearTheTide.Core;
using UnityEngine;

namespace HearTheTide.App
{
    /// <summary>本地存档（JSON）。上线后改为服务器存档，本地仅缓存。</summary>
    public static class SaveStore
    {
        private static string PathOf(string slot) { return Path.Combine(Application.persistentDataPath, slot + ".json"); }

        public static GameState Load(string slot)
        {
            string path = PathOf(slot);
            if (!File.Exists(path)) return null;
            try
            {
                var state = JsonUtility.FromJson<GameState>(File.ReadAllText(path));
                return state;
            }
            catch (Exception e)
            {
                Debug.LogError("[存档] 读取失败，将新建存档: " + e);
                return null;
            }
        }

        public static void Save(string slot, GameState state)
        {
            string path = PathOf(slot);
            string tmp = path + ".tmp";
            try
            {
                File.WriteAllText(tmp, JsonUtility.ToJson(state));
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception e)
            {
                Debug.LogError("[存档] 写入失败: " + e);
            }
        }

        public static void Delete(string slot)
        {
            string path = PathOf(slot);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
