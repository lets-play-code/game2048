# Game2048

## 常用命令

```bash
dotnet build Game2048.sln
dotnet test Game2048.sln
dotnet run --project src/Game2048.Web
```

## 结构

- `src/Game2048.Game`：游戏核心逻辑与 SQLite 持久化
- `src/Game2048.Web`：Minimal API 与静态页面
- `tests/Game2048.Tests`：xUnit 测试

## 说明

- 默认数据库路径：`src/Game2048.Web/App_Data/game2048.db`
- 存档槽位：`auto`、`slot1`、`slot2`、`slot3`
