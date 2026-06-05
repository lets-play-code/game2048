package org.game2048;

import org.game2048.entity.ExistingGameSeed;
import org.testcharm.cucumber.restful.RestfulStep;
import org.testcharm.jfactory.DataRepository;

import java.util.ArrayList;
import java.util.Collection;
import java.util.List;

public class ExistingGameSeedRepository implements DataRepository {
    private final Game2048AppRuntime appRuntime;
    private final RestfulStep restfulStep;
    private final List<ExistingGameSeed> seeds = new ArrayList<>();

    public ExistingGameSeedRepository(Game2048AppRuntime appRuntime, RestfulStep restfulStep) {
        this.appRuntime = appRuntime;
        this.restfulStep = restfulStep;
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
            restfulStep.postObjectInJson("/api/test/games/clear-cache", null);
        }
    }

    @Override
    public void save(Object object) {
        ExistingGameSeed seed = (ExistingGameSeed) object;
        seeds.add(seed);
        restfulStep.postInJson("/api/test/games/" + seed.getGameId(), buildRequestBody(seed));
    }

    private String buildRequestBody(ExistingGameSeed seed) {
        return "{"
                + "\"boardJson\":\"" + escape(seed.getBoardJson()) + "\","
                + "\"score\":" + seed.getScore() + ","
                + "\"win\":" + seed.isWin() + ","
                + "\"lose\":" + seed.isLose() + ","
                + "\"scoreRecorded\":" + seed.isScoreRecorded() + ","
                + "\"leakedShouldAddTile\":" + seed.isLeakedShouldAddTile()
                + "}";
    }

    private String escape(String value) {
        if (value == null) {
            return "";
        }

        return value
                .replace("\\", "\\\\")
                .replace("\"", "\\\"");
    }
}
