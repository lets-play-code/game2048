package org.testcharm;

import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpServer;

import java.io.File;
import java.io.IOException;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.ServerSocket;
import java.net.URL;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.charset.StandardCharsets;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.concurrent.TimeUnit;

public class Game2048AppRuntime {
    private final String dotnetCommand;
    private final Path databasePath;
    private final String forcedGeneratedTileValue;
    private final Thread shutdownHook;
    private Process process;
    private int port;
    private HttpServer leaderboardWallServer;
    private int leaderboardWallPort;
    private volatile int leaderboardWallStatusCode = 200;

    public Game2048AppRuntime(String dotnetCommand, String databasePath, String forcedGeneratedTileValue) {
        this.dotnetCommand = dotnetCommand;
        this.databasePath = Path.of(databasePath).toAbsolutePath();
        this.forcedGeneratedTileValue = forcedGeneratedTileValue;
        this.shutdownHook = new Thread(this::stop, "game2048-e2e-runtime-shutdown");
        Runtime.getRuntime().addShutdownHook(shutdownHook);
    }

    public synchronized void start() {
        if (process != null && process.isAlive()) {
            return;
        }

        try {
            startLeaderboardWall();
            port = findAvailablePort();
            Files.createDirectories(this.databasePath.getParent());
            File logFile = this.databasePath.resolveSibling("game2048.log").toFile();
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
            processBuilder.environment().put("Game2048__DatabasePath", databasePath.toString());
            processBuilder.environment().put("Game2048__EnableTestApi", "true");
            processBuilder.environment().put("Game2048__LeaderboardWallUrl", getLeaderboardWallUrl());
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

        if (leaderboardWallServer != null) {
            leaderboardWallServer.stop(0);
            leaderboardWallServer = null;
            leaderboardWallPort = 0;
        }
        leaderboardWallStatusCode = 200;
    }

    public synchronized String getBaseUrl() {
        return "http://127.0.0.1:" + port;
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

    public synchronized void configureLeaderboardWallStatusCode(int statusCode) {
        if (statusCode < 100 || statusCode > 599) {
            throw new IllegalArgumentException("Leaderboard wall status code must be between 100 and 599.");
        }
        leaderboardWallStatusCode = statusCode;
    }

    public synchronized int getLeaderboardWallStatusCode() {
        return leaderboardWallStatusCode;
    }

    public synchronized void resetLeaderboardWall() {
        leaderboardWallStatusCode = 200;
    }

    public void clearData() {
        try (Connection connection = DriverManager.getConnection("jdbc:sqlite:" + databasePath);
             Statement statement = connection.createStatement()) {
            statement.executeUpdate("DELETE FROM LeaderboardEntries");
            statement.executeUpdate("DELETE FROM SavedGames");
        } catch (SQLException e) {
            throw new IllegalStateException("Failed to clear Game2048 e2e data.", e);
        }
        resetLeaderboardWall();
    }

    private int findAvailablePort() throws IOException {
        try (ServerSocket socket = new ServerSocket(0)) {
            return socket.getLocalPort();
        }
    }

    private synchronized void startLeaderboardWall() throws IOException {
        if (leaderboardWallServer != null) {
            return;
        }

        leaderboardWallServer = HttpServer.create(new InetSocketAddress(InetAddress.getLoopbackAddress(), 0), 0);
        leaderboardWallServer.createContext("/api/wall", this::handleLeaderboardWallRequest);
        leaderboardWallServer.start();
        leaderboardWallPort = leaderboardWallServer.getAddress().getPort();
        leaderboardWallStatusCode = 200;
    }

    private synchronized String getLeaderboardWallUrl() {
        return "http://127.0.0.1:" + leaderboardWallPort + "/api/wall";
    }

    private void waitUntilReady(Path logPath) {
        for (int attempt = 0; attempt < 80; attempt++) {
            if (process == null || !process.isAlive()) {
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

        throw new IllegalStateException("The Game2048 web app did not become ready in time.\n" + readLog(logPath));
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

    private void handleLeaderboardWallRequest(HttpExchange exchange) throws IOException {
        byte[] responseBody = "ok".getBytes(StandardCharsets.UTF_8);
        try (HttpExchange ignored = exchange) {
            if (!"POST".equalsIgnoreCase(exchange.getRequestMethod())) {
                exchange.sendResponseHeaders(405, -1);
                return;
            }

            while (exchange.getRequestBody().read() != -1) {
            }
            exchange.getResponseHeaders().set("Content-Type", "text/plain; charset=utf-8");
            exchange.sendResponseHeaders(leaderboardWallStatusCode, responseBody.length);
            try (OutputStream outputStream = exchange.getResponseBody()) {
                outputStream.write(responseBody);
            }
        }
    }
}
