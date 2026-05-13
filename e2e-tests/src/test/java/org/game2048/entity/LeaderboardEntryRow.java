package org.game2048.entity;

import javax.persistence.Column;
import javax.persistence.Convert;
import javax.persistence.Entity;
import javax.persistence.GeneratedValue;
import javax.persistence.GenerationType;
import javax.persistence.Id;
import javax.persistence.Table;
import java.time.Instant;

@Entity
@Table(name = "LeaderboardEntries")
public class LeaderboardEntryRow {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "PlayerName", nullable = false)
    private String playerName;

    @Column(name = "BestScore", nullable = false)
    private int bestScore;

    @Column(name = "UpdatedAtUtc", nullable = false)
    @Convert(converter = InstantStringConverter.class)
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
