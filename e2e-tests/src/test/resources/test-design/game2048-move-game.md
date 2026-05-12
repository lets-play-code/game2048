# POST /api/games/{id}/move 测试设计

## 被测对象

- 接口：`POST /api/games/{id}/move`
- 作用：按方向驱动某局 2048 游戏，并在每次请求后刷新 `auto` 存档

## 输入因子

| 因子 | 说明 | 等价类 / 边界值 |
| --- | --- | --- |
| `id` 对应游戏是否已存在 | `Program.cs` 通过 `games.GetOrAdd(id, _ => new Game2048())` 懒创建游戏 | 1. 已存在游戏 2. 不存在，需懒创建 |
| `direction` | `keyPressed` 对 `escape` 有单独的 reset 分支；`left/right/up/down` 会分别命中各自的 switch 分支；其他值走 legacy no-op | 1. 重置方向：`escape` 2. 合法移动方向：`left` 3. 合法移动方向：`right` 4. 合法移动方向：`up` 5. 合法移动方向：`down` 6. 非法方向：任意其他字符串，如 `legacy` |
| 棋盘是否可移动 | `!canMove()` 时先标记 `myLose = true`，随后不会进入方向分支 | 1. 可移动 2. 不可移动 |
| 移动后是否发生变化 | 决定分数是否增长、棋盘是否变化、是否补新方块 | 1. reset 后重新开局 2. no-op（非法方向） 3. 只移动不合并 4. 普通合并 5. 合并出 `2048` |
| 当前终局状态 | 影响 `win/lose/gameOver/canSaveRecord/overlay` | 1. 非终局 2. 胜利 3. 失败 |
| `auto` 存档状态 | 每次请求后都会调用 `saveGame("auto")` | 1. 刷新后 `hasData=true` 2. 分数与返回状态一致 3. `savedAtUtc` 非空 |

## 输出因子

| 因子 | 说明 |
| --- | --- |
| HTTP 状态码 | 当前实现始终返回 `200` |
| 返回的 `Game2048State` | 关注 `score`、`win`、`lose`、`gameOver`、`canMove`、`canSaveRecord`、`recordSaved`、`overlay`、`messages`、`tiles` |
| SQLite `SavedGames` 表中的 `auto` 记录 | 关注 `slotKey`、`boardJson`、`score`、`win`、`lose`、`scoreRecorded`、`leakedShouldAddTile`、`savedAtUtc` |

## 关键路径

```text
[收到 move 请求]
  ├── id 不存在 ──→ [懒创建新游戏]
  └── id 已存在 ──→ [加载已有游戏]
                      ↓
                  {canMove?}
                  ├── N → [lose=true] → [跳过方向分支] → [保存 auto]
                  └── Y → {direction 是 escape?}
                           ├── Y → [resetGame] → [保存 auto]
                           └── N → {direction 是四个合法移动之一?}
                                    ├── N → [legacy no-op] → [保存 auto]
                                    └── Y → [执行移动/合并]
                                              ↓
                                          {是否合成 2048?}
                                          ├── Y → [win=true]
                                          └── N → {是否仅移动不合并?}
                                                   ├── Y → [分数不变 + 补新方块]
                                                   └── N → [普通合并]
                                              ↓
                                          [保存 auto]
```

## 用例矩阵

| 用例名 | id 是否存在 | direction | 初始棋盘 | 预期返回 | 预期存档 |
| --- | --- | --- | --- | --- | --- |
| `lazy_create_with_unknown_direction` | 不存在 | `legacy` | 由系统新建 | 返回一个初始游戏状态；`score=0`、非终局、16 格、仅两个 `2` | `auto.hasData=true`，`auto.score=0` |
| `shift_without_merge_keeps_score` | 已存在 | `left` | 单个 `4`，可左移但不会合并 | `score` 不变、非终局、棋盘里保留 `4` 且新增一个 `2` | `auto.score` 保持原值 |
| `right_noop_on_aligned_row` | 已存在 | `right` | 第一行已右对齐且无可合并值 | 棋盘不变、`score` 不变、非终局 | `SavedGames.auto` 精确保存当前棋盘 |
| `up_noop_on_aligned_column` | 已存在 | `up` | 第一列已上对齐且无可合并值 | 棋盘不变、`score` 不变、非终局 | `SavedGames.auto` 精确保存当前棋盘 |
| `down_noop_on_aligned_column` | 已存在 | `down` | 第一列已下对齐且无可合并值 | 棋盘不变、`score` 不变、非终局 | `SavedGames.auto` 精确保存当前棋盘 |
| `merge_tiles_and_refresh_auto_save` | 已存在 | `left` | 满盘且最后一行可合并一次 | `score` 增加、棋盘左移合并、非终局 | `auto` 被刷新，分数与返回值一致 |
| `escape_resets_finished_game` | 已存在 | `escape` | 已结束/已记录成绩的旧局面 | 返回一个全新的初始游戏状态，终局标记被清空 | `auto.score=0` |
| `winning_merge_sets_win_state_and_auto_save` | 已存在 | `left` | 满盘且可合并出 `2048` | `win=true`、`gameOver=true`、`canSaveRecord=true`、棋盘包含 `2048` | `auto.score` 为胜利后的分数 |
| `stuck_board_marks_lose_and_auto_save` | 已存在 | `left` | 满盘且无任何可移动空间 | `lose=true`、`gameOver=true`、棋盘不变 | `auto.score` 保持原值 |

## 覆盖性检查

1. 代码路径覆盖：懒创建、`left/right/up/down` 四个合法方向分支、普通合并、`escape` reset、非法方向 no-op、胜利、失败都已覆盖
2. 输入取值覆盖：`id` 的两类取值、`direction` 的 `escape` / `left` / `right` / `up` / `down` / 非法 六类取值、棋盘的可移动/不可移动两类取值都已覆盖
3. 判断逻辑覆盖：
   - `games.GetOrAdd(...)`：覆盖命中缓存与创建新实例
   - `if (!canMove())`：覆盖 true / false
   - `if (keyCode == "escape")`：覆盖 true / false
   - `switch (keyCode)`：覆盖 `left/right/up/down` 四个合法分支与未命中任何分支
   - `if (num.Equals("2048"))`：覆盖 true / false
   - `if (!myWin && !canMove())`：覆盖胜利后不再转失败，以及非胜利且无路可走转失败
   - auto-save 持久化：覆盖通过 `SavedGames` 表直接验证存档行
   - 终局文案：覆盖胜利消息与失败消息的 legacy 重复输出
