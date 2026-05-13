package org.testcharm.spec;

import com.github.leeonky.jfactory.Spec;
import org.testcharm.entity.LeaderboardWallResponse;

public class LeaderboardWallResponses {
    public static class 排行榜墙响应 extends Spec<LeaderboardWallResponse> {
        @Override
        public void main() {
            property("statusCode").value(200);
        }
    }
}
