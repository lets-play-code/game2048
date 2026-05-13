package org.game2048.ui;

import org.testcharm.dal.runtime.DALCollection;
import org.testcharm.dal.runtime.ProxyObject;
import org.testcharm.pf.AbstractPanel;
import org.testcharm.pf.Elements;

import java.util.Iterator;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Set;
import java.util.stream.Stream;

public class RecordsPage extends AbstractPanel<Element> implements Iterable {
    public RecordsPage(Element element) {
        super(element);
    }

    @Override
    public Iterator iterator() {
        return table(locate("css[thead]"), locate("css[tbody]")).iterator();
    }

    private Stream<HtmlTableRow> table(Elements<Element> headers, Elements<Element> body) {
        Map<String, Integer> columns = new LinkedHashMap<>();
        headers.single().locate(".css[th]::filter: {visible: true}").list().stream().forEach(e -> columns.put(e.value().text(), e.index()));
        return body.single().locate(".css[tr]").list().values().map(e -> new HtmlTableRow(e, columns));
    }

    public static class HtmlTableRow extends AbstractPanel<Element> implements ProxyObject {
        private final Map<String, Integer> columns;
        private final DALCollection<Element> cells;

        public HtmlTableRow(Element element, Map<String, Integer> columns) {
            super(element);
            this.columns = columns;
            cells = locate("css[td]::filter: {visible: true}").list();
        }

        @Override
        public Object getValue(Object column) {
            return cells.getByIndex(columns.computeIfAbsent((String) column, ig -> {
                throw new IllegalArgumentException("Column <%s> not exist".formatted(column));
            }));
        }

        @Override
        public Set<?> getPropertyNames() {
            return columns.keySet();
        }
    }
}
