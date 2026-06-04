# Copilot 说明 — Game2048

目的：为未来的 Copilot 会话提供仓库快速参考：构建/测试/运行命令、环境要求、高层架构概览、ABP 要点与仓库特有约定。

---

## 环境要求（Quick prerequisites）

- .NET SDK（注意：各 *.csproj 的 TargetFramework 以 csproj 为准；仓库 README 原文提到 .NET SDK 8，但项目文件 target 为 net10.0，请确保本地 SDK 与项目兼容）
- Docker / Docker Compose
- Git
- 浏览器（运行本地前端或 VNC 时）
- Java 17（运行 e2e-tests 时需要）

---

## 目录结构（概要）

Game2048.sln
├── src/
│   ├── Game2048.Game/   # 游戏核心逻辑与 MySQL 持久化
│   └── Game2048.Web/    # Web API、静态前端与 ABP 模块
├── tests/
│   └── Game2048.Tests/  # xUnit 测试
├── e2e-tests/           # Gradle + Cucumber 端到端测试
└── docker/              # 容器配置（Web、MySQL、Playwright、浏览器 VNC）

---

## 默认端口（来自 README）

- MySQL: 127.0.0.1:53306
- MockServer: 127.0.0.1:51081
- Web: 127.0.0.1:5000 (或通过 ASPNETCORE_URLS 指定)
- Playwright server: 127.0.0.1:53000
- Browser VNC: 127.0.0.1:57900

---

## 快速命令（Build / Test / Run / E2E）

- 构建解决方案

```bash
dotnet build Game2048.sln
```

- 运行全部单元测试

```bash
dotnet test Game2048.sln
```

- 运行单个测试方法（示例）

```bash
dotnet test tests/Game2048.Tests/Game2048.Tests.csproj --filter "FullyQualifiedName~Game2048.Tests.Game2048Test.test_init_status"
```

- 运行单个测试类（示例）

```bash
dotnet test tests/Game2048.Tests/Game2048.Tests.csproj --filter "FullyQualifiedName~Game2048.Tests.Game2048Test"
```

- 在本地运行 Web 应用

```bash
dotnet run --project src/Game2048.Web
# 或自定义监听地址：
ASPNETCORE_URLS=http://127.0.0.1:5099 dotnet run --project src/Game2048.Web
```

- 启动依赖服务（在运行集成/端到端测试前必须）

```bash
docker compose up -d --build mysql8 mockserver
# 启动完整堆栈（包含 Playwright / VNC 等）:
docker compose up -d --build
```

- 运行端到端测试（需要 Java 17）

```bash
cd e2e-tests
./gradlew cucumber
```

- 端到端覆盖率报告输出（示例）

```text
e2e-tests/build/reports/coverage/backend-runtime-html/index.html
```

- 代码格式化（仓库未提供专门的 lint 脚本，可选）

```bash
# 若未安装：
dotnet tool install -g dotnet-format
# 执行格式化：
dotnet format
```

---

## 高层架构（大局视角）

- Game2048.sln：包含核心逻辑、Web 层与测试。
- src/Game2048.Game：游戏核心逻辑及持久化（Entity Framework Core + MySQL）。
- src/Game2048.Web：基于 ABP 的 Web 模块（Minimal API + 静态前端），包含 API 控制器与运行时服务。入口：Program.cs（使用 Autofac 和 Game2048WebModule）。
- tests/Game2048.Tests：xUnit 单元/集成测试，引用 Game 与 Web 项目作为集成样例。
- e2e-tests：Gradle + Cucumber 的端到端测试（需要 Java 17）。
- docker/ + docker-compose.yml：本地集成环境（MySQL、MockServer、Playwright、浏览器 VNC 等）。

---

## ABP 相关要点（重点）

### ABP CLI 使用（快速参考）

- 安装 / 更新 ABP CLI（全局）

```bash
# 全局安装（推荐）
dotnet tool install -g Volo.Abp.Cli
# 已安装时升级
dotnet tool update -g Volo.Abp.Cli
```

- 验证与帮助

```bash
abp --version
abp --help
abp <command> --help
```

- 使用建议
  - 若仓库使用 tool manifest（dotnet-tools.json），使用 `dotnet tool restore`；一般开发场景推荐全局安装以便直接使用 `abp` 命令。
  - 在运行会影响数据库或模块结构的 CLI 命令前，先阅读 `abp <command> --help` 并备份/确认当前配置。
  - 官方文档：https://docs.abp.io/en/abp/latest/CLI


### 模块与启动（关键实现点）

- 模块：`src/Game2048.Web/Game2048WebModule.cs`，继承自 `AbpModule`，依赖 `AbpAspNetCoreMvcModule`、`AbpAutofacModule`。
- 启动：`Program.cs` 使用 `builder.Host.UseAutofac()` 并通过 `await builder.AddApplicationAsync<Game2048WebModule>()` 注册 ABP 应用；随后调用 `await app.InitializeApplicationAsync()` 与 `app.RunAsync()`。文件包含 `public partial class Program { }`，便于 WebApplicationFactory 测试。
- 生命周期钩子：
  - ConfigureServices(ServiceConfigurationContext)：在此处可根据配置（例如 `Game2048:EnableTestApi`）注册测试专用控制器或配置 JsonOptions（仓库设置为 camelCase）。
  - OnApplicationInitialization(ApplicationInitializationContext)：用于应用启动时的初始化，例如：持久化配置、EnsureDatabaseReady、注册请求日志中间件、静态文件与路由、SPA fallback。
- 关键配置键：
  - `Game2048:EnableTestApi`（是否开启测试 API）
  - `Game2048:ConnectionString`（数据库连接字符串；如未提供会回退到默认）
  - `Game2048:ForcedGeneratedTileValue`（用于测试时强制生成方块值）
  - `Game2048:LeaderboardWallUrl`（排行榜地址）
- 容器 / DI：仓库通过 Autofac（AbpAutofacModule + UseAutofac）集成，添加自定义容器行为请使用 ABP/Autofac 扩展点。

---

## 仓库关键约定与注意事项

- 存档槽（save slots）顺序固定且重要：`auto`, `slot1`, `slot2`, `slot3`。API 与测试假定响应按此顺序返回。
- 每次移动会刷新 `auto` 存档；手动槽位可通过 API 存/取。
- 在运行集成测试 / e2e 测试前，先用 `docker compose` 启动 `mysql8` 与 `mockserver`。e2e 还可能需要 Playwright 容器和 Java 17。
- JSON 序列化策略为 camelCase（由模块 JsonOptions 配置）。
- 请求日志中间件将生成 `LegacyRequest` 日志条目（在 Game2048WebModule.OnApplicationInitialization 注册）。
- tests/Game2048.Tests 同时引用 Game 与 Web 项目，可作为集成测试示例。

---

## Copilot 应优先查看的文件/路径

- `README.md`（已合并其关键信息至本文件）
- `Game2048.sln`
- `src/Game2048.Web/Program.cs`
- `src/Game2048.Web/Game2048WebModule.cs`
- `src/Game2048.Web/*Controller*.cs`, `src/Game2048.Web/Game2048RuntimeService.cs`
- `src/Game2048.Game/**`（核心逻辑、EF Migrations）
- `tests/Game2048.Tests/**`（单元与集成测试示例）
- `e2e-tests/features/**` 与 `e2e-tests` 的 Gradle 配置
- `docker/` 与 `docker-compose.yml`

---

## 变更/维护建议

- 若需新增仅用于测试的 API，请通过 `Game2048:EnableTestApi` 配置控制，并在 `ConditionalTestApiControllerConvention` 中处理注册。
- 修改数据库连接或迁移配置时，优先在本地通过 `docker compose up -d mysql8` 启动依赖并在安全环境中执行迁移。

---

（本文件由 README.md、项目文件与仓库结构合并生成，已去除对 AGENTS.md 的引用）
