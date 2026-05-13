# LegacyCode C# 练习工程

这是从 `~/code/ase/legacy-2048/` 迁移来的 C#/.NET 7 练习项目，用于课程中的遗留代码补测试、特征测试、代码味道扫描与重构练习。

当前 solution 也纳入了 Gilded Rose kata，对应的 legacy 核心实现位于 `src/GildedRose.App`，并配有独立的 `src/GildedRose.Web` 沙盒页面，方便直接观察 `UpdateQuality()` 的行为。

## 迁移说明

- 源项目：Java + Swing + Maven。
- 目标项目：.NET 7，2048 核心逻辑在 C# 类库 `Game2048.Game` 中，整个 solution 名称为 `LegacyCode.sln`。
- UI：为了兼容 macOS 和 Windows，使用 ASP.NET Core 提供一个极薄的 Web 页面。
- 练习定位：刻意保留了原始代码中的多处坏味道，例如：
  - `myTiles` / `myWin` / `myLose` / `myScore` 这类命名和公开字段。
  - `Tile.value` 继续用 `string` 表示数值，并在合并时反复 `int.Parse`。
  - `right()` / `up()` / `down()` 继续通过旋转棋盘复用 `left()`。
  - `paint()` / `drawTile()` 仍放在核心类里，保留 UI 与逻辑混杂的练习素材。
  - `drawTile()` 仍然每个格子都更新一次 `ScoreText`，测试中用 `ScoreTextDrawCount` 保留这个现象。

## 目录结构

```text
legacy-2048-csharp/
├── LegacyCode.sln
├── src/
│   ├── Game2048.Game/        # C# 迁移后的 2048 核心逻辑，包含并入 Game2048 的记录榜逻辑
│   ├── Game2048.Web/         # 跨平台 Web UI，调用 C# 游戏逻辑并展示记录页
│   ├── GildedRose.App/       # 保留原始 if/else 嵌套的 Gilded Rose legacy 核心
│   └── GildedRose.Web/       # Gilded Rose 沙盒 Web UI，直接驱动 UpdateQuality()
└── tests/
    ├── Game2048.Tests/       # xUnit 测试，包含 2048 核心测试与保留的 legacy 跳过测试
    └── GildedRose.Tests/     # Gilded Rose 测试、TexttestFixture 与 approval 风格快照测试
```

## 环境要求

与 `../dev-env.md` 对齐：

- .NET SDK 7.0+
- Git
- Docker / Docker Compose
- 浏览器（macOS/Windows 自带浏览器即可）

不需要 Node.js/Yarn 才能运行本项目；Web UI 由 ASP.NET Core 静态文件提供。

## 构建、测试与运行

### 构建

macOS / Linux：

```bash
cd course/testing_ai/legacy-2048-csharp
dotnet build LegacyCode.sln
```

Windows PowerShell：

```powershell
cd course\testing_ai\legacy-2048-csharp
dotnet build LegacyCode.sln
```

### 运行测试

先启动 Game2048 测试环境：

```bash
cd course/testing_ai/legacy-2048-csharp
docker compose -f docker-compose.stack.yml up -d --build mysql8 mockserver game2048-web
```

macOS / Linux：

```bash
cd course/testing_ai/legacy-2048-csharp
dotnet test LegacyCode.sln
```

Windows PowerShell：

```powershell
cd course\testing_ai\legacy-2048-csharp
dotnet test LegacyCode.sln
```

说明：默认 `dotnet test LegacyCode.sln` 应通过。`tests/Game2048.Tests/Game2048LegacyBehaviorTest.cs` 保留了一组显式 `Skip` 的 legacy characterization fixtures，用于后续练习时复现那些“怪现象”，但不会阻塞主干测试。

运行 Game2048 的 cucumber e2e：

```bash
cd course/testing_ai/legacy-2048-csharp/e2e-tests
./gradlew cucumber
```

这套 cucumber 默认复用 `docker-compose.stack.yml` 里的 `game2048-web` 容器。

默认依赖端口：

- MySQL 8: `127.0.0.1:53306`
- MockServer: `127.0.0.1:51081`

如果要把 **Game2048 Web**、**Playwright** 和 **web-driver** 也一起放进容器，使用完整 stack：

```bash
cd course/testing_ai/legacy-2048-csharp
docker compose -f docker-compose.stack.yml up -d --build
```

额外暴露端口：

- Game2048 Web: `127.0.0.1:5000`
- Playwright: `127.0.0.1:13000`
- web-driver: `127.0.0.1:14444`
- web-driver VNC: `127.0.0.1:17900`

### 运行 2048 游戏

macOS / Linux：

```bash
cd course/testing_ai/legacy-2048-csharp
dotnet run --project src/Game2048.Web
```

Windows PowerShell：

```powershell
cd course\testing_ai\legacy-2048-csharp
dotnet run --project src\Game2048.Web
```

默认会在本机启动 ASP.NET Core Web 服务，然后打开浏览器访问终端输出中的地址。通常是：

```text
http://localhost:5000
```

如果端口被占用，可以在启动前指定，例如：

```bash
ASPNETCORE_URLS=http://127.0.0.1:5099 dotnet run --project src/Game2048.Web
```

方向键移动，`ESC` 或页面上的 `New Game` 重开。2048 Web 启动时会自动连接并迁移本地 MySQL 8（默认连接到 `127.0.0.1:53306` 的 `game2048` 库）。页面支持：

- `View Records` 查看 MySQL 中持久化的排行榜。
- 每次方向键操作后自动刷新 `Auto` 存档。
- `Slot 1` / `Slot 2` / `Slot 3` 的手动 `Save` / `Load`。
- 终局后输入昵称并保存一次成绩；保存成绩时仍会额外触发 legacy wall `POST` 副作用。

## Gilded Rose 模块

`src/GildedRose.App` 里保留了来源示例中的 `Item` 与 `GildedRose` 实现，`UpdateQuality()` 的原始 `if/else` 嵌套没有被摊平。`tests/GildedRose.Tests/TestFixture.cs` 保留了文本入口，`ApprovalTest.cs` 和 `GildedRoseTest.cs` 则是把来源示例测试迁移为当前工程的 xUnit 写法。

### 运行 Gilded Rose Sandbox

macOS / Linux：

```bash
cd course/testing_ai/legacy-2048-csharp
dotnet run --project src/GildedRose.Web
```

Windows PowerShell：

```powershell
cd course\testing_ai\legacy-2048-csharp
dotnet run --project src\GildedRose.Web
```

启动后打开终端输出中的地址，默认通常是：

```text
http://localhost:5000
```

页面默认加载与 `TestFixture` 一致的示例库存，支持 `Reset to Sample`、`Clear Inventory`、手动增删改商品，以及点击 `Next Day` 触发一次真实的 legacy `UpdateQuality()`。

## 2048 记录榜整合练习

原本独立的 `Leaderboard` / `Player` 已被硬并进 `src/Game2048.Game/Game2048.cs`，以保留“高耦合、低内聚”的遗留代码练习特征。

### 当前场景

- 记录的是 2048 游戏成绩，而不是 poker 筹码。
- 玩家在终局后手动输入昵称，再保存记录。
- 每个昵称只保留个人最高分。
- 排行榜持久化到 MySQL 8，服务重启后仍会保留。
- 提供 1 个自动存档（`auto`）和 3 个手动槽位（`slot1` / `slot2` / `slot3`）。
- `getPositionOfPlayer(playerName)` 仍保留按名次查询的能力。

### 当前实现特征

- `Game2048` 仍同时负责棋盘逻辑、终局状态、记录保存、MySQL 读写和存档恢复，继续保留 God Class / legacy 练习感。
- MySQL schema 与 migrations 位于 `src/Game2048.Game`，Web 启动时自动执行迁移，不需要手工建库。
- 同一局终局后只能保存一次成绩；但保存动作仍先写 MySQL，再 `POST http://7k7k6666.com/api/wall`，所以外部 wall 失败时仍可能留下“数据库已更新、当前局未标记已保存”的 legacy 现象。
- Web 页面仍是单个 `index.html`，通过显示/隐藏切换游戏页和记录页，只额外扩展了 Save / Load 面板。
- 项目刻意保留了若干异常 legacy 行为，供后续补测试、调试和重构练习；README 不剧透其具体根因。

### 测试说明

- 排行榜、MySQL 持久化、全局存档、Auto Save 与 Web API 的主干测试位于 `tests/Game2048.Tests/`，默认应通过。
- `tests/Game2048.Tests/Game2048LegacyBehaviorTest.cs` 保留为 `Skip` 的教学脚手架，用来提示仓库中存在一些故意保留的怪现象。
- 练习重点之一仍然是观察：排行榜、存档、内存状态与外部 HTTP 副作用被一起塞进核心游戏类后，代码和行为会变得更加纠缠。
