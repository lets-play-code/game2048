package org.testcharm;

import java.io.File;
import java.io.IOException;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URI;
import java.net.ServerSocket;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.concurrent.TimeUnit;

public class Game2048AppRuntime {
    private final String dotnetCommand;
    private final String configuredBaseUrl;
    private final String connectionString;
    private final String jdbcUrl;
    private final String databaseUser;
    private final String databasePassword;
    private final String forcedGeneratedTileValue;
    private final String leaderboardWallUrl;
    private final Thread shutdownHook;
    private Process process;
    private String baseUrl;
    private int port;

    public Game2048AppRuntime(
            String dotnetCommand,
            String configuredBaseUrl,
            String connectionString,
            String jdbcUrl,
            String databaseUser,
            String databasePassword,
            String forcedGeneratedTileValue,
            String leaderboardWallUrl) {
        this.dotnetCommand = dotnetCommand;
        this.configuredBaseUrl = configuredBaseUrl;
        this.connectionString = connectionString;
        this.jdbcUrl = jdbcUrl;
        this.databaseUser = databaseUser;
        this.databasePassword = databasePassword;
        this.forcedGeneratedTileValue = forcedGeneratedTileValue;
        this.leaderboardWallUrl = leaderboardWallUrl;
        this.shutdownHook = new Thread(this::stop, "game2048-e2e-runtime-shutdown");
        Runtime.getRuntime().addShutdownHook(shutdownHook);
    }

    public synchronized void start() {
        if (isExternallyManaged()) {
            if (baseUrl == null) {
                baseUrl = configuredBaseUrl;
            }
            waitUntilReady(null);
            applyConfiguredGeneratedTileValue();
            return;
        }

        if (process != null && process.isAlive()) {
            return;
        }

        try {
            port = findAvailablePort();
            baseUrl = "http://127.0.0.1:" + port;
            Path logDirectory = Path.of(System.getProperty("user.dir"), "build");
            Files.createDirectories(logDirectory);
            File logFile = logDirectory.resolve("game2048-e2e.log").toFile();
            if (logFile.exists() && !logFile.delete()) {
                throw new IOException("Failed to reset log file: " + logFile.getAbsolutePath());
            }

            ProcessBuilder processBuilder = new ProcessBuilder(
                    dotnetCommand,
                    "run",
                    "--no-build",
                    "--no-launch-profile",
                    "--project",
                    "../src/Game2048.Web/Game2048.Web.csproj");
            processBuilder.directory(new File(System.getProperty("user.dir")));
            processBuilder.redirectErrorStream(true);
            processBuilder.redirectOutput(ProcessBuilder.Redirect.appendTo(logFile));
            processBuilder.environment().put("ASPNETCORE_URLS", getBaseUrl());
            processBuilder.environment().put("Game2048__ConnectionString", connectionString);
            processBuilder.environment().put("Game2048__EnableTestApi", "true");
            processBuilder.environment().put("Game2048__LeaderboardWallUrl", leaderboardWallUrl);
            processBuilder.environment().put("Logging__LogLevel__Default", "Warning");

            System.out.println("[INFO] Starting Game2048 web app on " + getBaseUrl());
            process = processBuilder.start();
            waitUntilReady(logFile.toPath());
            applyConfiguredGeneratedTileValue();
        } catch (IOException e) {
            stop();
            throw new IllegalStateException("Failed to start the Game2048 web app.", e);
        }
    }

    public synchronized void stop() {
        if (isExternallyManaged()) {
            return;
        }

        if (process != null) {
            process.destroy();
            try {
                if (!process.waitFor(5, TimeUnit.SECONDS)) {
                    process.destroyForcibly();
                    process.waitFor(5, TimeUnit.SECONDS);
                }
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                throw new IllegalStateException("Interrupted while stopping the Game2048 web app.", e);
            } finally {
                process = null;
            }
        }
    }

    public synchronized String getBaseUrl() {
        if (baseUrl == null) {
            baseUrl = "http://127.0.0.1:" + port;
        }
        return baseUrl;
    }

    public synchronized boolean isRunning() {
        if (isExternallyManaged()) {
            return baseUrl != null;
        }
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
        try (Connection connection = DriverManager.getConnection(jdbcUrl, databaseUser, databasePassword);
             Statement statement = connection.createStatement()) {
            statement.executeUpdate("DELETE FROM LeaderboardEntries");
            statement.executeUpdate("DELETE FROM SavedGames");
        } catch (SQLException e) {
            throw new IllegalStateException("Failed to clear Game2048 e2e data.", e);
        }
    }

    private int findAvailablePort() throws IOException {
        try (ServerSocket socket = new ServerSocket(0)) {
            return socket.getLocalPort();
        }
    }

    private void waitUntilReady(Path logPath) {
        for (int attempt = 0; attempt < 80; attempt++) {
            if (!isExternallyManaged() && (process == null || !process.isAlive())) {
                throw new IllegalStateException("The Game2048 web app exited before it became ready.\n" + readLog(logPath));
            }

            try {
                HttpURLConnection connection = (HttpURLConnection) new URL(getBaseUrl() + "/api/leaderboard").openConnection();
                connection.setConnectTimeout(500);
                connection.setReadTimeout(500);
                connection.setRequestMethod("GET");
                int responseCode = connection.getResponseCode();
                if (responseCode >= 200 && responseCode < 500) {
                    return;
                }
            } catch (IOException ignored) {
            }

            try {
                Thread.sleep(250);
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                throw new IllegalStateException("Interrupted while waiting for the Game2048 web app.", e);
            }
        }

        if (isExternallyManaged()) {
            throw new IllegalStateException("The Game2048 web app at " + getBaseUrl() + " did not become ready in time.");
        }

        throw new IllegalStateException("The Game2048 web app did not become ready in time.\n" + readLog(logPath));
    }

    private String readLog(Path logPath) {
        if (logPath == null) {
            return "(no process log captured)";
        }
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

    private boolean isExternallyManaged() {
        return configuredBaseUrl != null && !configuredBaseUrl.isBlank();
    }
}
