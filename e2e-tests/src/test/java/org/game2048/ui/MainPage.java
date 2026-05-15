package org.game2048.ui;

import org.testcharm.pf.AbstractPanel;
import org.testcharm.pf.PlaywrightElement;

import java.util.List;

public class MainPage extends AbstractPanel<Element> {

    public MainPage(Element element) {
        super(element);
    }

    public RecordsPage records() {
        perform("caption[View Records].click");
        return new RecordsPage(locate("css[.records-card]").single());
    }

    public MainPage newGame() {
        perform("caption[New Game].click");
        return this;
    }

    public Object score() {
        return locate("css[.score]").single();
    }

    public List<String> board() {
        return locate("css[.board .tile]").list().collect().stream().map(PlaywrightElement::text).toList();
    }

    public MainPage right() {
        perform("caption[→].click");
        return this;
    }

    public MainPage left() {
        perform("caption[←].click");
        return this;
    }

    public MainPage up() {
        perform("caption[↑].click");
        return this;
    }

    public MainPage down() {
        perform("caption[↓].click");
        return this;
    }

    public MainPage slot1Save() {
        locate("caption[Save]").list().getByIndex(0).click();
        return this;
    }

    public MainPage autoLoad() {
        locate("caption[Load]").list().getByIndex(0).click();
        return this;
    }

}
