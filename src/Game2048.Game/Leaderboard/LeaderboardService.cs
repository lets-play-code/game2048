using System;
using System.Collections.Generic;
using System.Linq;

namespace Game2048.Game.Leaderboard;

internal sealed class LeaderboardService
{
    private readonly InMemoryLeaderboardStore store;

    public LeaderboardService(InMemoryLeaderboardStore store)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public void SaveRecord(string playerName, int score, bool gameOver, bool alreadyRecorded, Action<string> publishWallMessage)
    {
        string playerNameToRecord = playerName ?? string.Empty;
        if (playerNameToRecord.Length == 0)
        {
            throw new ArgumentException("Player name is required.", nameof(playerName));
        }
        if (!gameOver)
        {
            throw new InvalidOperationException("Leaderboard records can only be saved after the game is over.");
        }
        if (alreadyRecorded)
        {
            throw new InvalidOperationException("This game's score has already been recorded.");
        }

        store.SaveBestScore(playerNameToRecord, score);
        publishWallMessage(playerNameToRecord + " scored " + score + " points in LEGACY 2048!");
    }

    public List<LeaderboardEntry> GetEntries()
    {
        List<KeyValuePair<string, int>> scores = store.GetSortedScores();
        List<LeaderboardEntry> entries = new List<LeaderboardEntry>(scores.Count);
        int position = 1;
        for (int i = 0; i < scores.Count; i++)
        {
            if (i > 0 && scores[i].Value < scores[i - 1].Value)
            {
                position = i + 1;
            }
            entries.Add(new LeaderboardEntry
            {
                Rank = position,
                PlayerName = scores[i].Key,
                Score = scores[i].Value
            });
        }

        return entries;
    }

    public int GetPositionOfPlayer(string playerName)
    {
        string playerNameToFind = playerName ?? string.Empty;
        List<LeaderboardEntry> entries = GetEntries();
        LeaderboardEntry entry = entries.FirstOrDefault(item => item.PlayerName.Equals(playerNameToFind, StringComparison.Ordinal));
        return entry != null ? entry.Rank : entries.Count + 1;
    }
}
