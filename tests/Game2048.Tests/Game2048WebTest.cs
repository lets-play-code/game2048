using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Game2048.Tests;

public class Game2048WebTest
{

    [Fact]
    public async Task get_saves_returns_auto_and_three_manual_slots()
    {
        using Game2048PersistenceScope scope = new Game2048PersistenceScope();
        await using Game2048WebApplicationFactory factory = scope.CreateFactory();
        using HttpClient client = factory.CreateClient();

        List<SaveSummaryResponse> saves = await client.GetFromJsonAsync<List<SaveSummaryResponse>>("/api/saves");

        Assert.NotNull(saves);
        Assert.Collection(saves,
            auto => AssertEmptySlot(auto, "auto"),
            slot1 => AssertEmptySlot(slot1, "slot1"),
            slot2 => AssertEmptySlot(slot2, "slot2"),
            slot3 => AssertEmptySlot(slot3, "slot3"));
    }

    [Fact]
    public async Task configured_port_file_receives_dynamic_base_url_for_process_startup()
    {
        using Game2048PersistenceScope scope = new Game2048PersistenceScope();
        string rootDirectory = Path.GetDirectoryName(scope.DatabasePath)!;
        string baseUrlPath = Path.Combine(rootDirectory, "listening-url.txt");
        StringBuilder processOutput = new StringBuilder();
        using Process process = StartWebProcess(scope.DatabasePath, baseUrlPath, processOutput);

        try
        {
            string baseUrl = await WaitForBaseUrlAsync(baseUrlPath, process, processOutput);
            Uri uri = new Uri(baseUrl);

            Assert.Equal("127.0.0.1", uri.Host);
            Assert.NotEqual(0, uri.Port);

            using HttpClient client = new HttpClient { BaseAddress = uri };
            using HttpResponseMessage response = await client.GetAsync("/api/saves");
            response.EnsureSuccessStatusCode();
        }
        finally
        {
            StopProcess(process);
        }
    }

    [Fact]
    public async Task test_shutdown_api_stops_process_gracefully()
    {
        using Game2048PersistenceScope scope = new Game2048PersistenceScope();
        string rootDirectory = Path.GetDirectoryName(scope.DatabasePath)!;
        string baseUrlPath = Path.Combine(rootDirectory, "listening-url.txt");
        StringBuilder processOutput = new StringBuilder();
        using Process process = StartWebProcess(scope.DatabasePath, baseUrlPath, processOutput, enableTestApi: true);

        string baseUrl = await WaitForBaseUrlAsync(baseUrlPath, process, processOutput);
        using HttpClient client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        using HttpResponseMessage response = await client.PostAsync("/api/test/shutdown", content: null);

        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(process.WaitForExit(5000), processOutput.ToString());
    }

    private static void AssertEmptySlot(SaveSummaryResponse slot, string slotKey)
    {
        Assert.Equal(slotKey, slot.SlotKey);
        Assert.False(slot.HasData);
        Assert.Null(slot.Score);
        Assert.Null(slot.SavedAtUtc);
    }

    private static Process StartWebProcess(string databasePath, string baseUrlPath, StringBuilder processOutput, bool enableTestApi = false)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = GetRepositoryRoot()
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add("src/Game2048.Web/Game2048.Web.csproj");
        startInfo.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:0";
        startInfo.Environment["Game2048__DatabasePath"] = databasePath;
        startInfo.Environment["Game2048__PortFilePath"] = baseUrlPath;
        startInfo.Environment["Game2048__EnableTestApi"] = enableTestApi ? "true" : "false";
        startInfo.Environment["Logging__LogLevel__Default"] = "Warning";

        Process process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data != null)
            {
                lock (processOutput)
                {
                    processOutput.AppendLine(args.Data);
                }
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data != null)
            {
                lock (processOutput)
                {
                    processOutput.AppendLine(args.Data);
                }
            }
        };

        Assert.True(process.Start());
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static async Task<string> WaitForBaseUrlAsync(string baseUrlPath, Process process, StringBuilder processOutput)
    {
        for (int attempt = 0; attempt < 80; attempt++)
        {
            if (File.Exists(baseUrlPath))
            {
                string baseUrl = (await File.ReadAllTextAsync(baseUrlPath)).Trim();
                if (!string.IsNullOrWhiteSpace(baseUrl))
                {
                    return baseUrl;
                }
            }

            if (process.HasExited)
            {
                throw new InvalidOperationException($"The Game2048 web app exited early.\n{processOutput}");
            }

            await Task.Delay(250);
        }

        throw new InvalidOperationException($"The Game2048 web app did not write its base URL.\n{processOutput}");
    }

    private static void StopProcess(Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        process.Kill(entireProcessTree: true);
        process.WaitForExit(5000);
    }

    private static string GetRepositoryRoot()
    {
        DirectoryInfo current = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(current.FullName, "Game2048.sln")))
        {
            current = current.Parent ?? throw new DirectoryNotFoundException("Failed to locate the repository root.");
        }

        return current.FullName;
    }


    public class SaveSummaryResponse
    {
        public string SlotKey { get; set; } = string.Empty;
        public bool HasData { get; set; }
        public int? Score { get; set; }
        public DateTime? SavedAtUtc { get; set; }
    }
}
