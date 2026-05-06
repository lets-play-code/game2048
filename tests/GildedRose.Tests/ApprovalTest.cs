using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace GildedRose.Tests;

public class ApprovalTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ApprovalTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void thirty_days()
    {
        string output = TexttestFixture.RenderInventoryReport();
        string approvalPath = Path.Combine(AppContext.BaseDirectory, "ApprovalFiles", "ThirtyDays.approved.txt");

        if (File.Exists(approvalPath))
        {
            Assert.Equal(Normalize(File.ReadAllText(approvalPath)), Normalize(output));
        }
        else
        {
            _testOutputHelper.WriteLine("Expect result: \n" + output);
        }
    }

    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n");
    }
}
