package org.game2048.spec;

import org.testcharm.jfactory.Spec;
import org.game2048.entity.ExistingGameSeed;

public class ExistingGames {
    public static class 已存在的游戏 extends Spec<ExistingGameSeed> {
        @Override
        public void main() {
            property("score").value(0);
            property("win").value(false);
            property("lose").value(false);
            property("scoreRecorded").value(false);
            property("leakedShouldAddTile").value(false);
            property("boardJson").value("[\"\", \"\", \"\", \"\", \"\", \"\", \"\", \"\", \"\", \"\", \"\", \"\", \"\", \"\", \"\", \"\"]");
        }
    }
}
