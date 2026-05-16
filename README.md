# Game2048

一个基于 ASP.NET Core 的 2048 游戏，包含本地排行榜、自动存档和手动存档。

## 环境要求

- .NET SDK 8
- Git
- 浏览器
- Java 8+（仅在运行 `e2e-tests` 时需要）

## 目录结构

```text
Game2048.sln
├── src/
│   ├── Game2048.Game/   # 游戏核心逻辑与 SQLite 持久化
│   └── Game2048.Web/    # Web API 与静态页面
├── tests/
│   └── Game2048.Tests/  # xUnit 测试
└── e2e-tests/           # Gradle + Cucumber 端到端测试
```

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

启动后打开终端输出中的地址，默认通常是：

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
- 排行榜与存档保存在 SQLite 中

默认数据库文件：

```text
src/Game2048.Web/App_Data/game2048.db
```

## 运行端到端测试

```bash
cd e2e-tests
./gradlew cucumber
```

如需让 HTML 覆盖率报告启用 ReportGenerator license（例如方法级覆盖率细节），在运行前设置环境变量：

```bash
export REPORTGENERATOR_LICENSE='<your-license>'
cd e2e-tests
./gradlew cucumber
```

也支持仓库级别别名环境变量：

```bash
export GAME2048_REPORTGENERATOR_LICENSE='<your-license>'
cd e2e-tests
./gradlew cucumber
```

报告输出位置：

```text
e2e-tests/build/reports/coverage/backend-runtime.cobertura.xml
e2e-tests/build/reports/coverage/backend-runtime-html/index.html
```

Windows：

```powershell
cd e2e-tests
.\gradlew.bat cucumber
```
