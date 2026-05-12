package org.testcharm;

import com.github.leeonky.jfactory.DataRepository;
import org.testcharm.entity.LeaderboardEntryRow;

import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.time.Instant;
import java.util.ArrayList;
import java.util.Collection;
import java.util.List;

public class SQLiteLeaderboardDataRepository implements DataRepository {
    private final Game2048AppRuntime appRuntime;

    public SQLiteLeaderboardDataRepository(Game2048AppRuntime appRuntime) {
        this.appRuntime = appRuntime;
    }

    @Override
    public <T> Collection<T> queryAll(Class<T> type) {
        if (!type.equals(LeaderboardEntryRow.class)) {
            return new ArrayList<T>();
        }

        List<T> rows = new ArrayList<T>();
        try (Connection connection = openConnection();
             Statement statement = connection.createStatement();
             ResultSet resultSet = statement.executeQuery(
                     "SELECT Id, PlayerName, BestScore, UpdatedAtUtc " +
                             "FROM LeaderboardEntries ORDER BY BestScore DESC, PlayerName COLLATE NOCASE")) {
            while (resultSet.next()) {
                rows.add(type.cast(new LeaderboardEntryRow()
                        .setId(resultSet.getLong("Id"))
                        .setPlayerName(resultSet.getString("PlayerName"))
                        .setBestScore(resultSet.getInt("BestScore"))
                        .setUpdatedAtUtc(readInstant(resultSet, "UpdatedAtUtc"))));
            }
        } catch (SQLException e) {
            throw new IllegalStateException("Failed to read leaderboard data from SQLite.", e);
        }
        return rows;
    }

    @Override
    public void clear() {
        execute("DELETE FROM LeaderboardEntries");
        execute("DELETE FROM sqlite_sequence WHERE name = 'LeaderboardEntries'");
    }

    @Override
    public void save(Object object) {
        LeaderboardEntryRow row = (LeaderboardEntryRow) object;
        try (Connection connection = openConnection();
             PreparedStatement statement = connection.prepareStatement(
                     "INSERT INTO LeaderboardEntries (PlayerName, BestScore, UpdatedAtUtc) VALUES (?, ?, ?)")) {
            statement.setString(1, row.getPlayerName());
            statement.setInt(2, row.getBestScore());
             statement.setString(3, row.getUpdatedAtUtc().toString());
            statement.executeUpdate();
        } catch (SQLException e) {
            throw new IllegalStateException("Failed to insert leaderboard data into SQLite.", e);
        }
    }

    private Connection openConnection() throws SQLException {
        return DriverManager.getConnection("jdbc:sqlite:" + appRuntime.getDatabasePath().toAbsolutePath());
    }

    private void execute(String sql) {
        try (Connection connection = openConnection();
             Statement statement = connection.createStatement()) {
            statement.executeUpdate(sql);
        } catch (SQLException e) {
            throw new IllegalStateException("Failed to execute SQLite statement: " + sql, e);
        }
    }

    private Instant readInstant(ResultSet resultSet, String columnName) throws SQLException {
        String text = resultSet.getString(columnName);
        if (text == null) {
            return null;
        }
        try {
            return Instant.parse(text);
        } catch (Exception ignored) {
            return resultSet.getTimestamp(columnName).toInstant();
        }
    }
}
