# POST /api/games/{id}/leaderboard 测试设计

## 被测对象

- 接口：`POST /api/games/{id}/leaderboard`
- 作用：为指定游戏实例保存排行榜成绩；该流程会先写入 SQLite `LeaderboardEntries`，再向外部 wall API 发送文本消息，只有 wall 成功后当前游戏才会被标记为 `recordSaved=true`

## 输入因子

| 因子 | 说明 | 等价类 / 边界值 |
| --- | --- | --- |
| `id` 对应游戏是否已存在 | `Program.cs` 通过 `games.GetOrAdd(id, _ => new Game2048())` 懒创建游戏 | 1. 已存在游戏 2. 不存在，需懒创建 |
| 游戏是否已结束 | `saveLeaderboardRecord` 仅允许 `win=true` 或 `lose=true` 的游戏保存成绩 | 1. 未结束 2. 已结束（`win`/`lose` 对该接口保存资格等价） |
| `scoreRecorded` | 已记录过成绩的局面不能再次保存 | 1. `false` 2. `true` |
| `playerName` | 先做 `null -> ""` 归一化，再判断 `Length == 0` | 1. 缺失 / `null` 2. 空字符串 `""` 3. 仅空白字符（如 `"   "`） 4. 普通非空名字 |
| 排行榜中是否已有同名玩家 | 通过 `PlayerName` 唯一索引查找 | 1. 不存在 2. 已存在 |
| 当前分数与已有最佳分的关系 | 仅在 `myScore > existing.BestScore` 时更新最佳分 | 1. 更高 2. 相等或更低 |
| wall API 结果 | `EnsureSuccessStatusCode()` 决定是否把当前局标记为 `recordSaved=true` | 1. 成功（2xx） 2. 失败（非 2xx / 请求失败） |

## 输出因子

| 因子 | 说明 |
| --- | --- |
| HTTP 状态码 | 参数或状态校验失败为 `400`；wall 失败时为 `500`；成功为 `200` |
| 返回的 `Game2048State` | 成功时返回当前局面；wall 失败后再次 `GET` 应看到 `recordSaved=false`、`canSaveRecord=true` |
| SQLite `LeaderboardEntries` 表 | 关注 `playerName`、`bestScore`、`updatedAtUtc` 是否新增/更新/保持不变 |
| 对当前游戏的影响 | 只有 wall 成功后 `recordSaved=true`，否则保持可再次保存 |

## 关键路径

```text
[收到 leaderboard 保存请求]
  ↓
[games.GetOrAdd(id, ...)]
  ├── id 不存在 ──→ [懒创建默认新局]
  └── id 已存在 ──→ [取出已有游戏]
                      ↓
                  [saveLeaderboardRecord(playerName)]
                      ↓
                  [null -> ""]
                      ↓
                  {playerName.Length == 0 ?}
                  ├── Y → [ArgumentException] → [400]
                  └── N
                      ↓
                  {游戏已结束?}
                  ├── N → [InvalidOperationException] → [400]
                  └── Y
                      ↓
                  {scoreRecorded?}
                  ├── Y → [InvalidOperationException] → [400]
                  └── N
                      ↓
                  [查询同名排行榜记录]
                      ↓
                  {已有记录?}
                  ├── N → [插入新行]
                  └── Y
                      ↓
                  {当前分数更高?}
                  ├── Y → [更新最佳分与时间]
                  └── N → [保留原最佳分]
                      ↓
                  [SaveChanges]
                      ↓
                  [POST wall 文本消息]
                      ↓
                  {wall 成功?}
                  ├── N → [异常冒泡，当前局仍未 recordSaved]
                  └── Y → [myScoreRecorded = true] → [200 + 返回当前状态]
```

## 用例矩阵

| 用例名 | 游戏是否存在 | 游戏是否结束 | `scoreRecorded` | `playerName` | 已有同名记录 | 分数关系 | wall 结果 | 预期 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `reject_lazy_created_game_before_game_over` | 不存在 | 未结束（懒创建默认新局） | `false` | 普通名字 | 不适用 | 不适用 | 不适用 | `400`，排行榜仍为空 |
| `reject_missing_player_name` | 已存在 | 已结束 | `false` | 缺失 / `null` | 不适用 | 不适用 | 不适用 | `400`，排行榜不写入 |
| `save_new_record_with_whitespace_only_name_when_wall_succeeds` | 已存在 | 已结束 | `false` | 仅空白字符 | 否 | 不适用 | 成功 | `200`，新增排行榜记录，当前局 `recordSaved=true` |
| `update_existing_best_score_when_current_score_is_higher` | 已存在 | 已结束 | `false` | 普通名字 | 是 | 更高 | 成功 | `200`，覆盖排行榜最佳分 |
| `keep_existing_best_score_when_current_score_is_lower` | 已存在 | 已结束 | `false` | 普通名字 | 是 | 更低 | 成功 | `200`，排行榜保持旧最佳分，但当前局仍标记已记录 |
| `reject_game_that_has_already_recorded_score` | 已存在 | 已结束 | `true` | 普通名字 | 是/否均可 | 不适用 | 不适用 | `400`，排行榜不变 |
| `persist_database_but_leave_game_unsaved_when_wall_fails` | 已存在 | 已结束 | `false` | 普通名字 | 否 | 不适用 | 失败 | `500`，排行榜已写入，但再次 `GET` 仍显示 `recordSaved=false` |

## 覆盖性检查

1. 代码路径覆盖：懒创建路径、`playerName` 校验、未终局校验、已记录校验、新增排行、更新排行、保留旧最佳分、wall 成功、wall 失败全部覆盖
2. 输入取值覆盖：`id` 的两类取值、`playerName` 的缺失/空白/普通名字、`scoreRecorded` 的两类取值、已有记录的三种后续分支（无记录/更高/更低）均已覆盖
3. 判断逻辑覆盖：
   - `playerNameToRecord.Length == 0`：覆盖 `true/false`
   - `!myWin && !myLose`：覆盖 `true/false`
   - `myScoreRecorded`：覆盖 `true/false`
   - `existingEntry == null`：覆盖 `true/false`
   - `myScore > existingEntry.BestScore`：覆盖 `true/false`
   - `response.EnsureSuccessStatusCode()`：覆盖成功与失败
