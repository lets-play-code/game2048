# Game2048

一个基于 ASP.NET Core 的 2048 游戏，使用 MySQL 保存排行榜和存档，并提供 Docker 运行环境与端到端测试。

## 环境要求

- .NET SDK 8
- Docker / Docker Compose
- Git
- 浏览器
- Java 17（运行 `e2e-tests` 时需要）

## 目录结构

```text
Game2048.sln
├── src/
│   ├── Game2048.Game/   # 游戏核心逻辑与 MySQL 持久化
│   └── Game2048.Web/    # Web API 与静态页面
├── tests/
│   └── Game2048.Tests/  # xUnit 测试
├── e2e-tests/           # Gradle + Cucumber 端到端测试
└── docker/              # Web、MySQL、Playwright 相关镜像配置
```

## 启动依赖服务

运行本地测试前，先启动 MySQL 和 MockServer：

```bash
docker compose up -d --build mysql8 mockserver
```

如果需要完整容器环境（包含 Web、Playwright 和浏览器驱动）：

```bash
docker compose up -d --build
```

默认端口：

- MySQL: `127.0.0.1:53306`
- MockServer: `127.0.0.1:51081`
- Web: `127.0.0.1:5000`
- Playwright server: `127.0.0.1:53000`
- Browser VNC: `127.0.0.1:57900`

## 构建

```bash
dotnet build Game2048.sln
```

## 运行单元测试

```bash
dotnet test Game2048.sln
```

## 运行游戏

```bash
dotnet run --project src/Game2048.Web
```

启动后打开：

```text
http://localhost:5000
```

如果端口被占用，可以指定地址：

```bash
ASPNETCORE_URLS=http://127.0.0.1:5099 dotnet run --project src/Game2048.Web
```

## 游戏说明

- 使用方向键移动方块
- 按 `ESC` 或点击 `New Game` 重新开始
- 每次移动都会刷新 `Auto` 存档
- `Slot 1`、`Slot 2`、`Slot 3` 可手动 `Save` / `Load`
- 排行榜与存档保存在 MySQL 8 中

## 运行端到端测试

```bash
cd e2e-tests
./gradlew cucumber
```
