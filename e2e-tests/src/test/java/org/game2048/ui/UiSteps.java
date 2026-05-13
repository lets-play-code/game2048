package org.game2048.ui;

import com.microsoft.playwright.Locator;
import com.microsoft.playwright.Page;
import io.cucumber.java.After;
import io.cucumber.java.Before;
import io.cucumber.java.zh_cn.那么;
import org.springframework.beans.factory.annotation.Autowired;
import org.testcharm.jfactory.JFactory;
import org.testcharm.pf.By;
import org.testcharm.pf.PlaywrightPageFlow;

public class UiSteps {

    @Autowired
    private PlaywrightBrowser browser;

    @那么("关闭页面")
    @After("@ui")
    public void close() {
        browser.close(null);
    }

    private MainPage mainPage;

    @Autowired
    private JFactory jFactory;

    @Before("@ui")
    public void open() {
        browser.launchByUrl("");
        Page page = browser.getPage();
        Locator html = page.locator("html");
        PlaywrightPageFlow pageflow = PlaywrightPageFlow.builder().page(page).jFactory(jFactory).build();
        Element element = new Element(pageflow, html);
        element.setLocator(By.css("html"));
        mainPage = new MainPage(element);
    }

    @那么("用户应该:")
    public void 用户应该(String expression) {
        mainPage.should(expression);
    }

}
