package org.testcharm.entity;

public class ExistingGameSeed {
    private String gameId;
    private String boardJson;
    private int score;
    private boolean win;
    private boolean lose;
    private boolean scoreRecorded;
    private boolean leakedShouldAddTile;

    public String getGameId() {
        return gameId;
    }

    public ExistingGameSeed setGameId(String gameId) {
        this.gameId = gameId;
        return this;
    }

    public String getBoardJson() {
        return boardJson;
    }

    public ExistingGameSeed setBoardJson(String boardJson) {
        this.boardJson = boardJson;
        return this;
    }

    public int getScore() {
        return score;
    }

    public ExistingGameSeed setScore(int score) {
        this.score = score;
        return this;
    }

    public boolean isWin() {
        return win;
    }

    public ExistingGameSeed setWin(boolean win) {
        this.win = win;
        return this;
    }

    public boolean isLose() {
        return lose;
    }

    public ExistingGameSeed setLose(boolean lose) {
        this.lose = lose;
        return this;
    }

    public boolean isScoreRecorded() {
        return scoreRecorded;
    }

    public ExistingGameSeed setScoreRecorded(boolean scoreRecorded) {
        this.scoreRecorded = scoreRecorded;
        return this;
    }

    public boolean isLeakedShouldAddTile() {
        return leakedShouldAddTile;
    }

    public ExistingGameSeed setLeakedShouldAddTile(boolean leakedShouldAddTile) {
        this.leakedShouldAddTile = leakedShouldAddTile;
        return this;
    }
}
