using System.Collections.Generic;
using Game2048Class = Game2048.Game.Game2048;

namespace Game2048.Tests.TestDoubles;

internal sealed class DeterministicGame2048 : Game2048Class
{
    private readonly Queue<double> randomValues = new Queue<double>();

    public List<string> PostedMessages { get; } = new List<string>();

    public void SetRandomValues(params double[] values)
    {
        randomValues.Clear();
        foreach (double value in values)
        {
            randomValues.Enqueue(value);
        }
    }

    protected override double nextRandomDouble()
    {
        return randomValues.Count > 0 ? randomValues.Dequeue() : 0.0;
    }

    protected override void postLeaderboardWallMessage(string message)
    {
        PostedMessages.Add(message);
    }
}
