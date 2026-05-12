package org.testcharm;

import com.github.leeonky.jfactory.CompositeDataRepository;
import com.github.leeonky.jfactory.JFactory;
import com.github.leeonky.jfactory.MemoryDataRepository;
import org.testcharm.entity.LeaderboardEntryRow;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class Factories {

    @Bean
    public Game2048AppRuntime game2048AppRuntime(@Value("${testcharm.app.command:dotnet}") String dotnetCommand) {
        return new Game2048AppRuntime(dotnetCommand);
    }

    @Bean
    public JFactory factorySet(Game2048AppRuntime appRuntime) {
        return new EntityFactory(
                new CompositeDataRepository(new MemoryDataRepository())
                        .registerByType(LeaderboardEntryRow.class, new SQLiteLeaderboardDataRepository(appRuntime))
        );
    }
}
