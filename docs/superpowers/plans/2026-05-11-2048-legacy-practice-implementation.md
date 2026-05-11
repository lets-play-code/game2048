# 2048 Legacy Practice Expansion Implementation Plan

> **Execution model:** This plan is designed for a single continuous executor. Start it with `/run-plan <plan-file>` after approval. The runner creates task branches for the touched repo(s), keeps status in `.pi/runs/...`, and only stops early for explicit stop conditions.

**Goal:** 把 2048 扩展为更适合遗留代码练习的项目：默认引入 3 类源于错误逻辑的偶发 bug，使用 EF Core + SQLite 持久化排行榜与 1 个自动存档 / 3 个手动存档，并保持保存排行榜时的外部 wall `POST` 副作用。

**Architecture:** 保持 `Game2048` 作为 God Class，不引入 repository / service 分层。EF Core 类型和 migrations 放在 `src/Game2048.Game` 中，但排行榜保存、存档读写、数据库初始化、外部 HTTP 副作用、故意 bug 仍主要集中在 `Game2048.cs` 与现有 Web 入口中。测试采用 TDD：先补失败测试，再做最小实现，并把故意 bug 的复现脚手架保留为 `Skip` 的 characterization tests，避免主干测试直接剧透答案。

**Tech Stack:** .NET 7, ASP.NET Core Minimal API, EF Core, EF Core Sqlite, xUnit, WebApplicationFactory

**Repo Scope:** single repo (`/Users/wuke/code/course-practice/legacy-code-csharp`)

---

## File Structure / Responsibility Map

### Production files
- Modify: `src/Game2048.Game/Game2048.Game.csproj` — 添加 EF Core / SQLite 依赖。
- Modify: `src/Game2048.Game/Game2048.cs` — 保持 God Class，增加持久化配置、数据库读写、排行榜逻辑迁移、存档/读档、自动存档、以及 3 个故意 bug 的实现。
- Create: `src/Game2048.Game/Game2048DbContext.cs` — 定义 EF Core `DbContext` 与模型配置。
- Create: `src/Game2048.Game/Game2048PersistenceModels.cs` — 定义 `LeaderboardEntryEntity`、`SavedGameEntity`。
- Create: `src/Game2048.Game/Migrations/*InitialGame2048Persistence*.cs` — 初始 schema migration 及 snapshot（由 `dotnet ef` 生成并提交）。
- Modify: `src/Game2048.Web/Program.cs` — 计算数据库路径、调用 `ConfigurePersistence` / `EnsureDatabaseReady`、增加 `/api/saves`、`/save/{slotKey}`、`/load/{slotKey}`，并在每次 move 后自动保存 `auto`。
- Modify: `src/Game2048.Web/wwwroot/index.html` — 增加 Auto / Slot1 / Slot2 / Slot3 面板与对应的 Save / Load 交互。
- Modify: `README.md` — 文档化 SQLite 自动初始化、排行榜持久化、自动存档/手动槽位，不剧透故意 bug 的根因。

### Test files
- Modify: `tests/Game2048.Tests/Game2048.Tests.csproj` — 增加 `Microsoft.AspNetCore.Mvc.Testing`，并引用 `src/Game2048.Web` 以支持最小 API 集成测试。
- Modify: `tests/Game2048.Tests/Game2048Test.cs` — 保留并更新现有初始化/排行榜相关测试，使其基于临时 SQLite 数据库而不是内存字典。
- Create: `tests/Game2048.Tests/Game2048PersistenceTest.cs` — 直接覆盖排行榜持久化、存档/读档、隐藏状态恢复。
- Create: `tests/Game2048.Tests/Game2048PersistenceTestSupport.cs` — 临时数据库路径、测试初始化、WebApplicationFactory 配置辅助。
- Create: `tests/Game2048.Tests/Game2048WebTest.cs` — 覆盖启动迁移、`/api/saves`、save/load endpoint、move 自动存档。
- Create: `tests/Game2048.Tests/Game2048LegacyBehaviorTest.cs` — 保留为 `Skip` 的故意 bug 复现脚手架。

### Docs / plans
- Reference only: `docs/superpowers/specs/2026-05-11-2048-legacy-practice-design.md` — 已批准设计，不在实现阶段重写。
- Create: `docs/superpowers/plans/2026-05-11-2048-legacy-practice-implementation.md` — 本计划文件。

---

## Gate 1: EF Core foundation, startup migration, and isolated test harness

**Goal:**
- 建立 EF Core + SQLite 基础设施。
- 让 Web 启动时自动迁移数据库。
- 提供测试隔离所需的临时数据库路径配置能力。

**Files:**
- Modify: `src/Game2048.Game/Game2048.Game.csproj`
- Create: `src/Game2048.Game/Game2048DbContext.cs`
- Create: `src/Game2048.Game/Game2048PersistenceModels.cs`
- Modify: `src/Game2048.Web/Program.cs`
- Modify: `tests/Game2048.Tests/Game2048.Tests.csproj`
- Create: `tests/Game2048.Tests/Game2048PersistenceTestSupport.cs`
- Create: `tests/Game2048.Tests/Game2048WebTest.cs`
- Create: `src/Game2048.Game/Migrations/*InitialGame2048Persistence*.cs`

**Preconditions / Notes:**
- 运行时默认数据库路径必须保持为 `src/Game2048.Web/App_Data/game2048.db`（即 Web 内容根目录下的 `App_Data/game2048.db`）。
- 允许为测试加入配置覆盖入口（推荐读取配置键 `Game2048:DatabasePath`，默认回退到固定路径）；不要用全局环境变量做每个测试的隔离，因为会引入并发污染。
- `DbContext` 必须放在 `src/Game2048.Game` 中；不要创建 repository 或 service 层。
- 在这个 gate 就把 `SavedGameEntity` 中未来要用到的“泄漏状态字段”加进去，避免后续再改迁移。

**Verification:**
- Run: `dotnet test tests/Game2048.Tests/Game2048.Tests.csproj --filter "FullyQualifiedName~Game2048WebTest.startup_creates_the_configured_sqlite_database_and_applies_migrations"`
- Expected: PASS；测试使用的临时 `.db` 文件被创建，应用启动时没有手工建库步骤。
- Run: `dotnet build src/Game2048.Web/Game2048.Web.csproj`
- Expected: PASS

**Continue when:**
- 启动迁移测试通过。
- 生成的 migration 文件已提交到 `src/Game2048.Game/Migrations/`。
- `Program.cs` 能在默认路径启动并调用自动迁移。

**Stop and report when:**
- `dotnet ef` 在当前环境不可用，且生成 migration 需要手工编写 C# migration 文件。
- 为了让 EF Core 在类库中工作，不得不引入额外项目或跨 repo 结构调整。
- 数据库路径覆盖方案会破坏默认开箱即用的运行要求。

- [ ] Step 1: 在 `tests/Game2048.Tests/Game2048WebTest.cs` 中先写一个失败的启动测试：使用临时数据库路径启动 Web app，并断言数据库文件存在且首页/`/api/games` 可用。
- [ ] Step 2: 运行上述单测，确认当前为 RED（缺少配置入口、缺少迁移、缺少 DbContext）。
- [ ] Step 3: 在 `Game2048.Game.csproj` 加入 EF Core 包；创建 `Game2048DbContext` 与 `Game2048PersistenceModels`，至少包含排行榜与存档表以及唯一索引配置。
- [ ] Step 4: 在 `Game2048.cs` 增加持久化配置入口（例如 `ConfigurePersistence` / `EnsureDatabaseReady`）；在 `Program.cs` 计算数据库路径、创建 `App_Data/` 并调用自动迁移。
- [ ] Step 5: 生成 migration：`dotnet ef migrations add InitialGame2048Persistence --project src/Game2048.Game --startup-project src/Game2048.Web --output-dir Migrations`。
- [ ] Step 6: 重新运行 targeted test，确认变绿；必要时提交一个 checkpoint commit。

### Gate 2: Replace in-memory leaderboard with SQLite while preserving the legacy wall side effect

**Goal:**
- 用 SQLite 替换内存排行榜。
- 保持“先写榜单，再发外部 wall POST，再标记 `myScoreRecorded`”的 legacy 顺序。
- 更新现有 2048 测试，使其基于临时数据库运行。

**Files:**
- Modify: `src/Game2048.Game/Game2048.cs`
- Modify: `tests/Game2048.Tests/Game2048Test.cs`
- Create: `tests/Game2048.Tests/Game2048PersistenceTest.cs`
- Modify: `tests/Game2048.Tests/Game2048PersistenceTestSupport.cs`

**Preconditions / Notes:**
- 不要 mock 外部 wall `POST` 成功路径来“修饰”当前行为；绿色测试应围绕数据库持久化结果与已知副作用顺序来写。
- 删除或停用 `leaderboardScoresByPlayer` 这套内存字典逻辑；排行榜读取和名次计算必须来自 SQLite。
- 继续保留“空白名不 trim”“wall 失败时数据库可能已更新但 `RecordSaved` 仍为 false”的 legacy 现象。

**Verification:**
- Run: `dotnet test tests/Game2048.Tests/Game2048.Tests.csproj --filter "FullyQualifiedName~Game2048Test|FullyQualifiedName~Game2048PersistenceTest"`
- Expected: PASS（已有 `Skip` 测试仍可保持 skip，但所有非 skip 的 2048 测试应通过）

**Continue when:**
- `getLeaderboardEntries()`、`getPositionOfPlayer()`、`saveLeaderboardRecord()` 全部改为读写 SQLite。
- 测试能证明排行榜数据跨 `Game2048` 实例持久存在。
- 现有排行榜相关测试不再依赖反射清空内存字典。

**Stop and report when:**
- 为了覆盖排行榜行为不得不改变“wall 失败仍可能 500”的既定语义。
- 排行榜模型需要新增历史流水或多表关系，超出已批准设计。

- [ ] Step 1: 在 `Game2048PersistenceTest.cs` 先写失败测试，覆盖：保存排行榜即使 wall POST 抛 `HttpRequestException`，SQLite 中也已记录最佳分；排行榜与玩家名次从 SQLite 中读取。
- [ ] Step 2: 在 `Game2048Test.cs` 改写初始化/排行榜用例的测试准备逻辑，先跑一次并确认 RED。
- [ ] Step 3: 在 `Game2048.cs` 中去掉对内存字典的依赖，改为每次创建 `DbContext` 直接查询/更新 `LeaderboardEntryEntity`。
- [ ] Step 4: 保持原顺序：先 upsert SQLite 排行榜，再 wall `POST`，最后才设置 `myScoreRecorded = true`。
- [ ] Step 5: 重新运行 2048 直接测试，确认变绿；如有用，提交一个 checkpoint commit。

### Gate 3: Add global save slots, auto-save, and load/save APIs

**Goal:**
- 实现 `auto` / `slot1` / `slot2` / `slot3` 四个全局共享槽位。
- 保存/读档必须恢复棋盘、分数、输赢、`myScoreRecorded`，以及故意 bug 使用的隐藏状态字段。
- 增加 `GET /api/saves`、`POST /api/games/{id}/save/{slotKey}`、`POST /api/games/{id}/load/{slotKey}`。
- 每次 move endpoint 调用后都自动保存 `auto`，不区分是否为有效移动。

**Files:**
- Modify: `src/Game2048.Game/Game2048.cs`
- Modify: `src/Game2048.Web/Program.cs`
- Modify: `tests/Game2048.Tests/Game2048PersistenceTest.cs`
- Modify: `tests/Game2048.Tests/Game2048WebTest.cs`
- Modify: `tests/Game2048.Tests/Game2048PersistenceTestSupport.cs`

**Preconditions / Notes:**
- 这里就把用于 Bug A 的隐藏状态字段在 `Game2048` 实例和 `SavedGameEntity` 之间打通，即使 Bug A 具体行为会在 Gate 5 才启用。
- `GET /api/saves` 返回固定 4 项摘要；不要把内部隐藏状态暴露到 API 响应中。
- `auto` 只允许系统覆盖；前端只做 `Load`。

**Verification:**
- Run: `dotnet test tests/Game2048.Tests/Game2048.Tests.csproj --filter "FullyQualifiedName~manual_save_and_load_restore_board_score_and_recorded_state|FullyQualifiedName~get_saves_returns_auto_and_three_manual_slots|FullyQualifiedName~move_endpoint_creates_auto_save_after_a_keypress|FullyQualifiedName~save_and_load_endpoints_round_trip_slot_data"`
- Expected: PASS

**Continue when:**
- 直接测试能证明存档/读档恢复了 `myScoreRecorded` 和隐藏泄漏状态字段。
- Web 测试能证明 `/api/saves`、save/load endpoint、生效中的 auto-save 全部可用。
- 当前 `gameId` 内存实例和全局 SQLite 存档的交互方式已经落地。

**Stop and report when:**
- 实现过程中发现必须新增更多槽位类型或改变“全局共享一套存档”的已批准设计。
- 为了让 load/save 工作，不得不把 `Game2048` 大规模拆分成 service / repository 层。

- [ ] Step 1: 先写失败的直接测试：保存到 `slot1` 后修改当前局，再 load `slot1` 能恢复棋盘、分数、输赢、`myScoreRecorded` 与隐藏泄漏状态字段。
- [ ] Step 2: 再写失败的 Web 测试：`GET /api/saves` 返回固定四项；`POST /api/games/{id}/save/slot1` 与 `/load/slot1` 可 round-trip；`POST /api/games/{id}/move` 后 `auto` 已存在。
- [ ] Step 3: 运行上述 targeted tests，确认 RED。
- [ ] Step 4: 在 `Game2048.cs` 中实现 snapshot 序列化/反序列化、`saveGame(slotKey)`、`loadGame(slotKey)`、`getSaveSummaries()`，并把 move 后自动存档挂到 `Program.cs` 的 move endpoint 中。
- [ ] Step 5: 重新运行 targeted tests，确认变绿；如有用，提交 checkpoint commit。

### Gate 4: Wire the single-page UI to the new save/load workflow

**Goal:**
- 在现有 `index.html` 单页中加入保存/读档面板。
- 初始化、move、save、load 后都刷新存档摘要。
- 保持现有原生 JS + fetch 风格，不引入前端框架。

**Files:**
- Modify: `src/Game2048.Web/wwwroot/index.html`

**Preconditions / Notes:**
- 仅扩展现有页面，不拆多页、不引入打包工具。
- Auto 行只有 `Load`；Slot1-3 有 `Save` / `Load`。
- 不把内部隐藏状态展示到 UI 上。

**Verification:**
- Run: `dotnet build src/Game2048.Web/Game2048.Web.csproj`
- Expected: PASS
- Run: `dotnet test tests/Game2048.Tests/Game2048.Tests.csproj --filter "FullyQualifiedName~Game2048WebTest"`
- Expected: PASS（确保 API 行为未被前端改动影响）

**Continue when:**
- 页面已具备 Auto/Slot1/Slot2/Slot3 面板。
- 前端在启动、move、save、load 后都会刷新摘要。
- 没有引入新的前端构建步骤。

**Stop and report when:**
- UI 落地需要超出已批准范围的交互设计决策（例如改成多页或需要新增账号/隔离概念）。
- 前端改动迫使后端 API 协议显著偏离已批准设计。

- [ ] Step 1: 在 `index.html` 中先加静态 Save 面板骨架和消息区域（不接逻辑），本地比照现有 DOM 结构确定插入位置。
- [ ] Step 2: 接入 `GET /api/saves`、save/load 的 fetch 逻辑，并在 `ensureGame()`、`move()`、手动 save/load 后刷新摘要。
- [ ] Step 3: 保持现有 leaderboard 页切换逻辑不变，只扩展游戏页。
- [ ] Step 4: 运行 build + Web 测试确认未破坏 API 层；必要时提交 checkpoint commit。

### Gate 5: Introduce the intentional legacy bugs and keep non-spoiler characterization scaffolds

**Goal:**
- 将 3 个故意 bug 落进 `Game2048.cs`，且“偶发性”来自错误逻辑本身，而不是显式概率常量。
- 保留少量 `Skip` 的 characterization tests 作为教学脚手架，而不是主干绿测答案。

**Files:**
- Modify: `src/Game2048.Game/Game2048.cs`
- Modify: `tests/Game2048.Tests/Game2048PersistenceTest.cs`
- Create: `tests/Game2048.Tests/Game2048LegacyBehaviorTest.cs`

**Preconditions / Notes:**
- Bug A：用跨回合泄漏的“是否该新增 tile”状态实现；不要写出明显的 `x%` 常量。
- Bug B：通过空位编号映射的 `n+1` / off-by-one 错误制造偶发覆盖；不要直接随机选全盘格子。
- Bug C：通过扁平数组相邻判断漏掉行边界制造“死局不判负”。
- `Game2048LegacyBehaviorTest.cs` 在完成本 gate 时必须恢复为 `Skip` 状态，避免主干测试直接剧透。

**Verification:**
- Run: `dotnet test tests/Game2048.Tests/Game2048.Tests.csproj --filter "FullyQualifiedName~Game2048Test|FullyQualifiedName~Game2048PersistenceTest|FullyQualifiedName~Game2048WebTest"`
- Expected: PASS
- Run: `dotnet test tests/Game2048.Tests/Game2048.Tests.csproj --filter "FullyQualifiedName~Game2048LegacyBehaviorTest"`
- Expected: 仅显示 skip，不应有 failure 留在主干

**Continue when:**
- 3 个故意 bug 的实现已经进入默认运行路径。
- 直接/集成测试仍全部通过。
- `Skip` 的 characterization tests 能作为日后练习脚手架保留在仓库中。

**Stop and report when:**
- 任一 bug 只有通过显式概率常量、测试专用开关或 mock 随机源才能成立。
- 引入 bug 后导致主干非 skip 测试无法稳定通过。

- [ ] Step 1: 先在 `Game2048LegacyBehaviorTest.cs` 中写 3 个复现测试，初始不要 `Skip`，让它们表达目标“怪现象”。
- [ ] Step 2: 运行这些测试，确认当前代码对这些怪现象是 RED（当前还没故意坏到位）。
- [ ] Step 3: 在 `Game2048.cs` 中按设计落入 3 个 bug：状态泄漏导致无效移动也加 tile；空位映射 off-by-one 导致偶发覆盖；扁平相邻判断导致部分死局不判负。
- [ ] Step 4: 若存档 round-trip 需要补强隐藏字段恢复，在 `Game2048PersistenceTest.cs` 补一个失败测试并修到 PASS。
- [ ] Step 5: 在本地确认 3 个 characterization tests 复现成功后，将它们恢复为 `[Fact(Skip = "legacy practice fixture")]`，再重新跑非 skip 的 2048 测试确认全绿；必要时提交 checkpoint commit。

### Gate 6: Update runtime docs and run final verification (including browser smoke)

**Goal:**
- 更新 README，使新人只看运行信息就能启动并理解持久化/存档。
- 用自动化测试 + 浏览器 smoke 确认最终行为可用。

**Files:**
- Modify: `README.md`

**Preconditions / Notes:**
- README 只说明数据库自动创建、保存机制、存档槽位、wall `POST` 副作用；不要剧透 3 个故意 bug 的根因。
- 浏览器 smoke 必须使用 `tmux` 启动长运行 Web 服务。

**Verification:**
- Run: `dotnet test LegacyCode.sln`
- Expected: PASS（允许故意 bug 复现测试保持 skip）
- Run in tmux:
  - `tmux new-session -d -s legacy-code-csharp-2048-smoke "cd /Users/wuke/code/course-practice/legacy-code-csharp && zsh -c 'source \"$HOME/.zshrc\" && ASPNETCORE_URLS=http://127.0.0.1:5099 dotnet run --project src/Game2048.Web'"`
- Manual smoke checklist:
  - 首页可打开，`src/Game2048.Web/App_Data/game2048.db` 已生成。
  - 初始化后 `GET /api/saves` 对应 UI 显示 4 个槽位（Auto + 3 manual）。
  - 走几步后 Auto 槽出现分数/时间。
  - 手动保存到 Slot 1，再继续游戏后加载 Slot 1，棋盘和分数恢复。
  - 终局后排行榜保存路径仍会触发外部副作用；若 wall 失败，UI 行为与现有 legacy 风格一致。
  - View Records 页面能读到 SQLite 中的排行榜数据。
- Cleanup:
  - `tmux kill-session -t legacy-code-csharp-2048-smoke`

**Continue when:**
- README 已更新且未剧透故意 bug 根因。
- `dotnet test LegacyCode.sln` 全绿。
- 手工 smoke checklist 全部完成。

**Stop and report when:**
- 全量测试在一次有界修复尝试后仍失败。
- 手工 smoke 暴露数据库路径、自动迁移或 save/load 行为与批准 spec 不一致。
- 排行榜持久化、存档、或默认开箱即用运行要求之间出现无法兼容的设计冲突。

- [ ] Step 1: 更新 `README.md` 的运行说明、排行榜说明、存档说明，保持不剧透故意 bug 根因。
- [ ] Step 2: 运行 `dotnet test LegacyCode.sln`，修复本次改动引入的最后问题直到全绿。
- [ ] Step 3: 用 `tmux` 启动 Web 服务，完成手工 smoke checklist。
- [ ] Step 4: 清理 tmux session，并在任务分支上做最终 commit。

---

## Notes for the executor

- 每个 gate 都先写失败测试，再做最小实现，不要跳过 RED。
- 不要顺手把 `Game2048` 拆成 clean architecture；本任务的目标是**保留并强化 legacy 练习感**。
- 不要为了让测试好写而把 wall `POST` 或存档逻辑过度抽象成独立服务。
- 如果某个实现选择会明显削弱“开箱即用 + 代码难读 + 适合练习”的目标，优先保持 legacy 风格。
- 持久化主数据必须进 SQLite；wall `POST` 只是伴随排行榜保存的副作用，不进 SQLite。
