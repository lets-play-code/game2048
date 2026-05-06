using System.Collections.Generic;
using System.Linq;

namespace Game2048.Game.Leaderboard;

internal sealed class InMemoryLeaderboardStore
{
    private readonly object syncRoot = new object();
    private readonly Dictionary<string, int> scoresByPlayer = new Dictionary<string, int>();

    public void SaveBestScore(string playerName, int score)
    {
        lock (syncRoot)
        {
            if (!scoresByPlayer.TryGetValue(playerName, out int bestScore) || score > bestScore)
            {
                scoresByPlayer[playerName] = score;
            }
        }
    }

    public List<KeyValuePair<string, int>> GetSortedScores()
    {
        lock (syncRoot)
        {
            return scoresByPlayer
                .OrderByDescending(entry => entry.Value)
                .ThenBy(entry => entry.Key, System.StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
