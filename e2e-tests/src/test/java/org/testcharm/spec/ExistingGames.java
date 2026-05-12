package org.testcharm.spec;

import com.github.leeonky.jfactory.Spec;
import org.testcharm.entity.ExistingGameSeed;

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
