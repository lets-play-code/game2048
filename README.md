# LegacyCode C# 练习工程

这是从 `~/code/ase/legacy-2048/` 迁移来的 C#/.NET 7 练习项目，用于课程中的遗留代码补测试、特征测试、代码味道扫描与重构练习。

当前 solution 也纳入了 Gilded Rose kata，对应的 legacy 核心实现位于 `src/GildedRose.App`，并配有独立的 `src/GildedRose.Web` 沙盒页面，方便直接观察 `UpdateQuality()` 的行为。

## 迁移说明

- 源项目：Java + Swing + Maven。
- 目标项目：.NET 7，2048 核心逻辑在 C# 类库 `Game2048.Game` 中，整个 solution 名称为 `LegacyCode.sln`。
- UI：为了兼容 macOS 和 Windows，使用 ASP.NET Core 提供一个极薄的 Web 页面。
- 练习定位：刻意保留了原始代码中的多处 legacy 特征，同时示范如何在兼容前提下逐步拆模块，例如：
  - `myWin` / `myLose` / `myScore` 这类 façade 公开字段与 legacy 命名。
  - `Game2048.Tile.value` 继续用 `string` 表示数值，并在合并时反复 `int.Parse`。
  - `right()` / `up()` / `down()` 继续通过旋转棋盘复用 `left()`。
  - `drawTile()` 每个格子都更新一次 `ScoreText` 的怪异行为仍被保留，但已经被 presenter 模块和 approval 测试锁定。
  - 排行榜仍是进程内内存存储，运行时保存成绩仍会发布 wall message，只是通过可覆盖 seam 与测试隔离。

## 目录结构

```text
legacy-2048-csharp/
├── LegacyCode.sln
├── src/
│   ├── Game2048.Game/        # 2048 façade + Core / Presentation / Leaderboard / Models 模块
│   ├── Game2048.Web/         # 跨平台 Web UI，调用 C# 游戏逻辑并展示记录页
│   ├── GildedRose.App/       # 保留原始 if/else 嵌套的 Gilded Rose legacy 核心
│   └── GildedRose.Web/       # Gilded Rose 沙盒 Web UI，直接驱动 UpdateQuality()
└── tests/
    ├── Game2048.Tests/       # façade seam、approval、guard 与模块边界测试
    └── GildedRose.Tests/     # Gilded Rose 测试、TexttestFixture 与 approval 风格快照测试
```

## 环境要求

与 `../dev-env.md` 对齐：

- .NET SDK 7.0+
- Git
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

当前 `dotnet test LegacyCode.sln` 应该可以直接通过；`Game2048` 测试已经通过 deterministic seam 隔离了随机数与 wall-posting 副作用，不再依赖真实网络。

如果只想验证 2048 模块，可以使用：

```bash
dotnet test tests/Game2048.Tests/Game2048.Tests.csproj
```

如果只想先检查 façade seam：

```bash
dotnet test tests/Game2048.Tests/Game2048.Tests.csproj --filter "FullyQualifiedName~Game2048FacadeTest"
```

如果要执行 2048 模块的分支覆盖率门槛验证：

```bash
dotnet test tests/Game2048.Tests/Game2048.Tests.csproj \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=json \
  /p:Threshold=90 \
  /p:ThresholdType=branch \
  /p:ThresholdStat=total
```

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

方向键移动，`ESC` 或页面上的 `New Game` 重开。点击 `View Records` 可以查看当前内存中的 2048 记录榜；终局后可输入昵称并保存一次成绩。

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

## 2048 模块与测试安全网

`Game2048` 仍然是对 Web 层暴露的 façade，但内部职责已经拆成更清晰的模块：

- `Core/GameBoard` + `Core/BoardTile`：棋盘状态、移动/合并、计分、胜负与随机补 tile。
- `Presentation/GameStatePresenter`：把棋盘与终局状态映射成 `Game2048State` / `TileState`。
- `Leaderboard/InMemoryLeaderboardStore` + `LeaderboardService`：内存榜单、最佳分保留、并列排名、wall message 发布编排。
- `Models/*`：Web 层和测试复用的 DTO。

### 兼容性与 deterministic seams

- 对外 public API 仍由 `Game2048` façade 提供，Web 路由契约保持不变。
- `protected virtual nextRandomDouble()` 让测试可以完全控制补 tile 的位置和值。
- `protected virtual postLeaderboardWallMessage()` 让测试可以拦截 wall message，而不发真实 HTTP 请求。
- 运行时保存成绩仍会发布 wall message；测试则通过 `DeterministicGame2048` 覆盖 seam 来隔离副作用。
- `Core` / `Presentation` / `Leaderboard` 模块类型保持 `internal`，通过 `src/Game2048.Game/Properties/AssemblyInfo.cs` 里的 `InternalsVisibleTo("Game2048.Tests")` 暴露给测试，避免为了测试而扩大运行时 public API。

### 排行榜语义

- 记录的是 2048 游戏成绩，而不是 poker 筹码。
- 玩家在终局后手动输入昵称，再保存记录。
- 每个昵称只保留个人最高分。
- 同一局终局后只能保存一次。
- 排行榜只保存在进程内存中，服务重启后清空。
- `getPositionOfPlayer(playerName)` 仍保留按名次查询能力；并列分数共享名次，后续名次跳号。

### Game2048 测试结构

- `Game2048FacadeTest`：验证 deterministic seams 与 façade 兼容性。
- `ApprovalTest` + `ApprovalFiles/*.approved.txt`：锁定 full/win/leaderboard 三类长流程行为。
- `Game2048GuardTest`：补齐参数校验、重复保存、unknown rank 等 guard/branch 行为。
- `Core/GameBoardTest`、`Presentation/GameStatePresenterTest`、`Leaderboard/LeaderboardServiceTest`：模块边界测试。
- `tests/Game2048.Tests/AssemblyInfo.cs` 关闭该测试程序集的并行执行；因为排行榜仍是进程内共享内存状态，新测试应继续复用 reset helper，而不是依赖并行隔离。

这组测试既保留了 legacy 行为（包括一些奇怪但兼容的表现），也为后续重构提供了可重复执行的 safety net。
