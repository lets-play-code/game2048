package org.game2048;

import lombok.SneakyThrows;
import org.game2048.entity.ExistingGameSeed;
import org.game2048.entity.LeaderboardEntryRow;
import org.game2048.entity.NextGameIdSeed;
import org.game2048.entity.SavedGameRow;
import org.mockserver.client.MockServerClient;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;
import org.testcharm.cucumber.restful.RestfulStep;
import org.testcharm.jfactory.CompositeDataRepository;
import org.testcharm.jfactory.JFactory;
import org.testcharm.jfactory.MemoryDataRepository;
import org.testcharm.jfactory.repo.JPADataRepository;

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
            @Value("${testcharm.game2048.coverage-recorder-directory:}") String coverageRecorderDirectory,
            @Value("${testcharm.game2048.coverage-report-path:}") String coverageReportPath,
            @Value("${testcharm.game2048.forced-tile-value:}") String forcedTileValue,
            @Value("${mock-server.endpoint}") String mockServerEndpoint) {
        URI endpoint = URI.create(mockServerEndpoint);
        var restfulStep = new RestfulStep();
        return new Game2048AppRuntime(
                dotnetCommand,
                baseUrl,
                connectionString,
                jdbcUrl,
                databaseUser,
                databasePassword,
                coverageRecorderDirectory,
                coverageReportPath,
                forcedTileValue,
                endpoint.resolve("/api/wall").toString(),
                restfulStep);
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
        var restfulStep = new RestfulStep();
        restfulStep.setBaseUrl(game2048AppRuntime.getBaseUrl());
        return new EntityFactory(
                new CompositeDataRepository(new MemoryDataRepository())
                        .registerByType(ExistingGameSeed.class, new ExistingGameSeedRepository(game2048AppRuntime, restfulStep))
                        .registerByType(NextGameIdSeed.class, new NextGameIdSeedRepository(game2048AppRuntime, restfulStep))
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
