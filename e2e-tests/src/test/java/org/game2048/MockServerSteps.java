package org.game2048;

import io.cucumber.java.After;
import io.cucumber.java.Before;
import io.cucumber.java.zh_cn.假如;
import io.cucumber.java.zh_cn.并且;
import org.mockserver.client.MockServerClient;
import org.springframework.beans.factory.annotation.Autowired;
import org.testcharm.jfactory.JFactory;

import java.util.Collections;
import java.util.List;
import java.util.stream.Collectors;
import java.util.stream.IntStream;

import static org.testcharm.dal.Assertions.expect;

public class MockServerSteps {

    @Autowired
    private MockServerClient mockServerClient;
    @Autowired
    private DALMockServer dalMockServer;
    @Autowired
    private JFactory jFactory;

    @Before(order = 0)
    public void setupMockServer() {
        mockServerClient.reset();
        dalMockServer.clear();
    }

    @After(order = 0)
    public void tearDownMockServer() {
        dalMockServer.stopDelay();
    }

    @假如("Mock API:")
    public void mock_api(String mock) {
        String[] requestAndResponses = mock.split("---");

        List<DALMockServer.ResponseBuilder> responseBuilders = IntStream.range(1, requestAndResponses.length)
                .mapToObj(i -> (DALMockServer.ResponseBuilder)
                        jFactory.useDAL().create("DefaultResponseBuilder", requestAndResponses[i]))
                .collect(Collectors.toList());

        dalMockServer.mock(Collections.singletonMap(requestAndResponses[0].trim(), responseBuilders));
    }

    @并且("验证Mock API:")
    public void 验证mockAPI(String dalExpression) {
        expect(dalMockServer.requests()).should(dalExpression);
    }
}
