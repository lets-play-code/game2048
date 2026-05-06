using System.IO;
using Xunit;

namespace Game2048.Tests;

public class ApprovalTest
{
    [Fact]
    public void full_journey()
    {
        AssertApproved(Game2048ApprovalFixture.RenderFullJourney(), "FullJourney.approved.txt");
    }

    [Fact]
    public void win_journey()
    {
        AssertApproved(Game2048ApprovalFixture.RenderWinJourney(), "WinJourney.approved.txt");
    }

    [Fact]
    public void leaderboard_journey()
    {
        AssertApproved(Game2048ApprovalFixture.RenderLeaderboardJourney(), "LeaderboardJourney.approved.txt");
    }

    private static void AssertApproved(string actual, string fileName)
    {
        string approvalDirectory = Path.Combine(AppContext.BaseDirectory, "ApprovalFiles");
        Directory.CreateDirectory(approvalDirectory);

        string normalizedActual = Normalize(actual);
        string approvalPath = Path.Combine(approvalDirectory, fileName);
        string receivedPath = Path.Combine(
            approvalDirectory,
            Path.GetFileNameWithoutExtension(fileName).Replace(".approved", string.Empty) + ".received.txt");

        File.WriteAllText(receivedPath, normalizedActual);
        Assert.True(File.Exists(approvalPath), $"Missing approval file: {approvalPath}\nReceived output: {receivedPath}");
        Assert.Equal(Normalize(File.ReadAllText(approvalPath)), normalizedActual);
    }

    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n").TrimEnd('\n');
    }
}
