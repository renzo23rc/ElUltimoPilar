using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Records local terminal match scores without retaining match state.
/// </summary>
public static class MenuScores
{
    private const string ScoresFileName = "menu-scores.json";
    private const int CurrentSchemaVersion = 1;
    private const int MaximumEntries = 10;
    private const int MaximumScore = 100;
    private const int MaximumPlayerCount = 4;
    private const string EmptySummary = "Todavía no hay puntuaciones locales.";
    private const string LocalTimeLabel = " (hora local)";
    private static ScoresData cachedData;
    private static bool loaded;

    [Serializable]
    private sealed class ScoresData
    {
        public int schemaVersion = CurrentSchemaVersion;
        public List<ScoreEntry> entries = new List<ScoreEntry>();
    }

    [Serializable]
    private sealed class ScoreEntry
    {
        public int score;
        public int waveReached;
        public int totalWaves;
        public string utcDate;
        public int playerCount;
    }

    /// <summary>Records one valid terminal match result in the local leaderboard.</summary>
    public static void Record(MatchResult result, int waveReached, int totalWaves, int playerCount)
    {
        if (result == null || totalWaves <= 0 || waveReached < 0 || waveReached > totalWaves ||
            playerCount < 1 || playerCount > MaximumPlayerCount)
        {
            return;
        }

        EnsureLoaded();
        cachedData.entries.Add(new ScoreEntry
        {
            score = result.Score,
            waveReached = waveReached,
            totalWaves = totalWaves,
            utcDate = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            playerCount = playerCount
        });
        cachedData.entries.Sort(CompareEntries);
        if (cachedData.entries.Count > MaximumEntries)
        {
            cachedData.entries.RemoveRange(MaximumEntries, cachedData.entries.Count - MaximumEntries);
        }

        Save();
    }

    /// <summary>Returns a compact summary of locally recorded scores.</summary>
    public static string GetSummary()
    {
        EnsureLoaded();
        if (cachedData.entries.Count == 0)
        {
            return EmptySummary;
        }

        StringBuilder summary = new StringBuilder();
        for (int index = 0; index < cachedData.entries.Count; index++)
        {
            ScoreEntry entry = cachedData.entries[index];
            if (index > 0)
            {
                summary.AppendLine();
            }

            DateTime utcDate = DateTime.ParseExact(
                entry.utcDate,
                "o",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
            string localDate = utcDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
            summary.Append(index + 1)
                .Append(". ")
                .Append(entry.score)
                .Append(" puntos | Oleada ")
                .Append(entry.waveReached)
                .Append('/')
                .Append(entry.totalWaves)
                .Append(" | ")
                .Append(entry.playerCount)
                .Append(entry.playerCount == 1 ? " jugador | " : " jugadores | ")
                .Append(localDate)
                .Append(LocalTimeLabel);
        }

        return summary.ToString();
    }

    private static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        cachedData = new ScoresData();
        try
        {
            string path = GetScoresPath();
            if (!File.Exists(path))
            {
                return;
            }

            ScoresData storedData = JsonUtility.FromJson<ScoresData>(File.ReadAllText(path));
            if (storedData == null || storedData.schemaVersion != CurrentSchemaVersion || storedData.entries == null)
            {
                return;
            }

            foreach (ScoreEntry entry in storedData.entries)
            {
                if (IsValid(entry))
                {
                    cachedData.entries.Add(entry);
                }
            }

            cachedData.entries.Sort(CompareEntries);
            if (cachedData.entries.Count > MaximumEntries)
            {
                cachedData.entries.RemoveRange(MaximumEntries, cachedData.entries.Count - MaximumEntries);
            }
        }
        catch (IOException exception)
        {
            Debug.LogWarning($"[MenuScores] Could not read local scores: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogWarning($"[MenuScores] Local scores are not accessible: {exception.Message}");
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning($"[MenuScores] Local scores are invalid: {exception.Message}");
        }
    }

    private static bool IsValid(ScoreEntry entry)
    {
        return entry != null && entry.score >= 0 && entry.score <= MaximumScore && entry.waveReached >= 0 &&
            entry.totalWaves > 0 && entry.waveReached <= entry.totalWaves &&
            entry.playerCount >= 1 && entry.playerCount <= MaximumPlayerCount &&
            DateTime.TryParseExact(
                entry.utcDate,
                "o",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTime parsedDate) && parsedDate.Kind == DateTimeKind.Utc;
    }

    private static int CompareEntries(ScoreEntry first, ScoreEntry second)
    {
        int scoreOrder = second.score.CompareTo(first.score);
        if (scoreOrder != 0)
        {
            return scoreOrder;
        }

        int waveOrder = second.waveReached.CompareTo(first.waveReached);
        if (waveOrder != 0)
        {
            return waveOrder;
        }

        return string.CompareOrdinal(first.utcDate, second.utcDate);
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(GetScoresPath(), JsonUtility.ToJson(cachedData, true));
        }
        catch (IOException exception)
        {
            Debug.LogWarning($"[MenuScores] Could not save local scores: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogWarning($"[MenuScores] Local scores are not writable: {exception.Message}");
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning($"[MenuScores] Local scores could not be saved: {exception.Message}");
        }
    }

    private static string GetScoresPath()
    {
        return Path.Combine(Application.persistentDataPath, ScoresFileName);
    }
}
