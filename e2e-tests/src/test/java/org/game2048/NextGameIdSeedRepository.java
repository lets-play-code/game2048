package org.game2048;

import org.game2048.entity.NextGameIdSeed;
import org.testcharm.cucumber.restful.RestfulStep;
import org.testcharm.jfactory.DataRepository;

import java.util.ArrayList;
import java.util.Collection;
import java.util.List;

public class NextGameIdSeedRepository implements DataRepository {
    private final Game2048AppRuntime appRuntime;
    private final RestfulStep restfulStep;
    private final List<NextGameIdSeed> seeds = new ArrayList<>();

    public NextGameIdSeedRepository(Game2048AppRuntime appRuntime, RestfulStep restfulStep) {
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
    }

    @Override
    public void save(Object object) {
        NextGameIdSeed seed = (NextGameIdSeed) object;
        seeds.add(seed);
        restfulStep.postInJson("/api/test/games/next-id", buildRequestBody(seed));
    }

    private String buildRequestBody(NextGameIdSeed seed) {
        return "{\"id\":\"" + escape(seed.getId()) + "\"}";
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
