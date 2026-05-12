# `legacy-code-csharp` Copilot 指南

## 构建与测试命令

本仓库基于 **.NET 7**，两个 Web 项目都不依赖 Node.js。

```bash
dotnet build LegacyCode.sln
dotnet test LegacyCode.sln
```

运行单个测试时，使用 xUnit 的全限定名过滤：

```bash
dotnet test tests/Game2048.Tests/Game2048.Tests.csproj --filter "FullyQualifiedName=Game2048.Tests.Game2048WebTest.move_endpoint_creates_auto_save_after_a_keypress"
dotnet test tests/GildedRose.Tests/GildedRose.Tests.csproj --filter "FullyQualifiedName=GildedRose.Tests.GildedRoseWebTest.sample_items_match_the_text_fixture_inventory"
```

直接运行两个薄 Web 沙盒：

```bash
dotnet run --project src/Game2048.Web
dotnet run --project src/GildedRose.Web
```

## 高层架构

```text
LegacyCode.sln
├── Game2048 练习线
│   ├── src/Game2048.Game
│   │   ├── 单个 legacy God Class：棋盘逻辑 + 渲染 DTO + SQLite 持久化 + 外部 wall POST
│   │   └── EF Core 的 SQLite schema 和 migrations 放在这里
│   ├── src/Game2048.Web
│   │   ├── Minimal API + 单页静态 UI
│   │   ├── 启动时配置数据库路径并执行迁移
│   │   └── 用 ConcurrentDictionary 按 game id 保存内存中的游戏实例
│   └── tests/Game2048.Tests
│       ├── 核心/持久化测试直接调用类库
│       └── Web 测试通过 WebApplicationFactory 注入独立 SQLite 路径
└── GildedRose 练习线
    ├── src/GildedRose.App
    │   └── 经典 legacy kata 核心：Item + 嵌套 if/else 的 UpdateQuality()
    ├── src/GildedRose.Web
    │   └── Minimal API + 静态沙盒页，每次只执行一次 UpdateQuality()
    └── tests/GildedRose.Tests
        ├── 单元测试
        ├── approval 风格文本夹具
        └── Web API 测试
```

### Game2048 流程

`src/Game2048.Web/Program.cs` 会先调用 `Game2048.ConfigurePersistence(...)` 配置数据库路径，再调用 `EnsureDatabaseReady()` 执行迁移，然后提供单个 `wwwroot/index.html`。浏览器把 `gameId` 存在 `localStorage` 中，并只通过 Minimal API 与后端通信。API 在修改某个 `Game2048` 实例前会先加锁，而且 `/move` 每次都会顺手刷新 `auto` 存档。

`src/Game2048.Game/Game2048.cs` 是刻意保留的高耦合练习代码，里面混在一起的职责包括：

- 棋盘状态与移动逻辑
- `paint()` / `drawTile()` 生成渲染 DTO
- 排行榜与存档槽位的 SQLite 访问
- 保存成绩后向 `http://7k7k6666.com/api/wall` 发送副作用请求

### Gilded Rose 流程

`src/GildedRose.Web/Program.cs` 会先复制请求里的 items，再实例化 `GildedRose.App.GildedRose`，执行一次 `UpdateQuality()`，最后返回更新后的 items。这个 Web 沙盒只是同一套 legacy 规则的另一种观察入口，不是单独的领域层实现。

## 关键约定

- 把 **Game2048** 和 **Gilded Rose** 当成同一个 solution 里的两条独立练习线处理。大多数改动只会落在其中一条线对应的 app/web/tests 组合里。
- 除非任务明确要求重构，否则要保留 **legacy 形状**。像 `myTiles`、`myWin`、`myLose`、`myScore`、`Tile.value`，以及 `GildedRose.UpdateQuality()` 里的嵌套 `if/else`，都是刻意保留的训练素材。
- 在 **Game2048** 中，存档槽位键名必须保持为 `auto`、`slot1`、`slot2`、`slot3`。UI 和测试都依赖这些精确值，也依赖 `/move` 只刷新 `auto` 槽位的行为。
- `Game2048.saveLeaderboardRecord()` 会**先写 SQLite，再调用外部 wall POST**，并且只有在 POST 成功后才把当前局标记为已保存成绩。测试依赖这个 legacy 不对称行为，不要顺手“修正”。
- `Game2048State` 是 API 和前端之间的契约。两个 Web 项目都通过 `ConfigureHttpJsonOptions(... PropertyNamingPolicy = JsonNamingPolicy.CamelCase)` 输出 camelCase JSON，静态 HTML 直接依赖这个约定。
- `tests/Game2048.Tests` 禁用了并行执行，并通过 `Game2048PersistenceScope` 为每次测试创建独立的临时 SQLite 文件。新增持久化或 Web 测试时，复用这个模式，不要共享默认的 `App_Data/game2048.db`。
- 一部分 Game2048 测试会故意保持 `Skip`，作为 **legacy practice fixture**。除非任务明确要求把某个特征测试转正，否则保持跳过状态。
- Gilded Rose 的样例库存需要在 `src/GildedRose.Web/SampleInventory.cs` 和 `tests/GildedRose.Tests/TestFixture.cs` 之间保持一致；`GildedRoseWebTest` 会断言两个入口暴露出的样例数据完全相同。
- approval 风格测试会从输出目录读取 `tests/GildedRose.Tests/ApprovalFiles/ThirtyDays.approved.txt`。如果你有意修改行为，就更新 approved 文件，而不是改掉这种测试风格。
