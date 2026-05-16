package org.testcharm;

import java.io.File;
import java.io.IOException;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.ArrayList;
import java.util.List;
import java.util.stream.Collectors;
import java.util.concurrent.TimeUnit;

public class Game2048AppRuntime {
    private static final String WEB_APP_DLL_PATH = "../src/Game2048.Web/bin/Debug/net8.0/Game2048.Web.dll";
    private static final String WEB_APP_INSTRUMENTED_DLL_NAME = "Game2048.Web.dll";
    private static final String WEB_APP_CONTENT_ROOT_PATH = "../src/Game2048.Web";

    private final String dotnetCommand;
    private final Path databasePath;
    private final Path baseUrlPath;
    private final Path coverageRecorderDirectory;
    private final Path coverageReportPath;
    private final String forcedGeneratedTileValue;
    private final String leaderboardWallUrl;
    private final Thread shutdownHook;
    private Process process;
    private String baseUrl;

    public Game2048AppRuntime(
            String dotnetCommand,
            String databasePath,
            String coverageRecorderDirectory,
            String coverageReportPath,
            String forcedGeneratedTileValue,
            String leaderboardWallUrl) {
        this.dotnetCommand = dotnetCommand;
        this.databasePath = Path.of(databasePath).toAbsolutePath();
        this.baseUrlPath = buildBaseUrlPath(this.databasePath);
        boolean hasCoverageRecorderDirectory = coverageRecorderDirectory != null && !coverageRecorderDirectory.isBlank();
        boolean hasCoverageReportPath = coverageReportPath != null && !coverageReportPath.isBlank();
        if (hasCoverageRecorderDirectory != hasCoverageReportPath) {
            throw new IllegalArgumentException("Both coverage recorder directory and coverage report path are required together.");
        }
        this.coverageRecorderDirectory = hasCoverageRecorderDirectory ? Path.of(coverageRecorderDirectory).toAbsolutePath() : null;
        this.coverageReportPath = hasCoverageReportPath ? Path.of(coverageReportPath).toAbsolutePath() : null;
        this.forcedGeneratedTileValue = forcedGeneratedTileValue;
        this.leaderboardWallUrl = leaderboardWallUrl;
        this.shutdownHook = new Thread(this::stop, "game2048-e2e-runtime-shutdown");
        Runtime.getRuntime().addShutdownHook(shutdownHook);
    }

    public synchronized void start() {
        if (process != null && process.isAlive()) {
            return;
        }

        try {
            Files.createDirectories(this.databasePath.getParent());
            File logFile = buildLogPath(this.databasePath).toFile();
            if (logFile.exists() && !logFile.delete()) {
                throw new IOException("Failed to reset log file: " + logFile.getAbsolutePath());
            }
            Files.deleteIfExists(baseUrlPath);
            baseUrl = null;

            ProcessBuilder processBuilder = new ProcessBuilder(coverageRecorderDirectory == null
                    ? buildStartCommand(dotnetCommand)
                    : buildCoverageStartCommand(
                    dotnetCommand,
                    coverageRecorderDirectory.toString(),
                    coverageReportPath.toString(),
                    buildWebAppContentRootPath().toString()));
            processBuilder.directory(new File(System.getProperty("user.dir")));
            processBuilder.redirectErrorStream(true);
            processBuilder.redirectOutput(ProcessBuilder.Redirect.appendTo(logFile));
            processBuilder.environment().put("ASPNETCORE_URLS", "http://127.0.0.1:0");
            processBuilder.environment().put("Game2048__DatabasePath", databasePath.toString());
            processBuilder.environment().put("Game2048__PortFilePath", baseUrlPath.toString());
            processBuilder.environment().put("Game2048__EnableTestApi", "true");
            processBuilder.environment().put("Game2048__LeaderboardWallUrl", leaderboardWallUrl);
            processBuilder.environment().put("Logging__LogLevel__Default", "Warning");

            System.out.println("[INFO] Starting Game2048 web app with a dynamic port");
            process = processBuilder.start();
            waitUntilReady(logFile.toPath(), baseUrlPath);
            applyConfiguredGeneratedTileValue();
        } catch (IOException | RuntimeException e) {
            stop();
            throw new IllegalStateException("Failed to start the Game2048 web app.", e);
        }
    }

    public synchronized void stop() {
        if (process != null) {
            try {
                if (!waitForGracefulShutdown()) {
                    List<ProcessHandle> descendants = process.toHandle().descendants().collect(Collectors.toList());
                    destroyProcessHandles(descendants);
                    process.destroy();
                }

                if (!process.waitFor(10, TimeUnit.SECONDS)) {
                    List<ProcessHandle> descendants = process.toHandle().descendants().collect(Collectors.toList());
                    destroyProcessHandlesForcibly(descendants);
                    process.destroyForcibly();
                    process.waitFor(5, TimeUnit.SECONDS);
                }
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                throw new IllegalStateException("Interrupted while stopping the Game2048 web app.", e);
            } finally {
                process = null;
                baseUrl = null;
            }
        }
    }

    public synchronized String getBaseUrl() {
        if (baseUrl == null || baseUrl.isBlank()) {
            throw new IllegalStateException("The Game2048 web app base URL is unavailable.");
        }
        return baseUrl;
    }

    public synchronized Path getDatabasePath() {
        return databasePath;
    }

    public synchronized boolean isRunning() {
        return process != null && process.isAlive();
    }

    public synchronized void applyConfiguredGeneratedTileValue() {
        if (forcedGeneratedTileValue == null || forcedGeneratedTileValue.isBlank()) {
            return;
        }

        forceGeneratedTileValue(forcedGeneratedTileValue);
    }

    public synchronized void forceGeneratedTileValue(String value) {
        if (value == null || value.isBlank()) {
            throw new IllegalArgumentException("Generated tile value is required.");
        }

        postJson("/api/test/generated-tile-value", "{\"value\":\"" + value + "\"}");
    }

    public void clearData() {
        try (Connection connection = DriverManager.getConnection("jdbc:sqlite:" + databasePath);
             Statement statement = connection.createStatement()) {
            statement.executeUpdate("DELETE FROM LeaderboardEntries");
            statement.executeUpdate("DELETE FROM SavedGames");
        } catch (SQLException e) {
            throw new IllegalStateException("Failed to clear Game2048 e2e data.", e);
        }
    }

    static Path buildLogPath(Path databasePath) {
        String databaseFileName = databasePath.getFileName().toString();
        int extensionIndex = databaseFileName.lastIndexOf('.');
        String stem = extensionIndex > 0 ? databaseFileName.substring(0, extensionIndex) : databaseFileName;
        return databasePath.resolveSibling(stem + ".log");
    }

    static Path buildBaseUrlPath(Path databasePath) {
        String databaseFileName = databasePath.getFileName().toString();
        int extensionIndex = databaseFileName.lastIndexOf('.');
        String stem = extensionIndex > 0 ? databaseFileName.substring(0, extensionIndex) : databaseFileName;
        return databasePath.resolveSibling(stem + ".url");
    }

    static List<String> buildStartCommand(String dotnetCommand) {
        List<String> command = new ArrayList<>();
        command.add(dotnetCommand);
        command.add(WEB_APP_DLL_PATH);
        return command;
    }

    static List<String> buildCoverageStartCommand(
            String dotnetCommand,
            String recorderDirectory,
            String coberturaReportPath,
            String contentRootPath) {
        List<String> command = new ArrayList<>();
        command.add(dotnetCommand);
        command.add("tool");
        command.add("run");
        command.add("altcover");
        command.add("--");
        command.add("Runner");
        command.add("--recorderDirectory");
        command.add(recorderDirectory);
        command.add("--workingDirectory");
        command.add(recorderDirectory);
        command.add("--executable");
        command.add(dotnetCommand);
        command.add("--cobertura");
        command.add(coberturaReportPath);
        command.add("--");
        command.add(WEB_APP_INSTRUMENTED_DLL_NAME);
        command.add("--contentRoot");
        command.add(contentRootPath);
        return command;
    }

    private static void destroyProcessHandles(List<ProcessHandle> processHandles) {
        for (ProcessHandle processHandle : processHandles) {
            processHandle.destroy();
        }
    }

    private static void destroyProcessHandlesForcibly(List<ProcessHandle> processHandles) {
        for (ProcessHandle processHandle : processHandles) {
            processHandle.destroyForcibly();
        }
    }

    private boolean waitForGracefulShutdown() {
        if (process == null || !process.isAlive() || baseUrl == null || baseUrl.isBlank()) {
            return false;
        }

        postJson("/api/test/shutdown", "{}");
        try {
            return process.waitFor(10, TimeUnit.SECONDS);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            throw new IllegalStateException("Interrupted while waiting for graceful shutdown.", e);
        }
    }

    private static Path buildWebAppContentRootPath() {
        return Path.of(System.getProperty("user.dir"))
                .resolve(WEB_APP_CONTENT_ROOT_PATH)
                .normalize()
                .toAbsolutePath();
    }

    private void waitUntilReady(Path logPath, Path baseUrlPath) {
        for (int attempt = 0; attempt < 80; attempt++) {
            if (process == null || !process.isAlive()) {
                throw new IllegalStateException("The Game2048 web app exited before it became ready.\n" + readLog(logPath));
            }

            String candidateBaseUrl = readBaseUrl(baseUrlPath);
            if (candidateBaseUrl != null) {
                baseUrl = candidateBaseUrl;
                try {
                    HttpURLConnection connection = (HttpURLConnection) new URL(candidateBaseUrl + "/api/leaderboard").openConnection();
                    connection.setConnectTimeout(500);
                    connection.setReadTimeout(500);
                    connection.setRequestMethod("GET");
                    int responseCode = connection.getResponseCode();
                    if (responseCode >= 200 && responseCode < 500) {
                        return;
                    }
                } catch (IOException ignored) {
                }
            }

            try {
                Thread.sleep(250);
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                throw new IllegalStateException("Interrupted while waiting for the Game2048 web app.", e);
            }
        }

        throw new IllegalStateException("The Game2048 web app did not become ready in time.\n" + readLog(logPath));
    }

    private String readBaseUrl(Path baseUrlPath) {
        try {
            if (!Files.exists(baseUrlPath)) {
                return null;
            }

            String detectedBaseUrl = Files.readString(baseUrlPath).trim();
            return detectedBaseUrl.isBlank() ? null : detectedBaseUrl;
        } catch (IOException e) {
            throw new IllegalStateException("Failed to read the Game2048 web app base URL.", e);
        }
    }

    private String readLog(Path logPath) {
        try {
            if (!Files.exists(logPath)) {
                return "(no process log captured)";
            }
            return new String(Files.readAllBytes(logPath));
        } catch (IOException e) {
            return "(failed to read process log: " + e.getMessage() + ")";
        }
    }

    private void postJson(String path, String requestBody) {
        try {
            HttpURLConnection connection = (HttpURLConnection) new URL(getBaseUrl() + path).openConnection();
            connection.setConnectTimeout(500);
            connection.setReadTimeout(500);
            connection.setRequestMethod("POST");
            connection.setDoOutput(true);
            connection.setRequestProperty("Content-Type", "application/json");
            byte[] bytes = requestBody.getBytes(StandardCharsets.UTF_8);
            connection.setFixedLengthStreamingMode(bytes.length);
            try (OutputStream outputStream = connection.getOutputStream()) {
                outputStream.write(bytes);
            }

            int responseCode = connection.getResponseCode();
            if (responseCode < 200 || responseCode >= 300) {
                throw new IllegalStateException("Request failed with status code " + responseCode + ".");
            }
        } catch (IOException e) {
            throw new IllegalStateException("Failed to call the Game2048 test API.", e);
        }
    }
}
