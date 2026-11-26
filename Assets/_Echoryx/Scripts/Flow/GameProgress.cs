using System;
using System.Collections.Generic;
using UnityEngine;

namespace Echoryx.Flow
{
    public interface IProgressStorage
    {
        string Read();
        void Write(string json);
    }

    public sealed class PlayerPrefsProgressStorage : IProgressStorage
    {
        public const string Key = "Echoryx.Progress";
        public string Read() => PlayerPrefs.GetString(Key, string.Empty);

        public void Write(string json)
        {
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Volatile storage, so tests never touch the player's saved progress.</summary>
    public sealed class MemoryProgressStorage : IProgressStorage
    {
        public string Json = string.Empty;
        public string Read() => Json;
        public void Write(string json) => Json = json;
    }

    /// <summary>A completed level. Each best value is kept independently across runs.</summary>
    [Serializable]
    public sealed class LevelRecord
    {
        public string id;
        public int bestAttempts;
        public int bestRewinds;
        public double bestSeconds;
    }

    /// <summary>Which levels were completed and their best statistics, saved as one JSON string.</summary>
    public sealed class GameProgress
    {
        [Serializable]
        private sealed class Data
        {
            public int version = 1;
            public List<LevelRecord> levels = new List<LevelRecord>();
        }

        private static IProgressStorage storage;

        /// <summary>Where the game saves. Set to null to return to PlayerPrefs.</summary>
        public static IProgressStorage Storage
        {
            get => storage ?? (storage = new PlayerPrefsProgressStorage());
            set => storage = value;
        }

        private readonly IProgressStorage target;
        private readonly Data data;

        public GameProgress(IProgressStorage target)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            data = Parse(target.Read());
        }

        public static GameProgress Load() => new GameProgress(Storage);

        public bool IsCompleted(string levelId) => Find(levelId) != null;

        /// <summary>The saved record, or null if the level was never completed.</summary>
        public LevelRecord Find(string levelId) => data.levels.Find(record => record != null && record.id == levelId);

        public void RecordCompletion(string levelId, int attempts, int rewinds, double seconds)
        {
            if (string.IsNullOrWhiteSpace(levelId)) throw new ArgumentException("A level id is required.", nameof(levelId));
            var record = Find(levelId);
            if (record == null)
                data.levels.Add(new LevelRecord { id = levelId, bestAttempts = attempts, bestRewinds = rewinds, bestSeconds = seconds });
            else
            {
                record.bestAttempts = Math.Min(record.bestAttempts, attempts);
                record.bestRewinds = Math.Min(record.bestRewinds, rewinds);
                record.bestSeconds = Math.Min(record.bestSeconds, seconds);
            }
            target.Write(JsonUtility.ToJson(data));
        }

        /// <summary>The first level is always open; completing a level opens the next one.</summary>
        public bool IsUnlocked(LevelCatalog catalog, int index)
        {
            if (catalog == null || index < 0 || index >= catalog.Count) return false;
            // A completed level stays open even if new levels are later inserted before it.
            return index == 0 || IsCompleted(catalog.Levels[index].Id) || IsCompleted(catalog.Levels[index - 1].Id);
        }

        /// <summary>What "Play" starts: the first open level still pending, or the first level when none is.</summary>
        public int ContinueIndex(LevelCatalog catalog)
        {
            for (var i = 0; i < catalog.Count; i++)
                if (IsUnlocked(catalog, i) && !IsCompleted(catalog.Levels[i].Id)) return i;
            return 0;
        }

        private static Data Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new Data();
            try
            {
                var parsed = JsonUtility.FromJson<Data>(json);
                if (parsed == null) return new Data();
                if (parsed.levels == null) parsed.levels = new List<LevelRecord>();
                return parsed;
            }
            catch (ArgumentException)
            {
                Debug.LogWarning("Saved progress is unreadable; starting with empty progress.");
                return new Data();
            }
        }
    }
}
