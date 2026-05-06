# Game2048 安全网测试与三模块重构设计

> 日期：2026-05-06  
> 状态：已与用户确认的设计草案  
> 适用范围：`src/Game2048.Game/Game2048.cs` 及 `tests/Game2048.Tests`

## 1. 背景

当前 `src/Game2048.Game/Game2048.cs` 同时承担了以下职责：

- 2048 棋盘规则与移动合并
- 渲染状态组装（`paint()` / `drawTile()`）
- 排行榜内存存储与排序
- 终局后保存成绩
- 对外 HTTP 墙消息发送
- 随机数驱动的新方块生成

这导致几个直接问题：

1. **不可预测**：随机数让完整流程测试不稳定。
2. **不可隔离**：保存成绩时会访问真实 HTTP 地址，测试脆弱且可能失败。
3. **高耦合**：规则、展示、排行榜混在一个类中，难以重构。
4. **缺少安全网**：没有类似 Gilded Rose 的 approval 风格长流程保护测试。

## 2. 本次任务目标

严格按以下顺序执行：

1. 通过抽取 `override` seam 的方式，在测试中接管 `random` 和 HTTP 请求，让被测代码在测试中完全确定且不出错。
2. 参考 `tests/GildedRose.Tests/TestFixture.cs` 与 `ApprovalTest.cs` 的思路，为 2048 搭建 approval 风格安全网测试；要求完整运行一次游戏流程，并让整体测试对 `Game2048` 达到 **90% 以上分支覆盖率**。
3. 在安全网通过后，再进行模块重构。
4. 将 `Game2048.cs` 改为 3 个模块：
   - 游戏核心规则
   - 游戏展示
   - 排行榜记录

额外已确认边界：

- **保持现有对外接口兼容**：`Game2048` 继续作为 façade，对外 public API 尽量不变；`src/Game2048.Web/Program.cs` 的 HTTP 接口保持兼容。
- **现有 `tests/Game2048.Tests/Game2048Test.cs` 可直接废弃并重写**。
- **安全网测试形式采用多个 approval 场景组合**，而不是一个超长单测试。

## 3. 方案对比与选型

### 方案 1：只做最小 override seam 后直接拆模块

优点：
- 与遗留代码最贴近
- 修改路径直接

缺点：
- 容易把“安全网建立”和“结构重构”混在一起
- approval 场景容易失焦

### 方案 2：直接引入接口注入（IRandom / IHttp / ILeaderboard）

优点：
- 最终结构更现代
- 依赖更显式

缺点：
- 不符合本任务明确要求的“先通过 override 方式处理”
- 对遗留代码第一步改动过大

### 方案 3：两阶段混合方案（采纳）

第一阶段：
- 仅在 `Game2048` 上抽出少量 `protected virtual` seam，专门处理随机数与外部 HTTP
- 先建立 approval 风格安全网测试

第二阶段：
- 在安全网保护下，将内部实现拆分为 3 个模块
- `Game2048` 保留 façade 与现有对外 API

**结论：采纳方案 3。**

## 4. 设计原则

### 4.1 保持 façade 兼容

`Game2048` 继续保留现有 public API，包括：

- `resetGame()`
- `left()` / `right()` / `up()` / `down()`
- `keyPressed(string keyCode)`
- `saveLeaderboardRecord(string playerName)`
- `getGameState()` / `paint()`
- `getLeaderboardEntries()` / `getPositionOfPlayer()`

`Game2048` 的定位从“所有逻辑都在这里”调整为“兼容 façade / 协调层”。

### 4.2 先做 seam，再做安全网，再做重构

不能先拆模块再补测试。必须先把随机数和 HTTP 副作用变为可控，再用 approval 风格锁住行为，最后才允许做结构性重构。

### 4.3 模块边界优先于代码搬运

本次拆分不是为了“把几百行拆成几个文件”，而是为了把不同变化原因分开：

- 规则变化 -> 只影响核心规则模块
- 展示变化 -> 只影响 presenter
- 排行榜变化 -> 只影响 leaderboard 模块

### 4.4 approval 测试保护外部行为

approval 测试的作用是保护长流程可观察行为，而不是测试每个内部实现细节。

## 5. 第一阶段：override seam 设计

为满足“通过抽取 override 方式在测试中处理 random 和 HTTP 请求代码”的要求，在 `Game2048` façade 上引入两个受保护可覆盖入口。

### 5.1 随机数 seam

建议形式：

- `protected virtual double NextRandomDouble()`

用途：

- 替代 `addTile()` 中直接使用的 `Random.NextDouble()`
- 在测试子类中返回预设随机序列，从而稳定控制：
  - 落子位置
  - 新方块是 `2` 还是 `4`

### 5.2 外部 HTTP seam

建议形式：

- `protected virtual void PostToWall(string message)`

用途：

- 替代 `saveLeaderboardRecord()` 中直接 new `HttpClient` 并发送请求的行为
- 在测试子类中改为仅记录消息，不发真实网络请求

### 5.3 seam 位置选择理由

将 seam 放在 `Game2048` façade，而不是散落到后续模块内部，原因如下：

- 符合题目要求的 override 方式
- 对现有外部接口侵入最小
- 便于先建安全网再重构
- 后续排行榜模块仍可通过 façade 提供的委托/回调走到同一个 seam

## 6. 安全网测试设计

测试整体风格仿照 `tests/GildedRose.Tests/TestFixture.cs` 与 `ApprovalTest.cs`：

- 用 fixture 生成稳定文本快照
- 用 approval 文件对比锁定行为
- 不参考当前 `Game2048Test.cs` 的断言设计

### 6.1 测试文件结构

建议结构：

```text
tests/Game2048.Tests/
├── ApprovalTest.cs
├── Game2048ApprovalFixture.cs
├── ApprovalFiles/
│   ├── FullJourney.approved.txt
│   ├── WinJourney.approved.txt
│   └── LeaderboardJourney.approved.txt
└── TestDoubles/
    └── DeterministicGame2048.cs
```

职责划分：

- `DeterministicGame2048`：继承 `Game2048`，override 随机数与 HTTP
- `Game2048ApprovalFixture`：组织完整游戏脚本，输出稳定文本
- `ApprovalTest`：读取 approved 文件并比较

### 6.2 approval 输出内容

approval 文本不是只打印棋盘，而是打印：

- 当前步骤名
- 棋盘状态
- score / win / lose / canMove / canSaveRecord / recordSaved
- 展示状态（overlay、messages、scoreText）
- 已发送的 wall message
- 当前 leaderboard 结果

这样可以同时保护：

- 核心规则行为
- 展示状态映射
- 排行榜业务输出

### 6.3 approval 场景划分

#### 场景一：`FullJourney.approved.txt`

主流程安全网，覆盖：

- 新开一局
- 四方向移动至少都执行过一次
- 至少出现一次“有效移动才新增 tile”行为
- 进入 lose 终局
- 保存成绩成功
- 查看 leaderboard
- `ESC` 重开

#### 场景二：`WinJourney.approved.txt`

补 win 分支，覆盖：

- 通过确定性局面合并出 `2048`
- `myWin = true`
- win overlay 与消息
- 保存一次成功
- 再次保存被拒绝

#### 场景三：`LeaderboardJourney.approved.txt`

补排行榜规则，覆盖：

- 同一玩家多局只保留最高分
- 多玩家同分并列
- 名次跳号
- 未上榜玩家名次返回 `entries.Count + 1`

### 6.4 覆盖率达标策略

approval 测试负责长流程保护；再辅以少量精确断言测试补齐 guard clauses 与边界分支，例如：

- 游戏未结束时保存成绩 -> `InvalidOperationException`
- `playerName == null` / `""` -> `ArgumentException`
- 同一局重复保存 -> `InvalidOperationException`
- `getPositionOfPlayer()` 未命中玩家分支
- `paint()` 在 normal / win / lose 的差异

### 6.5 覆盖率验证

在 `tests/Game2048.Tests/Game2048.Tests.csproj` 中加入覆盖率工具，并将 branch coverage 作为硬门槛。

参考验证命令：

```bash
dotnet test tests/Game2048.Tests/Game2048.Tests.csproj \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=json \
  /p:Threshold=90 \
  /p:ThresholdType=branch \
  /p:ThresholdStat=total
```

## 7. 三模块重构设计

最终将 `Game2048.cs` 中的实现拆分为 3 个模块，并保留 `Game2048` 作为 façade。

### 7.1 目标结构

```text
src/Game2048.Game/
├── Game2048.cs                     # façade，保留现有对外 API
├── Core/
│   ├── GameBoard.cs                # 游戏核心规则
│   └── BoardTile.cs                # 核心 tile 值对象
├── Presentation/
│   └── GameStatePresenter.cs       # 游戏展示
├── Leaderboard/
│   ├── LeaderboardService.cs       # 排行榜记录规则
│   └── LeaderboardStore.cs         # 内存榜单存储
└── Models/
    ├── Game2048State.cs
    ├── TileState.cs
    └── LeaderboardEntry.cs
```

如果实施中发现保持模型类型原位更利于兼容，则允许只拆职责文件，不强制移动所有模型文件，但职责边界必须成立。

### 7.2 模块关系

```text
                 ┌──────────────────────┐
                 │      Game2048        │
                 │   façade / 协调层     │
                 └─────────┬────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
        ▼                  ▼                  ▼
┌──────────────┐   ┌────────────────┐   ┌──────────────────┐
│ GameBoard    │   │ GameState      │   │ Leaderboard      │
│ 核心规则模块 │   │ Presenter      │   │ Service          │
└──────────────┘   │ 展示模块       │   │ 排行榜模块       │
                   └────────────────┘   └──────────────────┘
                                              │
                                              ▼
                                   ┌──────────────────────┐
                                   │ façade override seam │
                                   │ random / HTTP        │
                                   └──────────────────────┘
```

### 7.3 模块一：游戏核心规则

职责：

- 棋盘状态
- 初始化与 reset
- 四方向移动
- 行压缩与合并
- 计分
- 胜负判断
- 是否可移动
- 新 tile 生成（通过随机回调）

不负责：

- `Game2048State` 生成
- leaderboard
- HTTP
- UI 文案、颜色、坐标

测试边界：

- 仅验证输入棋盘 + 操作 -> 输出棋盘、分数、胜负、可移动状态
- 不验证展示对象
- 不验证排行榜或网络

### 7.4 模块二：游戏展示

职责：

- 将当前游戏状态转换为 `Game2048State`
- 组装 tile 的颜色、前景、字体、坐标偏移
- 组装 overlay、messages、scoreText
- 计算 `CanSaveRecord` / `RecordSaved`

不负责：

- 移动棋盘
- 排序 leaderboard
- 发送 HTTP

测试边界：

- 给定规则层状态，验证输出的 `Game2048State`
- 重点覆盖 normal / win / lose 三种呈现
- 验证 `ScoreTextDrawCount`、tile 坐标、颜色和消息

### 7.5 模块三：排行榜记录

职责：

- 终局后才能保存
- 同一局只能保存一次
- 玩家名合法性校验
- 每个玩家只保留最高分
- 排名与并列名次计算
- 生成 wall message
- 通过外部 publisher 回调发送消息

不负责：

- 棋盘移动
- UI state 组装
- 具体 HTTP 客户端实现

测试边界：

- 只验证保存规则、排序、排名、消息发布条件
- 不验证棋盘规则
- 不验证 UI 呈现

### 7.6 排行榜模块与 seam 的连接方式

排行榜模块不应直接 `new HttpClient()`，而应接收一个 publisher 回调，例如概念上：

- `Action<string> publishWallMessage`

由 `Game2048` 在调用排行榜模块时把自己的 `PostToWall(message)` 传入。这样：

- 排行榜模块不依赖网络实现
- 测试时可传 fake publisher
- override seam 仍然集中在 façade

## 8. `Game2048` façade 的最终职责

重构后 `Game2048` 只负责：

- 持有和协调 `GameBoard`
- 持有和协调排行榜模块
- 在 `paint()` / `getGameState()` 时调用 presenter
- 在 `saveLeaderboardRecord()` 时把当前游戏结束状态与分数委托给排行榜模块
- 暴露兼容 public API
- 暴露测试可覆盖的随机数 / HTTP seam

## 9. 模块依赖约束

必须保持以下依赖方向：

```text
Game2048 (façade)
  ├─ depends on GameBoard
  ├─ depends on GameStatePresenter
  └─ depends on LeaderboardService

GameStatePresenter
  └─ depends on board snapshot / game flags / record flags

LeaderboardService
  └─ depends on score + game-over flag + publisher callback

GameBoard
  └─ depends on random-number callback only
```

不允许：

- Presenter 调 leaderboard
- LeaderboardService 调 presenter
- GameBoard 调 HttpClient
- GameBoard 直接返回 Web view model

## 10. 实施顺序与门槛

严格按以下顺序：

```text
阶段 1：抽 override seam，隔离 random / HTTP
   ↓
阶段 2：建立 approval 风格安全网，并达到 90%+ branch coverage
   ↓
阶段 3：在安全网保护下重构为 3 个模块
   ↓
阶段 4：补齐模块边界测试并完成最终验证
```

### 阶段 1 完成标准

- `Game2048` 支持通过 override 接管随机与 wall 发布
- 不发生真实 HTTP
- 不改变现有 public API

### 阶段 2 完成标准

- approval 测试稳定通过
- 至少包含 `FullJourney` / `WinJourney` / `LeaderboardJourney`
- `Game2048` 分支覆盖率达到 90%+

### 阶段 3 完成标准

- `Game2048.cs` 不再承载全部实现逻辑
- 已拆成规则 / 展示 / 排行榜三个模块
- façade 行为保持兼容
- approval 测试持续通过

### 阶段 4 完成标准

- 每个模块都有明确边界测试
- 全部测试通过
- 覆盖率仍满足门槛

## 11. 预期影响文件

### 生产代码

- 修改：`src/Game2048.Game/Game2048.cs`
- 新增：`src/Game2048.Game/Core/...`
- 新增：`src/Game2048.Game/Presentation/...`
- 新增：`src/Game2048.Game/Leaderboard/...`
- 视实现需要调整模型文件位置

### 测试代码

- 删除或重写：`tests/Game2048.Tests/Game2048Test.cs`
- 新增：`tests/Game2048.Tests/ApprovalTest.cs`
- 新增：`tests/Game2048.Tests/Game2048ApprovalFixture.cs`
- 新增：`tests/Game2048.Tests/ApprovalFiles/...`
- 新增：`tests/Game2048.Tests/TestDoubles/...`
- 新增：`tests/Game2048.Tests/Core/...`
- 新增：`tests/Game2048.Tests/Presentation/...`
- 新增：`tests/Game2048.Tests/Leaderboard/...`
- 修改：`tests/Game2048.Tests/Game2048.Tests.csproj`

## 12. 风险与应对

### 风险 1：approval 文本过于脆弱

应对：

- 只输出稳定且有行为意义的数据
- 统一换行
- 固定排序
- 固定随机序列

### 风险 2：重构中破坏 façade 兼容

应对：

- approval 测试持续保护完整流程
- 补 façade 兼容断言
- 尽量不改 `Program.cs`

### 风险 3：覆盖率达标但模块边界仍模糊

应对：

- approval 只做安全网
- 每个模块单独建立边界测试
- 测试命名直接体现职责

## 13. 完成后的结果

完成后应得到：

1. 一个可控、无真实网络副作用的 2048 测试基座
2. 一套 approval 风格的完整流程安全网
3. `Game2048` 相关代码 90%+ 的分支覆盖率门槛
4. 三个松耦合模块：
   - 游戏核心规则
   - 游戏展示
   - 排行榜记录
5. 每个模块都有明确的边界测试
6. `Game2048` 继续作为兼容 façade 被 Web 层使用
