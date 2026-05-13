package org.game2048.spec;

import org.testcharm.jfactory.Spec;
import org.game2048.DALMockServer;

public class MockServerResponseBuilders {
    public static class DefaultResponseBuilder extends Spec<DALMockServer.ResponseBuilder> {
        @Override
        public void main() {
            property("code").value(200);
            property("times").value(0);
            property("delayResponse").value(0);
        }
    }
}
