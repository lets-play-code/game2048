# Game2048

## 常用命令

```bash
docker compose up -d --build mysql8 mockserver
dotnet build Game2048.sln
dotnet test Game2048.sln
dotnet run --project src/Game2048.Web
```

端到端测试：

```bash
cd e2e-tests
./gradlew cucumber
```

## 结构

- `src/Game2048.Game`：游戏核心逻辑与 MySQL 持久化
- `src/Game2048.Web`：Minimal API 与静态页面
- `tests/Game2048.Tests`：xUnit 测试
- `e2e-tests`：Gradle + Cucumber 测试
- `docker`：容器环境配置

## 说明

- 默认数据库端口：`127.0.0.1:53306`
- MockServer 端口：`127.0.0.1:51081`
- 存档槽位：`auto`、`slot1`、`slot2`、`slot3`
