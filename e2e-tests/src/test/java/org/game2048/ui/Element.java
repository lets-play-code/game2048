package org.game2048.ui;

import com.microsoft.playwright.Locator;
import org.testcharm.pf.PlaywrightElement;
import org.testcharm.pf.PlaywrightPageFlow;

public class Element extends PlaywrightElement<Element, PlaywrightPageFlow> {

    public Element(PlaywrightPageFlow pageFlow, Locator locator) {
        super(pageFlow, locator);
    }

}
