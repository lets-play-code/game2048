package org.testcharm;

import com.github.leeonky.jfactory.DataRepository;
import org.testcharm.entity.LeaderboardWallResponse;

import java.util.Collection;
import java.util.Collections;

public class LeaderboardWallResponseRepository implements DataRepository {
    private final Game2048AppRuntime appRuntime;
    private LeaderboardWallResponse current = new LeaderboardWallResponse().setStatusCode(200);

    public LeaderboardWallResponseRepository(Game2048AppRuntime appRuntime) {
        this.appRuntime = appRuntime;
    }

    @Override
    @SuppressWarnings("unchecked")
    public <T> Collection<T> queryAll(Class<T> type) {
        return (Collection<T>) Collections.singletonList(copy(current));
    }

    @Override
    public void clear() {
        current = new LeaderboardWallResponse().setStatusCode(200);
        appRuntime.resetLeaderboardWall();
    }

    @Override
    public void save(Object object) {
        current = copy((LeaderboardWallResponse) object);
        appRuntime.configureLeaderboardWallStatusCode(current.getStatusCode());
    }

    private static LeaderboardWallResponse copy(LeaderboardWallResponse source) {
        return new LeaderboardWallResponse().setStatusCode(source.getStatusCode());
    }
}
