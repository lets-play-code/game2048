package org.game2048.spec;

import org.testcharm.jfactory.Spec;
import org.game2048.entity.LeaderboardEntryRow;

import java.time.Instant;

public class LeaderboardEntries {
    public static class 排行榜记录 extends Spec<LeaderboardEntryRow> {
        @Override
        public void main() {
            property("updatedAtUtc").value(Instant.parse("2026-01-01T00:00:00Z"));
        }
    }
}
