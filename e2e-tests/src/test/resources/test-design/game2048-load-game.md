# POST /api/games/{id}/load/{slotKey} 测试设计

## 被测对象

- 接口：`POST /api/games/{id}/load/{slotKey}`
- 作用：从 `SavedGames` 表中读取某个存档槽位的数据，恢复到指定 `id` 对应的内存游戏实例，并返回恢复后的 `Game2048State`

## 输入因子

| 因子 | 说明 | 等价类 / 边界值 |
| --- | --- | --- |
| `id` 对应游戏是否已存在 | `Program.cs` 通过 `games.GetOrAdd(id, _ => new Game2048())` 先获取或懒创建内存游戏 | 1. 已存在游戏 2. 不存在，需懒创建 |
| `slotKey` | `loadGame` 只接受 `auto`、`slot1`、`slot2`、`slot3` | 1. `auto` 2. 手动槽位（`slot1/slot2/slot3`，行为等价） 3. 非法槽位（如 `slot9`、大小写错误） |
| 目标槽位是否有存档 | `SavedGames.SingleOrDefault(...)` 找不到时抛 `InvalidOperationException` | 1. 有存档 2. 无存档 |
| 存档中的游戏状态 | `applySavedGame` 会原样恢复棋盘、分数、`win/lose/scoreRecorded/leakedShouldAddTile` | 1. 进行中的普通局面 2. 已终局且已记录成绩状态 |
| 当前内存游戏状态 | 仅在 `id` 已存在时有意义，用于验证 load 会覆盖旧状态 | 1. 默认新局 2. 与存档不同的旧局面 |

## 输出因子

| 因子 | 说明 |
| --- | --- |
| HTTP 状态码 | 成功为 `200`；`slotKey` 非法或槽位为空时返回 `400` |
| 返回体 | 失败时返回 `ErrorResponse.error`；成功时返回恢复后的 `Game2048State` |
| 内存中的游戏实例 | 成功时应被替换为存档状态；`id` 不存在但请求已到达时也会先被懒创建 |
| SQLite `SavedGames` 表 | load 只读，不应新增、覆盖或更新时间戳 |

## 关键路径

```text
[收到 load 请求]
  ↓
[games.GetOrAdd(id, ...)]
  ├── id 不存在 ──→ [懒创建新游戏]
  └── id 已存在 ──→ [取出已有游戏]
                      ↓
                  [loadGame(slotKey)]
                      ↓
                  {slotKey 合法?}
                  ├── N → [抛 ArgumentException] → [400 + 不改 SavedGames]
                  └── Y → [读取目标槽位]
                           ↓
                       {槽位有数据?}
                       ├── N → [抛 InvalidOperationException] → [400 + 不改 SavedGames]
                       └── Y → [applySavedGame]
                                ↓
                            [200 + 返回恢复后的 Game2048State]
```

## 用例矩阵

| 用例名 | id 是否存在 | slotKey | 目标槽位是否有存档 | 当前内存状态 | 存档状态 | 预期返回 | 预期副作用 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `reject_unknown_slot_key` | 不存在 | 非法值 `slot9` | 不适用 | 由系统懒创建的默认新局 | 不适用 | `400` + `error='Unknown save slot.'` | `SavedGames` 不变 |
| `empty_slot_returns_400_after_lazy_creation` | 不存在 | 合法空槽位 `slot3` | 否 | 由系统懒创建的默认新局 | 不适用 | `400` + `error='Save slot is empty.'` | `SavedGames` 不变；同一 `id` 后续 GET 可命中刚创建的默认新局 |
| `lazy_created_game_can_load_manual_slot` | 不存在 | 手动槽位 `slot2` | 是 | 由系统懒创建的默认新局 | 已胜利且 `scoreRecorded=true` 的终局局面 | `200`，返回终局/已记录成绩状态 | `SavedGames` 不变；内存游戏被替换为 `slot2` 内容 |
| `load_auto_slot_overwrites_existing_memory_game` | 已存在 | `auto` | 是 | 与存档不同的普通旧局面 | 进行中的普通局面 | `200`，返回 `auto` 中的状态 | `SavedGames` 不变；后续 GET 返回已加载的新状态 |

## 覆盖性检查

1. 代码路径覆盖：非法槽位失败、空槽位失败、懒创建后成功加载、覆盖已有内存游戏成功加载都已覆盖
2. 输入取值覆盖：`id` 的两类取值、`slotKey` 的 `auto` / 手动槽位 / 非法值三类取值、槽位空/非空两类取值都已覆盖
3. 判断逻辑覆盖：
   - `games.GetOrAdd(...)`：覆盖命中缓存与创建新实例
   - `normalizeSlotKey(...)`：覆盖合法与非法
   - `savedGame == null`：覆盖空槽位与有数据槽位
   - `applySavedGame(...)`：覆盖普通局面和终局且已记录成绩局面
   - load 的只读特性：通过断言 `SavedGames` 表内容与 `savedAtUtc` 保持不变来覆盖
