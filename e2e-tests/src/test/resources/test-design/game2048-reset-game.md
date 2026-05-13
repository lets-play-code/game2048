# POST /api/games/{id}/reset 测试设计

## 被测对象

- 接口：`POST /api/games/{id}/reset`
- 作用：将指定 `id` 对应的 2048 游戏重置为新开局状态

## 输入因子

| 因子 | 说明 | 等价类 / 边界值 |
| --- | --- | --- |
| `id` 对应游戏是否已存在 | `Program.cs` 通过 `games.GetOrAdd(id, _ => new Game2048())` 懒创建实例，然后统一执行 `resetGame()` | 1. 已存在游戏 2. 不存在，需懒创建 |
| 当前游戏状态 | `resetGame()` 会清空旧局面的分数、终局标记、已记录成绩标记，再重新生成两个初始方块 | 1. 已结束且已记录成绩 2. 全新或不存在的游戏 |
| 请求体 | 路由不读取 body，但 RESTful-cucumber 发送 POST 时需要显式传 `{}` | 固定值：`{}` |
| 存档表现状 | `/reset` 路由不会调用 `saveGame(...)`，因此不应创建或刷新任何存档 | 1. 无存档记录 |

## 输出因子

| 因子 | 说明 |
| --- | --- |
| HTTP 状态码 | 当前实现始终返回 `200` |
| 返回的 `Game2048State` | 关注 `score`、`win`、`lose`、`gameOver`、`canMove`、`canSaveRecord`、`recordSaved`、`overlay`、`messages`、`tiles` |
| SQLite `SavedGames` 表 | 关注调用后仍然没有新增 `auto` 或手动存档记录 |

## 关键路径

```text
[收到 reset 请求]
  ↓
{id 已存在?}
  ├── N → [懒创建新游戏实例]
  └── Y → [取出现有游戏实例]
            ↓
        [执行 resetGame()]
            ↓
        [清空 score / win / lose / recordSaved / leakedShouldAddTile]
            ↓
        [重新放置两个初始方块]
            ↓
        [返回新的 Game2048State]
```

## 用例矩阵

| 用例名 | id 是否存在 | 初始状态 | 预期返回 | 预期存档 |
| --- | --- | --- | --- | --- |
| `lazy_create_unknown_game_and_return_new_board` | 不存在 | 无 | 返回一个新的初始游戏状态；`score=0`、非终局、16 格、仅两个 `2` | `SavedGames` 仍为空 |
| `reset_existing_finished_game_and_clear_terminal_flags` | 已存在 | 已胜利、已记录成绩、分数非零 | 返回一个新的初始游戏状态；旧局面的 `win`、`recordSaved`、`score` 被清空 | `SavedGames` 仍为空 |

## 覆盖性检查

1. 代码路径覆盖：覆盖了 `games.GetOrAdd(...)` 的懒创建路径和命中已有实例路径，以及二者共用的 `resetGame()` 路径
2. 输入取值覆盖：`id` 的两类取值、当前状态的“无现有游戏”和“已结束旧局面”两类取值都已覆盖
3. 判断逻辑覆盖：
   - `games.GetOrAdd(...)`：覆盖创建新实例与复用已有实例
   - `resetGame()`：覆盖旧状态字段被清空并重新生成初始棋盘
   - `/reset` 无持久化副作用：通过直接断言 `SavedGames` 为空覆盖“不刷新 auto 存档”的行为
