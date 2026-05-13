package org.game2048.ui;

import com.microsoft.playwright.BrowserType;
import com.microsoft.playwright.Page;
import com.microsoft.playwright.Playwright;
import io.cucumber.java.Scenario;
import lombok.SneakyThrows;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

import javax.annotation.PreDestroy;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.time.Instant;
import java.util.Map;

@Slf4j
@Component
public class PlaywrightBrowser {
    private static final String UPLOAD_AND_DOWNLOAD_DEFAULT_FOLDER = "/tmp/f2c/";
    private Playwright playwright;
    private com.microsoft.playwright.Browser browser;
    private Page page = null;

    public void launchByUrl(String path) {
        getPage().navigate("http://web.net:5000" + path);
    }

    public void close(Scenario scenario) {
        if (page != null) {
            page.close();
            if (scenario != null)
                saveVideoForFailedScenario(scenario);
            page = null;
        }
        if (browser != null) {
            browser.close();
            browser = null;
        }
    }

    @PreDestroy
    public void destroy() {
        playwright.close();
    }

    public Page getPage() {
        if (page == null) {
            String wsHost = "localhost";
            int wsPort = 53000;
            createPlaywright();
            browser = playwright.chromium().connect("ws://" + wsHost + ":" + wsPort + "/", new BrowserType.ConnectOptions().setHeaders(Map.of("x-playwright-launch-options", ("{ \"headless\": false,     \"downloadsPath\": \"%s\" }").formatted(UPLOAD_AND_DOWNLOAD_DEFAULT_FOLDER))));
            var context = browser.newContext(new com.microsoft.playwright.Browser.NewContextOptions()
                    .setAcceptDownloads(true)
                    .setViewportSize(1920, 1080)
                    .setTimezoneId("Asia/Shanghai")
                    .setRecordVideoSize(1920, 1080)
                    .setRecordVideoDir(Paths.get("../../../dev-ops/videos"))
            );
            page = context.newPage();
            page.clock().install();
            page.onConsoleMessage(message -> {
                if (message.type().equals("error")) {
                    log.error("Console message: {}", message.text());
                } else if (message.type().equals("log")) {
                    log.info("Console message: {}", message.text());
                }
            });
            page.onRequestFailed(request -> {
                if (request.failure() != null) {
                    log.error("Request failed: {}", request.failure());
                    log.error("Request failed: {}", request.url());
                    log.error("Request failed: {}", request.method());
                    log.error("Request failed: {}", request.headers());
                    log.error("Request failed: {}", request.postData());
                    var response = request.response();
                    if (response != null && !response.ok()) {
                        log.error("Request failed response: {}", response.status());
                        log.error("Request failed response: {}", response.statusText());
                        log.error("Request failed response: {}", response.headers());
                        log.error("Request failed response: {}", response.text());
                    }
                }
            });
        }
        return page;
    }

    private void createPlaywright() {
        if (playwright == null) playwright = Playwright.create();
    }

    @SneakyThrows
    private void saveVideoForFailedScenario(Scenario scenario) {
        if (scenario.isFailed()) {
            getPage().video().saveAs(Path.of("../../../dev-ops/videos/%s-%d.webm".formatted(scenario.getName(), Instant.now().toEpochMilli())));
        }
    }
}
