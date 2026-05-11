# 2048 Legacy Practice Expansion Design

## 背景

当前 `Game2048` 项目已经具备明显的 legacy 练习特征，但仍然偏轻量：

- 排行榜只保存在内存中
- 没有游戏存档能力
- 已有坏味道主要集中在命名、状态暴露、UI/逻辑混杂
- 某些现有问题更多是“外部依赖脆弱”，而不是适合新人逐步分析的业务/边界 bug

本次设计的目标，是让 2048 更适合作为编码练习项目：

1. 默认引入若干**源于错误逻辑本身**的偶发 bug，供分析、补测试、调试与重构练习
2. 将排行榜持久化到 SQLite，但仍保留保存时的外部 HTTP 副作用
3. 增加游戏存取档功能，并持久化到 SQLite
4. 保证新人除了安装 `.NET` 之外，无需额外数据库服务或手工初始化步骤即可运行项目

---

## 已确认的产品决策

### 练习模式与 bug
- 人为引入的 bug **默认始终启用**
- bug 的“偶发性”必须来自**错误的状态、边界或数据流逻辑**
- 不允许通过明显的概率常量（例如 `if (Random < 0.1)`）来制造练习题

### 持久化技术
- 数据库使用 **SQLite**
- 代码层使用 **EF Core + SQLite Provider**
- 不直接手写 SQL 访问 SQLite
- migration 文件需要提交进仓库
- 应用启动时自动执行 `Database.Migrate()`

### 排行榜
- 排行榜主数据改为 SQLite 持久化
- 保存排行榜时，继续保留外部副作用：
  - `POST http://7k7k6666.com/api/wall`
- 外部 wall `POST` 本身**不写入 SQLite**

### 存档
- 提供 **1 个自动存档** + **3 个手动槽位**
- 手动槽位为固定槽：`slot1` / `slot2` / `slot3`
- 自动存档为固定槽：`auto`
- 存档范围是**应用全局唯一一套**，不是按 `gameId` 或浏览器实例隔离
- 自动存档触发时机为：**每次按方向键后都自动保存**，不区分该次移动是否有效
- 读档后必须恢复“这局是否已经保存过排行榜记录”的状态

### 架构风格
- 采用**方案 A：最小拆分、最大混杂**
- `Game2048` 继续承担主要职责，不进行 clean architecture / repository / service 分层
- 本次改动的许多“缺点”本身就是练习项目的目标特性

---

## 总体架构设计

### 设计原则
本次改造刻意维持 `Game2048` 作为 God Class / Ball of Mud 的方向：

- 新功能继续往 `Game2048` 类中添加
- 不引入 repository 层
- 不引入 application service 层
- 不把外部 HTTP wall 调用抽象成独立基础设施服务
- 不把游戏状态和持久化状态优雅分离

这样做的目的不是追求整洁，而是为了保留并强化以下练习价值：

- 业务逻辑、状态机、副作用、数据库读写高度耦合
- 新人需要实际读代码，而不是只看整洁接口
- 更适合做补测试、行为刻画、调试、重构和职责拆分练习

### 组件边界

#### `src/Game2048.Game/Game2048.cs`
继续集中负责：

- 棋盘状态
- 移动/合并
- 输赢判断
- 排行榜保存
- EF Core 数据持久化调用
- 自动存档 / 手动存档
- 读档恢复
- 外部 wall `POST`
- 为故意 bug 新增的隐藏内部状态

#### `src/Game2048.Game/*`
在同一个类库中新增：

- `Game2048DbContext`
- 排行榜实体
- 存档实体
- EF Core migrations

这些新增类型是为了让 EF Core 可用，不代表架构上进行职责净化。

#### `src/Game2048.Web/Program.cs`
继续保持薄 API 层，仅负责：

- 计算数据库路径
- 配置并初始化持久化
- 调用 `Database.Migrate()`
- 暴露存档/读档相关 endpoint
- 将 Web 请求直接转发给 `Game2048`

#### `src/Game2048.Web/wwwroot/index.html`
继续保持单页、原生 JS、显示/隐藏切换的风格，只做最小必要扩展：

- 新增全局存档面板
- 展示 `auto` / `slot1` / `slot2` / `slot3`
- 提供 Save / Load 按钮
- 继续复用现有游戏页 + 排行榜页切换方式

---

## 持久化设计

### 技术选型
- ORM：`Microsoft.EntityFrameworkCore`
- Provider：`Microsoft.EntityFrameworkCore.Sqlite`
- Design-time package：`Microsoft.EntityFrameworkCore.Design`

### 数据库文件位置
默认数据库文件位于 Web 内容根目录下的固定位置：

- `App_Data/game2048.db`

在当前项目的默认运行方式下，其实际位置为：

- `src/Game2048.Web/App_Data/game2048.db`

要求：

- 应用启动时自动创建 `App_Data/`
- 不依赖外部数据库服务
- 新人执行 `dotnet run --project src/Game2048.Web` 即可启动并自动生成数据库

### 初始化方式
推荐在 `Program.cs` 启动时进行：

1. 计算数据库文件路径
2. 创建 `App_Data/` 目录（如不存在）
3. 调用类似 `Game2048.ConfigurePersistence(databasePath)` 的静态入口配置数据库路径
4. 调用类似 `Game2048.EnsureDatabaseReady()` 的静态入口执行 `Database.Migrate()`
5. 再开始接收请求

关键点：
- 不将初始化逻辑抽成整洁服务
- `Game2048` 内部直接知道如何创建 `DbContext`
- 持久化配置和领域逻辑继续紧密耦合

### Migration 策略
- migration 文件提交到仓库
- 存放于 `src/Game2048.Game/Migrations/`
- 启动时自动执行 `Database.Migrate()`
- 不要求新人手工执行 `dotnet ef database update`

---

## 数据模型设计

### 排行榜表
建议实体名：`LeaderboardEntryEntity`

用途：
- 保存每个玩家当前最高分

字段：
- `Id`
- `PlayerName`
- `BestScore`
- `UpdatedAtUtc`

约束：
- `PlayerName` 建唯一索引

行为语义：
- 同一玩家只保留一条记录
- 新成绩高于旧成绩时更新 `BestScore`
- 新成绩不高于旧成绩时保留旧值

说明：
- 本次不引入历史流水表
- 维持与当前 `Game2048` 排行榜行为相近的“按玩家保留最高分”模型

### 存档表
建议实体名：`SavedGameEntity`

用途：
- 保存全局共享的一套固定槽位存档

字段：
- `Id`
- `SlotKey`
- `BoardJson`
- `Score`
- `Win`
- `Lose`
- `ScoreRecorded`
- `SavedAtUtc`
- 与故意 bug 相关的隐藏状态字段

约束：
- `SlotKey` 建唯一索引
- 允许值限定为：
  - `auto`
  - `slot1`
  - `slot2`
  - `slot3`

### 棋盘存储方式
棋盘不拆成子表，直接保存到文本字段 `BoardJson` 中，例如：

```json
["2","","4","","","","","","2","","","","","","",""]
```

原因：
- 结构简单
- 实现直接
- 符合 legacy 项目“先能跑再说”的风格
- 方便 `Game2048` 自己序列化/反序列化

### 隐藏状态持久化要求
存档不仅保存“表面状态”，还必须保存“足以恢复后续行为”的内部状态，包括：

- `myScoreRecorded`
- 用于制造“无效移动也新增 tile”的泄漏标记
- 未来如新增其他影响后续移动结果的内部字段，也应进入存档

理由：
- 读档后要恢复真实的继续游戏状态
- 不能让读档过程意外“修复”掉 bug
- 练习者需要面对真实、连续的状态机，而不是一次性棋盘快照

---

## 运行态与持久化态的关系

### 当前游戏实例
继续保留当前机制：
- Web 层仍维护 `gameId -> Game2048` 的内存映射

### 长期状态
持久化到 SQLite 的仅包括：
- 排行榜
- 全局存档槽位

### 交互方式
- 玩家实际操作的是当前内存中的 `Game2048` 实例
- 手动保存或自动保存时，将当前实例状态写入数据库
- 读档时，从数据库取出槽位内容，覆盖回当前 `gameId` 对应的 `Game2048` 实例

这样会同时存在三类状态：
- 内存中的当前局状态
- SQLite 中的持久化状态
- 保存排行榜时的外部 HTTP 副作用状态

这种状态并存与交织是本练习项目的刻意特征，而不是待立即修复的问题。

---

## 排行榜保存流程设计

### 目标行为
排行榜主数据写入 SQLite，但继续保留外部 wall `POST` 作为保存动作的一部分。

### 流程
前端在终局后调用：
- `POST /api/games/{id}/leaderboard`

`Game2048.saveLeaderboardRecord(playerName)` 内部顺序设计为：

1. 校验 `playerName`
2. 校验当前是否已终局
3. 校验该局是否尚未记录过分数
4. 使用 EF Core 查询玩家记录
5. 按“保留最高分”规则插入或更新 SQLite 中的排行榜数据
6. 发起外部副作用：
   - `POST http://7k7k6666.com/api/wall`
7. 仅在外部 wall 返回成功后，才设置：
   - `myScoreRecorded = true`

### 刻意保留的 legacy 行为
以下行为本次保留：

- 仅拦截空字符串，不 `Trim()` 空白名
- 先写 SQLite，再调用外部 wall
- 外部 wall 调用失败时，数据库可能已更新，但 `myScoreRecorded` 仍为 `false`
- `Program.cs` 仍只捕获：
  - `ArgumentException`
  - `InvalidOperationException`
- 因此 wall 请求失败时，接口仍可能直接冒成 500

这些不一致是有意保留的练习材料，不在本次改造中顺手修复。

---

## 存档流程设计

### 槽位定义
固定全局槽位：
- `auto`
- `slot1`
- `slot2`
- `slot3`

其中：
- `auto`：系统自动覆盖，前端仅提供 `Load`
- `slot1` / `slot2` / `slot3`：前端提供 `Save` 和 `Load`

### 自动存档
自动存档绑定在每次方向键请求之后执行。

流程：
1. 前端调用 `POST /api/games/{id}/move`
2. `Program.cs` 中先调用 `game2048.keyPressed(direction)`
3. 无论本次移动是否真的改变棋盘
4. 都调用 `game2048.saveGame("auto")`
5. 返回最新 `Game2048State`

结果：
- 无效移动也会刷新自动存档
- 终局后继续按方向键也会刷新自动存档
- 自动存档刷新绑定在 move 请求之后，而不是绑定在“有效移动之后”

这属于有意保留的副作用绑定不当。

### 手动存档
前端提供：
- `POST /api/games/{id}/save/slot1`
- `POST /api/games/{id}/save/slot2`
- `POST /api/games/{id}/save/slot3`

行为要求：
- 任意时刻可存档
- 直接覆盖对应槽位
- 保存内容包括：
  - 棋盘
  - 分数
  - `Win`
  - `Lose`
  - `myScoreRecorded`
  - 与故意 bug 相关的隐藏内部状态

### 读档
前端提供：
- `POST /api/games/{id}/load/auto`
- `POST /api/games/{id}/load/slot1`
- `POST /api/games/{id}/load/slot2`
- `POST /api/games/{id}/load/slot3`

行为要求：
- 从数据库读取槽位
- 将槽位数据覆盖回当前 `gameId` 对应的 `Game2048` 实例
- 返回最新 `Game2048State`

特别要求：
- 读档后必须恢复 `myScoreRecorded`
- 已保存过排行榜的局，读档回来后仍然不得再次保存排行榜
- 读档后也必须恢复与故意 bug 相关的隐藏状态，不能把 bug 状态“洗掉”

### 存档摘要接口
新增：
- `GET /api/saves`

返回固定 4 项摘要，顺序固定：
1. `auto`
2. `slot1`
3. `slot2`
4. `slot3`

每项建议包含：
- `slotKey`
- `hasData`
- `score`
- `savedAtUtc`

不建议返回太多内部状态，以避免前端把练习答案直接展示出来。

---

## 故意引入的 bug 设计

### Bug A：无效移动时仍然新增数字

#### 目标现象
玩家执行一次本应无效的移动后，棋盘上仍可能新增数字。

#### 设计方式
不是显式概率，而是通过**跨回合泄漏的“是否该新增 tile”状态**实现：

- 当前实现中，“这次移动是否需要新增 tile”本应是一次 move 内部的临时判断
- 设计上将其变为 `Game2048` 的实例字段
- 在某些路径下，该状态不会被正确清理或重置
- 某次有效移动后遗留下来的状态，可能影响下一次本应无效的移动
- 从而导致无效移动也触发 `addTile()`

#### 练习价值
- 问题与历史状态相关，不是单步输入即可完全解释
- 需要追踪实例字段生命周期
- 适合做状态泄漏、命令副作用、最小复现与 characterization test 练习

### Bug B：新增数字时偶发覆盖已填充格子

#### 目标现象
`addTile()` 大多数时候正常，但少数情况下会把新数字写到一个已经有值的格子上。

#### 设计方式
通过**空位编号映射的 off-by-one 错误**实现：

- 仍然保留“从空位里随机选一个”的表面结构
- 先统计空位数量 `n`
- 再错误地生成 `0..n` 的序号，而不是合法的 `0..n-1`
- 后续通过扫描棋盘把“空位序号”映射回真实格子时，边界值会落入错误位置
- 错误位置有时是空格，有时是已填充格子

#### 练习价值
- 偶发性来自真实边界错误，而非显式概率常量
- 需要理解空位计数、随机范围、序号到格子的映射过程
- 非常适合新人练习边界复现与调试

### Bug C：某些死局不会判负

#### 目标现象
某些满盘且已经无法移动的局面，不会触发失败，玩家会卡在“推不动但不结束”的状态。

#### 设计方式
通过**扁平数组相邻判断漏掉行边界**实现：

- 将 `canMove()` 写成基于一维数组的简单相邻扫描
- 判断 `myTiles[i]` 与 `myTiles[i + 1]` 是否相等
- 但忘记处理每行末尾和下一行开头并不真正相邻的问题
- 于是当索引 `3 == 4`、`7 == 8`、`11 == 12` 等跨行位置碰巧相等时，会误判成“还能移动”

#### 练习价值
- 必须真正理解二维棋盘与一维存储的关系
- 很像真实遗留代码中常见的边界误判
- 适合做死局构造、状态验证与回溯分析练习

---

## API 设计

### 保留的现有接口
- `GET /api/leaderboard`
- `POST /api/games`
- `GET /api/games/{id}`
- `POST /api/games/{id}/move`
- `POST /api/games/{id}/reset`
- `POST /api/games/{id}/leaderboard`

### 新增接口
- `GET /api/saves`
- `POST /api/games/{id}/save/{slotKey}`
- `POST /api/games/{id}/load/{slotKey}`

### 接口风格
维持当前项目的简单风格：
- endpoint 直接操作 `Game2048` 实例
- 不增加复杂 DTO 体系
- 不为 save/load 单独引入服务层
- 不为了响应优雅而重构整个 API 协议

---

## 前端页面设计

### 保持单页风格
继续使用：
- 单个 `index.html`
- 原生 JavaScript
- `fetch` + DOM 更新
- 游戏页与记录页显示/隐藏切换

### 新增存档面板
在游戏页中新增 `Saves` 区域，显示 4 行：

- Auto
  - 摘要：分数 / 保存时间 / 是否为空
  - 按钮：`Load`
- Slot 1
  - 按钮：`Save` / `Load`
- Slot 2
  - 按钮：`Save` / `Load`
- Slot 3
  - 按钮：`Save` / `Load`

### 前端刷新策略
保持简单直接，不做复杂优化：

- 页面初始化时：
  - `ensureGame()`
  - `GET /api/saves`
- 每次 `move()` 后：
  - 渲染返回的 `Game2048State`
  - 再调用 `GET /api/saves` 刷新 auto
- 每次手动保存/读档后：
  - 调用 `GET /api/saves`
- 继续保留 `View Records` 的现有切换模式

### 页面消息提示
新增简单的存档提示文本，例如：
- `Saved to Slot 1`
- `Loaded Auto Save`
- `Slot 2 is empty`

不建议把内部隐藏状态直接显示到 UI 上。

---

## 测试策略

### 稳定功能需要进入通过测试
以下内容应纳入主干绿色测试：

#### 核心逻辑测试
建议在 `tests/Game2048.Tests/` 中扩展测试，覆盖：

1. 数据库初始化后可正常创建游戏
2. 排行榜写入 SQLite 并按最高分更新
3. SQLite 排行榜排序与名次规则正确
4. 手动保存到 `slot1/2/3` 后可读回
5. 自动存档会在每次 move 请求后刷新
6. 读档会恢复：
   - 棋盘
   - 分数
   - `Win`
   - `Lose`
   - `myScoreRecorded`
   - 故意 bug 相关的隐藏状态字段
7. 全局存档对新的 `Game2048` 实例可见

#### Web/API 集成测试
建议参照现有 Gilded Rose Web 测试风格，增加 2048 Web 集成测试，覆盖：

1. 启动时自动迁移数据库
2. `POST /api/games/{id}/move` 后 auto save 可通过 `GET /api/saves` 看到
3. `POST /api/games/{id}/save/slot1` 能覆盖 slot1
4. `POST /api/games/{id}/load/slot1` 能恢复当前游戏状态
5. `GET /api/leaderboard` 返回 SQLite 中的数据，而非进程内存数据

### 故意 bug 的测试策略
故意 bug 不应被主干绿测完全“剧透”。

建议：
- 不把 3 个故意 bug 的根因完整编码进常规通过测试
- 可保留少量 `Skip` 的 characterization tests 作为历史/教学脚手架
- 这些测试可以描述怪现象，但不直接把根因讲穿

这样可以兼顾：
- 可维护性
- 教学复现能力
- 不让练习者一跑测试就直接看到答案

### 外部 wall `POST` 的测试策略
保留当前 legacy 特征：
- 不将 wall `POST` 抽成干净可替换服务
- 默认通过测试不依赖真实外网成功

测试关注重点应放在：
- 前置校验
- SQLite 写入结果
- 读档恢复结果

对于 wall 失败导致的“数据库已写但当前局未标记已保存”“接口可能 500”等行为，可保留为：
- 跳过测试
- legacy 特征测试
- 或仅在需要时局部验证

---

## README 与长期文档策略

### README 应补充的信息
README 需要说明：
- 项目会自动创建 SQLite 数据库
- 数据库文件的默认位置
- 有 1 个自动存档 + 3 个手动槽位
- 排行榜现在是持久化的
- 保存排行榜时仍带有外部 wall `POST` 副作用

### README 不应剧透的信息
README 不应写出 3 个故意 bug 的具体根因。

最多只应描述为：
- 项目故意保留/引入若干 legacy 行为异常，供调试与重构练习使用

这样既保证项目可运行、可理解，又不破坏练习体验。

---

## 非目标

本次设计明确**不**追求以下目标：

- 不做 clean architecture
- 不引入 repository / service / mapper 分层
- 不消除 `Game2048` 的 God Class 特征
- 不修复排行榜保存中的部分成功/部分失败一致性问题
- 不修复 wall 请求失败时的 500 行为
- 不把前端响应整合成更优雅的协议
- 不把故意 bug 做成可配置概率开关

这些“缺点”是练习项目的一部分，而不是本次工作的遗漏。

---

## 设计总结

本次改造将把 2048 从“带少量坏味道的练习项目”，扩展为一个更适合编码训练的 legacy playground：

- 继续保留 God Class 风格
- 用 EF Core + SQLite 提供开箱即用的持久化
- 将排行榜与存档能力真正落地
- 同时通过状态泄漏、边界错误、邻接误判等方式注入更有分析价值的 bug
- 保留数据库、内存状态、外部副作用之间的复杂耦合

最终结果不是“更整洁”，而是“更适合练习”。
