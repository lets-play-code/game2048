package org.testcharm.entity;

import java.time.Instant;

public class LeaderboardEntryRow {
    private Long id;
    private String playerName;
    private int bestScore;
    private Instant updatedAtUtc;

    public Long getId() {
        return id;
    }

    public LeaderboardEntryRow setId(Long id) {
        this.id = id;
        return this;
    }

    public String getPlayerName() {
        return playerName;
    }

    public LeaderboardEntryRow setPlayerName(String playerName) {
        this.playerName = playerName;
        return this;
    }

    public int getBestScore() {
        return bestScore;
    }

    public LeaderboardEntryRow setBestScore(int bestScore) {
        this.bestScore = bestScore;
        return this;
    }

    public Instant getUpdatedAtUtc() {
        return updatedAtUtc;
    }

    public LeaderboardEntryRow setUpdatedAtUtc(Instant updatedAtUtc) {
        this.updatedAtUtc = updatedAtUtc;
        return this;
    }
}
