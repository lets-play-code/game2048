package org.testcharm.entity;

import javax.persistence.Column;
import javax.persistence.Entity;
import javax.persistence.GeneratedValue;
import javax.persistence.GenerationType;
import javax.persistence.Id;
import javax.persistence.Table;

@Entity
@Table(name = "SavedGames")
public class SavedGameRow {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "SlotKey", nullable = false)
    private String slotKey;

    @Column(name = "BoardJson", nullable = false)
    private String boardJson;

    @Column(name = "Score", nullable = false)
    private int score;

    @Column(name = "Win", nullable = false)
    private boolean win;

    @Column(name = "Lose", nullable = false)
    private boolean lose;

    @Column(name = "ScoreRecorded", nullable = false)
    private boolean scoreRecorded;

    @Column(name = "LeakedShouldAddTile", nullable = false)
    private boolean leakedShouldAddTile;

    @Column(name = "SavedAtUtc", nullable = false)
    private String savedAtUtc;

    public Long getId() {
        return id;
    }

    public SavedGameRow setId(Long id) {
        this.id = id;
        return this;
    }

    public String getSlotKey() {
        return slotKey;
    }

    public SavedGameRow setSlotKey(String slotKey) {
        this.slotKey = slotKey;
        return this;
    }

    public String getBoardJson() {
        return boardJson;
    }

    public SavedGameRow setBoardJson(String boardJson) {
        this.boardJson = boardJson;
        return this;
    }

    public int getScore() {
        return score;
    }

    public SavedGameRow setScore(int score) {
        this.score = score;
        return this;
    }

    public boolean isWin() {
        return win;
    }

    public SavedGameRow setWin(boolean win) {
        this.win = win;
        return this;
    }

    public boolean isLose() {
        return lose;
    }

    public SavedGameRow setLose(boolean lose) {
        this.lose = lose;
        return this;
    }

    public boolean isScoreRecorded() {
        return scoreRecorded;
    }

    public SavedGameRow setScoreRecorded(boolean scoreRecorded) {
        this.scoreRecorded = scoreRecorded;
        return this;
    }

    public boolean isLeakedShouldAddTile() {
        return leakedShouldAddTile;
    }

    public SavedGameRow setLeakedShouldAddTile(boolean leakedShouldAddTile) {
        this.leakedShouldAddTile = leakedShouldAddTile;
        return this;
    }

    public String getSavedAtUtc() {
        return savedAtUtc;
    }

    public SavedGameRow setSavedAtUtc(String savedAtUtc) {
        this.savedAtUtc = savedAtUtc;
        return this;
    }
}
