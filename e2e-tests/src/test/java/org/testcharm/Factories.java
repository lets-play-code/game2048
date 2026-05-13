package org.testcharm;

import com.github.leeonky.jfactory.CompositeDataRepository;
import com.github.leeonky.jfactory.JFactory;
import com.github.leeonky.jfactory.MemoryDataRepository;
import com.github.leeonky.jfactory.repo.JPADataRepository;
import lombok.SneakyThrows;
import org.mockserver.client.MockServerClient;
import org.mockserver.integration.ClientAndServer;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;
import org.testcharm.entity.ExistingGameSeed;
import org.testcharm.entity.LeaderboardEntryRow;
import org.testcharm.entity.SavedGameRow;

import javax.persistence.EntityManager;
import javax.persistence.EntityManagerFactory;

@Configuration
public class Factories {
    @Bean
    public Game2048AppRuntime game2048AppRuntime(
            @Value("${testcharm.app.command:dotnet}") String dotnetCommand,
            @Value("${testcharm.game2048.database-path}") String databasePath,
            @Value("${testcharm.game2048.forced-tile-value:}") String forcedTileValue,
            ClientAndServer mockServer) {
        return new Game2048AppRuntime(
                dotnetCommand,
                databasePath,
                forcedTileValue,
                "http://127.0.0.1:" + mockServer.getLocalPort() + "/api/wall");
    }

    @Bean
    public EntityManager entityManager(EntityManagerFactory entityManagerFactory) {
        return entityManagerFactory.createEntityManager();
    }

    @Bean
    public JPADataRepository jpaDataRepository(EntityManager entityManager) {
        return new JPADataRepository(entityManager);
    }

    @Bean
    public JFactory factorySet(JPADataRepository jpaDataRepository, Game2048AppRuntime game2048AppRuntime) {
                return new EntityFactory(
                        new CompositeDataRepository(new MemoryDataRepository())
                                .registerByType(ExistingGameSeed.class, new ExistingGameSeedRepository(game2048AppRuntime))
                                .registerByType(LeaderboardEntryRow.class, jpaDataRepository)
                                .registerByType(SavedGameRow.class, jpaDataRepository)
                );
    }

    @SneakyThrows
    @Primary
    @Bean
    public MockServerClient createMockServerClient(ClientAndServer mockServer) {
        return new MockServerClient("127.0.0.1", mockServer.getLocalPort()) {
            @Override
            public void close() {
            }
        };
    }

    @Bean(destroyMethod = "stop")
    public ClientAndServer mockServer() {
        return ClientAndServer.startClientAndServer(0);
    }

}
