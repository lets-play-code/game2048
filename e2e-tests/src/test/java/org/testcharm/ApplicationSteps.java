package org.testcharm;

import com.github.leeonky.cucumber.restful.RestfulStep;
import com.github.leeonky.dal.Assertions;
import com.github.leeonky.jfactory.JFactory;
import io.cucumber.java.After;
import io.cucumber.java.Before;
import io.cucumber.spring.CucumberContextConfiguration;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.test.context.SpringBootContextLoader;
import org.springframework.test.context.ContextConfiguration;

@ContextConfiguration(classes = {CucumberConfiguration.class}, loader = SpringBootContextLoader.class)
@CucumberContextConfiguration
public class ApplicationSteps {
    @Autowired
    private JFactory jFactory;

    @Autowired
    private Game2048AppRuntime appRuntime;

    @Autowired
    private RestfulStep restfulStep;

    @Value("${testcharm.dal.dumpinput:true}")
    private boolean dalDumpInput;

    @Before
    public void disableDALDump() {
        Assertions.dumpInput(dalDumpInput);
    }

    @Before(order = 0)
    public void startApplication() {
        appRuntime.start();
        restfulStep.setBaseUrl(appRuntime.getBaseUrl());
    }

    @Before(order = 1)
    public void clearState() {
        jFactory.getDataRepository().clear();
    }

    @After
    public void stopApplication() {
        appRuntime.stop();
    }
}
