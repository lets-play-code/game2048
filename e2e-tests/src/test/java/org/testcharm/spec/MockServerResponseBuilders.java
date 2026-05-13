package org.testcharm.spec;

import com.github.leeonky.jfactory.Spec;
import org.testcharm.DALMockServer;

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
