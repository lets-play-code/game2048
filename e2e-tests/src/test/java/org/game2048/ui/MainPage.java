package org.game2048.ui;

import org.testcharm.pf.AbstractPanel;

public class MainPage extends AbstractPanel<Element> {

    public MainPage(Element element) {
        super(element);
    }

    public RecordsPage records() {
        perform("caption[View Records].click");
        return new RecordsPage(element().locate("css[.records-card]").single());
    }
}
