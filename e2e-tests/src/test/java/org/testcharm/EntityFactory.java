package org.testcharm;

import com.github.leeonky.jfactory.DataRepository;
import com.github.leeonky.jfactory.JFactory;
import com.github.leeonky.jfactory.Spec;
import com.github.leeonky.util.Classes;

public class EntityFactory extends JFactory {

    public EntityFactory(DataRepository dataRepository) {
        super(dataRepository);
        configFactory();
    }

    private void configFactory() {
        Classes.subTypesOf(Spec.class, "org.testcharm.spec").forEach(c -> register((Class) c));

        ignoreDefaultValue(p -> p.getName().equals("id"));
    }
}
