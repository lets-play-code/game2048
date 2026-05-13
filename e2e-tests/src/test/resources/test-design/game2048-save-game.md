# POST /api/games/{id}/save/{slotKey} 测试设计

## 被测对象

- 接口：`POST /api/games/{id}/save/{slotKey}`
- 作用：把指定游戏实例的当前状态持久化到 `SavedGames` 表中的某个存档槽位，并返回当前游戏状态

## 输入因子

| 因子 | 说明 | 等价类 / 边界值 |
| --- | --- | --- |
| `id` 对应游戏是否已存在 | `Program.cs` 通过 `games.GetOrAdd(id, _ => new Game2048())` 懒创建游戏 | 1. 已存在游戏 2. 不存在，需懒创建 |
| `slotKey` | `saveGame` 只接受 `auto`、`slot1`、`slot2`、`slot3` | 1. `auto` 2. 手动槽位（`slot1/slot2/slot3`，行为等价） 3. 非法槽位（如 `slot9`、大小写错误） |
| 目标槽位当前是否已有记录 | `saveGame` 会先查 `SavedGames.SingleOrDefault(...)`，无记录则新增，有记录则覆盖 | 1. 目标槽位为空 2. 目标槽位已有记录 |
| 当前游戏状态 | `saveGame` 原样保存棋盘、分数、`win/lose/scoreRecorded/leakedShouldAddTile` | 1. 新开局默认状态 2. 进行中的普通状态 3. 已终局且已记录成绩状态 |

## 输出因子

| 因子 | 说明 |
| --- | --- |
| HTTP 状态码 | 成功为 `200`；`slotKey` 非法时返回 `400` |
| 返回的 `Game2048State` | 成功时应与保存前当前内存游戏状态一致 |
| SQLite `SavedGames` 表 | 关注 `slotKey`、`boardJson`、`score`、`win`、`lose`、`scoreRecorded`、`leakedShouldAddTile`、`savedAtUtc` |

## 关键路径

```text
[收到 save 请求]
  ↓
[games.GetOrAdd(id, ...)]
  ├── id 不存在 ──→ [懒创建新游戏]
  └── id 已存在 ──→ [取出已有游戏]
                      ↓
                  [saveGame(slotKey)]
                      ↓
                  {slotKey 合法?}
                  ├── N → [抛 ArgumentException] → [400 + 不写入 SavedGames]
                  └── Y → [查询目标槽位]
                           ↓
                       {槽位已有记录?}
                       ├── N → [创建新行]
                       └── Y → [复用已有行]
                           ↓
                       [写入当前游戏状态]
                           ↓
                       [SaveChanges]
                           ↓
                       [200 + 返回当前 Game2048State]
```

## 用例矩阵

| 用例名 | id 是否存在 | slotKey | 目标槽位是否已有记录 | 初始游戏状态 | 预期返回 | 预期持久化 |
| --- | --- | --- | --- | --- | --- | --- |
| `reject_unknown_slot_key` | 不存在 | 非法值 `slot9` | 不适用 | 由系统懒创建的默认新局 | `400` | `SavedGames` 仍为空 |
| `lazy_create_and_save_auto_slot` | 不存在 | `auto` | 否 | 由系统懒创建的默认新局 | `200`，返回初始棋盘、`score=0`、非终局 | 新增一条 `auto` 记录，保存默认棋盘与默认状态 |
| `save_manual_slot_preserves_finished_recorded_game_state` | 已存在 | 手动槽位 `slot2` | 否 | 已胜利且 `scoreRecorded=true` 的终局局面 | `200`，返回胜利/终局/`canSaveRecord=false`/`recordSaved=true` | 新增一条 `slot2` 记录，完整保存终局标记与棋盘 |
| `overwrite_existing_slot_record` | 已存在 | 手动槽位 `slot2` | 是 | 进行中的普通局面 | `200`，返回保存前的当前状态 | 原 `slot2` 记录被覆盖为新状态，总记录数不增加，其他槽位不受影响 |

## 覆盖性检查

1. 代码路径覆盖：懒创建成功、已存在游戏成功、非法槽位失败、新建槽位、覆盖已有槽位都已覆盖
2. 输入取值覆盖：`id` 的两类取值、`slotKey` 的 `auto` / 手动槽位 / 非法槽位三类取值、槽位空/非空两类取值都已覆盖
3. 判断逻辑覆盖：
   - `games.GetOrAdd(...)`：覆盖命中缓存与创建新实例
   - `normalizeSlotKey(...)`：覆盖合法与非法
   - `savedGame == null`：覆盖创建新存档行与覆盖已有存档行
   - 状态持久化：覆盖默认状态、进行中状态、终局且已记录成绩状态
