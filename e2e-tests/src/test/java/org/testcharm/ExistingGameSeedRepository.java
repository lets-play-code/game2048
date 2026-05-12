package org.testcharm;

import com.github.leeonky.jfactory.DataRepository;
import org.testcharm.entity.ExistingGameSeed;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Collection;
import java.util.List;

public class ExistingGameSeedRepository implements DataRepository {
    private final Game2048AppRuntime appRuntime;
    private final List<ExistingGameSeed> seeds = new ArrayList<>();

    public ExistingGameSeedRepository(Game2048AppRuntime appRuntime) {
        this.appRuntime = appRuntime;
    }

    @Override
    @SuppressWarnings("unchecked")
    public <T> Collection<T> queryAll(Class<T> type) {
        return (Collection<T>) new ArrayList<>(seeds);
    }

    @Override
    public void clear() {
        seeds.clear();
        if (appRuntime.isRunning()) {
            post("/api/test/games/clear-cache", null);
        }
    }

    @Override
    public void save(Object object) {
        ExistingGameSeed seed = (ExistingGameSeed) object;
        seeds.add(seed);
        post("/api/test/games/" + seed.getGameId(), buildRequestBody(seed));
    }

    private void post(String path, byte[] body) {
        try {
            HttpURLConnection connection = (HttpURLConnection) new URL(appRuntime.getBaseUrl() + path).openConnection();
            connection.setConnectTimeout(1000);
            connection.setReadTimeout(1000);
            connection.setRequestMethod("POST");
            if (body != null) {
                connection.setDoOutput(true);
                connection.setRequestProperty("Content-Type", "application/json");
                try (OutputStream outputStream = connection.getOutputStream()) {
                    outputStream.write(body);
                }
            }

            int responseCode = connection.getResponseCode();
            if (responseCode < 200 || responseCode >= 300) {
                throw new IllegalStateException("Seeding existing game failed with status " + responseCode + ": " + readBody(connection));
            }
            closeQuietly(connection);
        } catch (IOException e) {
            throw new IllegalStateException("Failed to call the Game2048 test API.", e);
        }
    }

    private static String readBody(HttpURLConnection connection) throws IOException {
        InputStream stream = connection.getErrorStream();
        if (stream == null) {
            stream = connection.getInputStream();
        }
        if (stream == null) {
            return "";
        }

        try (InputStream inputStream = stream) {
            ByteArrayOutputStream buffer = new ByteArrayOutputStream();
            byte[] chunk = new byte[1024];
            int count;
            while ((count = inputStream.read(chunk)) != -1) {
                buffer.write(chunk, 0, count);
            }
            byte[] bytes = buffer.toByteArray();
            return new String(bytes, StandardCharsets.UTF_8);
        }
    }

    private static void closeQuietly(HttpURLConnection connection) {
        try (InputStream inputStream = connection.getInputStream()) {
            while (inputStream != null && inputStream.read() != -1) {
            }
        } catch (IOException ignored) {
        }
    }

    private static byte[] buildRequestBody(ExistingGameSeed seed) {
        String json = "{"
                + "\"boardJson\":\"" + escape(seed.getBoardJson()) + "\","
                + "\"score\":" + seed.getScore() + ","
                + "\"win\":" + seed.isWin() + ","
                + "\"lose\":" + seed.isLose() + ","
                + "\"scoreRecorded\":" + seed.isScoreRecorded() + ","
                + "\"leakedShouldAddTile\":" + seed.isLeakedShouldAddTile()
                + "}";
        return json.getBytes(StandardCharsets.UTF_8);
    }

    private static String escape(String value) {
        if (value == null) {
            return "";
        }

        return value
                .replace("\\", "\\\\")
                .replace("\"", "\\\"");
    }
}
