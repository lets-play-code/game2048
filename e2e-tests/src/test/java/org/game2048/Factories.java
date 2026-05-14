package org.game2048;

import org.testcharm.jfactory.CompositeDataRepository;
import org.testcharm.jfactory.JFactory;
import org.testcharm.jfactory.MemoryDataRepository;
import org.testcharm.jfactory.repo.JPADataRepository;
import lombok.SneakyThrows;
import org.mockserver.client.MockServerClient;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;
import org.game2048.entity.ExistingGameSeed;
import org.game2048.entity.LeaderboardEntryRow;
import org.game2048.entity.NextGameIdSeed;
import org.game2048.entity.SavedGameRow;

import javax.persistence.EntityManager;
import javax.persistence.EntityManagerFactory;
import java.net.URI;

@Configuration
public class Factories {
    @Bean
    public Game2048AppRuntime game2048AppRuntime(
            @Value("${testcharm.app.command:dotnet}") String dotnetCommand,
            @Value("${testcharm.game2048.base-url:}") String baseUrl,
            @Value("${testcharm.game2048.connection-string}") String connectionString,
            @Value("${testcharm.game2048.jdbc-url}") String jdbcUrl,
            @Value("${spring.datasource.username}") String databaseUser,
            @Value("${spring.datasource.password}") String databasePassword,
            @Value("${testcharm.game2048.forced-tile-value:}") String forcedTileValue,
            @Value("${mock-server.endpoint}") String mockServerEndpoint) {
        URI endpoint = URI.create(mockServerEndpoint);
        return new Game2048AppRuntime(
                dotnetCommand,
                baseUrl,
                connectionString,
                jdbcUrl,
                databaseUser,
                databasePassword,
                forcedTileValue,
                endpoint.resolve("/api/wall").toString());
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
                        .registerByType(NextGameIdSeed.class, new NextGameIdSeedRepository(game2048AppRuntime))
                        .registerByType(LeaderboardEntryRow.class, jpaDataRepository)
                        .registerByType(SavedGameRow.class, jpaDataRepository)
        );
    }

    @SneakyThrows
    @Primary
    @Bean
    public MockServerClient createMockServerClient(@Value("${mock-server.endpoint}") String mockServerEndpoint) {
        URI endpoint = URI.create(mockServerEndpoint);
        int port = endpoint.getPort() == -1 ? 80 : endpoint.getPort();
        return new MockServerClient(endpoint.getHost(), port) {
            @Override
            public void close() {
            }
        };
    }
}
