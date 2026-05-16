# 2048 Runtime Coverage Gap API 测试设计

## 目标

补齐 `e2e-tests/build/reports/coverage/backend-runtime.cobertura.xml` 中仍可由 Cucumber API 场景驱动覆盖的运行时代码，重点关注：

1. 测试专用 API 的异常分支
2. 损坏棋盘数据在测试注入与正式加载路径上的处理
3. 满盘且仅存在纵向可移动时的 `canMove` / `addTile` 边界路径
4. 清空强制生成配置后回到随机出块路径

## 被测对象流程图

```text
[POST /api/test/generated-tile-value]
  ├── {value 非 2/4/null} ──→ [400 BadRequest]
  └── {value 为 null} ═══→ [清空强制出块配置]
                           └── [POST /api/games] ──→ [新局仅生成 2/4]

[POST /api/test/games/{id}]
  ├── {boardJson 缺失/空白} ──→ [400 BadRequest]
  └── {boardJson 长度非法} ──→ [400 BadRequest]

[POST /api/games/{id}/load/{slot}]
  ├── {存档棋盘长度非法} ──→ [400 BadRequest]
  └── {懒创建的内存游戏保留默认新局}

[POST /api/games/{id}/move]
  └── {满盘 + leakedShouldAddTile=true + 左移不变 + 仅纵向可移动}
       ═══→ [触发 addTile 的无空位返回]
       ═══→ [canMove 走纵向分支]
       ═══→ [响应棋盘不变，auto 存档 leakedShouldAddTile=false]
```

## 输入因子分析

| 因子 | 说明 | 取值/等价类 |
| --- | --- | --- |
| generatedTileValue.value | 测试 API 请求体中的强制出块值 | `8`（非法）、`null`（恢复随机） |
| seed.boardJson | 测试注入棋盘 JSON | 缺失/空白、长度非法（`["2"]`）、合法满盘 |
| savedGame.boardJson | 已存档棋盘 JSON | 长度非法（`["2"]`） |
| leakedShouldAddTile | 进入 `left()` 前的遗留标记 | `true`、`false` |
| board movability | 满盘时是否仍可移动 | 仅纵向可移动 |
| move.direction | 移动方向 | `left` |

## 输出因子

| 输出因子 | 说明 |
| --- | --- |
| HTTP status | 204 / 400 / 200 |
| error message | 非法输入或损坏数据时的错误消息 |
| game state | `canMove`、`lose`、`recordSaved`、棋盘内容 |
| saved games | `auto` 槽位是否写入、`leakedShouldAddTile` 是否归零 |

## 用例矩阵

| 用例名 | generatedTileValue.value | seed.boardJson | savedGame.boardJson | leakedShouldAddTile | direction | 期望输出 |
| --- | --- | --- | --- | --- | --- | --- |
| invalid-generated-tile-value | `8` | - | - | - | - | 400，提示只允许 `2`/`4`/`null` |
| clear-forced-generated-tile-value | `null` | - | - | - | - | 204，后续新局仅生成 `2` 或 `4` |
| seed-game-missing-board-json | - | 缺失/空白 | - | - | - | 400，提示棋盘数据必填 |
| seed-game-invalid-board-length | - | `["2"]` | - | - | - | 400，提示存档棋盘非法 |
| load-invalid-saved-board | - | - | `["2"]` | - | - | 400，且懒创建的内存游戏仍为默认新局 |
| move-full-board-with-vertical-only-move | - | 合法满盘 | - | `true` | `left` | 200，棋盘不变、`canMove=true`、不新增方块、auto 存档 `leakedShouldAddTile=false` |

## 覆盖性检查

1. **代码路径**：覆盖测试 API 的 `ArgumentException` / `InvalidOperationException` 分支、加载损坏存档分支、`left()` 内“需要补 tile 但没有空位”的路径、`canMove()` 的纵向比较分支。
2. **输入因子值**：每个输入因子的每个等价类至少由一个用例使用。
3. **判断点覆盖**：
   - `value != null && value != "2" && value != "4"`：覆盖非法值与 `null`
   - `string.IsNullOrWhiteSpace(boardJson)`：覆盖 `true`
   - `values.Length != 16`：覆盖 `true`
   - `if (list.Count == 0)`：覆盖 `true`
   - `if (myTiles[i].Equals(myTiles[i + PANEL_WIDTH]))`：覆盖 `true`
