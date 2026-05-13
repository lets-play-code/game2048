# language: zh-CN
功能: 保存 2048 排行榜成绩

  场景: POST /api/games/{id}/leaderboard 对不存在的 id 会懒创建默认新局并因未终局返回 400
    当POST "/api/games/lazy-leaderboard-game/leaderboard":
      """
      {
        "playerName": "Alice"
      }
      """
    那么response should be:
      """
      : {
        code= 400
        body.json.error= "Leaderboard records can only be saved after the game is over."
      }
      """
    那么所有"排行榜记录"应为:
      """
      = []
      """

  场景: POST /api/games/{id}/leaderboard 在 playerName 缺失时返回 400 且不写入排行榜
    假如存在"已存在的游戏":
      """
      gameId: missing-player-name-game
      score: 32
      lose: true
      boardJson: '["2","4","8","16","32","64","128","256","512","1024","2","4","8","16","32","64"]'
      """
    当POST "/api/games/missing-player-name-game/leaderboard":
      """
      {}
      """
    那么response should be:
      """
      : {
        code= 400
        body.json.error= "Player name is required. (Parameter 'playerName')"
      }
      """
    那么所有"排行榜记录"应为:
      """
      = []
      """

  场景: POST /api/games/{id}/leaderboard 在 wall 成功时会保存仅空白字符的名字并标记当前局已记录
    假如存在"排行榜墙响应":
      """
      statusCode: 200
      """
    假如存在"已存在的游戏":
      """
      gameId: blank-player-name-game
      score: 64
      lose: true
      boardJson: '["2","4","8","16","32","64","128","256","512","1024","2","4","8","16","32","64"]'
      """
    当POST "/api/games/blank-player-name-game/leaderboard":
      """
      {
        "playerName": "   "
      }
      """
    那么response should be:
      """
      : {
        code= 200
        body.json.score= 64
        body.json.win= false
        body.json.lose= true
        body.json.gameOver= true
        body.json.canSaveRecord= false
        body.json.recordSaved= true
      }
      """
    那么所有"排行榜记录"应为:
      """
      = [{
        id= *
        playerName: "   "
        bestScore: 64
        updatedAtUtc= *
      }]
      """

  场景: POST /api/games/{id}/leaderboard 会用更高的分数覆盖同名玩家已有最佳成绩
    假如存在"排行榜墙响应":
      """
      statusCode: 200
      """
    假如存在"排行榜记录":
      | playerName | bestScore |
      | Alice      | 128       |
    假如存在"已存在的游戏":
      """
      gameId: better-score-game
      score: 256
      win: true
      leakedShouldAddTile: true
      boardJson: '["1024","1024","","","","","","","","","","","","","",""]'
      """
    当POST "/api/games/better-score-game/leaderboard":
      """
      {
        "playerName": "Alice"
      }
      """
    那么response should be:
      """
      : {
        code= 200
        body.json.score= 256
        body.json.win= true
        body.json.lose= false
        body.json.gameOver= true
        body.json.canSaveRecord= false
        body.json.recordSaved= true
      }
      """
    那么所有"排行榜记录"应为:
      """
      = [{
        id= *
        playerName: "Alice"
        bestScore: 256
        updatedAtUtc= *
      }]
      """

  场景: POST /api/games/{id}/leaderboard 在当前分数更低时保持同名玩家已有最佳成绩
    假如存在"排行榜墙响应":
      """
      statusCode: 200
      """
    假如存在"排行榜记录":
      | playerName | bestScore |
      | Bob        | 512       |
    假如存在"已存在的游戏":
      """
      gameId: lower-score-game
      score: 128
      lose: true
      boardJson: '["2","4","8","16","32","64","128","256","512","1024","2","4","8","16","32","64"]'
      """
    当POST "/api/games/lower-score-game/leaderboard":
      """
      {
        "playerName": "Bob"
      }
      """
    那么response should be:
      """
      : {
        code= 200
        body.json.score= 128
        body.json.win= false
        body.json.lose= true
        body.json.gameOver= true
        body.json.canSaveRecord= false
        body.json.recordSaved= true
      }
      """
    那么所有"排行榜记录"应为:
      """
      = [{
        id= *
        playerName: "Bob"
        bestScore: 512
        updatedAtUtc= *
      }]
      """

  场景: POST /api/games/{id}/leaderboard 在当前局已记录过成绩时返回 400 且不修改排行榜
    假如存在"排行榜记录":
      | playerName | bestScore |
      | Cara       | 300       |
    假如存在"已存在的游戏":
      """
      gameId: already-recorded-game
      score: 256
      win: true
      scoreRecorded: true
      leakedShouldAddTile: true
      boardJson: '["1024","1024","","","","","","","","","","","","","",""]'
      """
    当POST "/api/games/already-recorded-game/leaderboard":
      """
      {
        "playerName": "Cara"
      }
      """
    那么response should be:
      """
      : {
        code= 400
        body.json.error= "This game's score has already been recorded."
      }
      """
    那么所有"排行榜记录"应为:
      """
      = [{
        id= *
        playerName: "Cara"
        bestScore: 300
        updatedAtUtc= *
      }]
      """

  场景: POST /api/games/{id}/leaderboard 在 wall 失败时仍先写入数据库但不会把当前局标记为已记录
    假如存在"排行榜墙响应":
      """
      statusCode: 500
      """
    假如存在"已存在的游戏":
      """
      gameId: wall-failure-game
      score: 200
      lose: true
      boardJson: '["2","4","8","16","32","64","128","256","512","1024","2","4","8","16","32","64"]'
      """
    当POST "/api/games/wall-failure-game/leaderboard":
      """
      {
        "playerName": "Dana"
      }
      """
    那么response should be:
      """
      code= 500
      """
    那么所有"排行榜记录"应为:
      """
      = [{
        id= *
        playerName: "Dana"
        bestScore: 200
        updatedAtUtc= *
      }]
      """
    当GET "/api/games/wall-failure-game"
    那么response should be:
      """
      : {
        code= 200
        body.json.score= 200
        body.json.win= false
        body.json.lose= true
        body.json.gameOver= true
        body.json.canSaveRecord= true
        body.json.recordSaved= false
      }
      """
