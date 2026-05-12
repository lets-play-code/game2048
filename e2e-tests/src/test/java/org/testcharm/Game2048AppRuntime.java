package org.testcharm;

import java.io.File;
import java.io.IOException;
import java.net.HttpURLConnection;
import java.net.ServerSocket;
import java.net.URL;
import java.nio.file.Files;
import java.nio.file.Path;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.concurrent.TimeUnit;

public class Game2048AppRuntime {
    private final String dotnetCommand;
    private final Path databasePath;
    private final Thread shutdownHook;
    private Process process;
    private int port;

    public Game2048AppRuntime(String dotnetCommand, String databasePath) {
        this.dotnetCommand = dotnetCommand;
        this.databasePath = Path.of(databasePath).toAbsolutePath();
        this.shutdownHook = new Thread(this::stop, "game2048-e2e-runtime-shutdown");
        Runtime.getRuntime().addShutdownHook(shutdownHook);
    }

    public synchronized void start() {
        if (process != null && process.isAlive()) {
            return;
        }

        try {
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
            processBuilder.environment().put("Logging__LogLevel__Default", "Warning");

            System.out.println("[INFO] Starting Game2048 web app on " + getBaseUrl());
            process = processBuilder.start();
            waitUntilReady(logFile.toPath());
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

    public void clearData() {
        try (Connection connection = DriverManager.getConnection("jdbc:sqlite:" + databasePath);
             Statement statement = connection.createStatement()) {
            statement.executeUpdate("DELETE FROM LeaderboardEntries");
        } catch (SQLException e) {
            throw new IllegalStateException("Failed to clear Game2048 saved games.", e);
        }
    }

    private int findAvailablePort() throws IOException {
        try (ServerSocket socket = new ServerSocket(0)) {
            return socket.getLocalPort();
        }
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
}
