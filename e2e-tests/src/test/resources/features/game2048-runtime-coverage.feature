# language: zh-CN
功能: 2048 运行时覆盖率补强

  场景: POST /api/test/generated-tile-value 在 value 非法时返回 400
    当POST "/api/test/generated-tile-value":
      """
      {
        "value": "8"
      }
      """
    那么response should be:
      """
      : {
        code= 400
        body.json.error= "Generated tile value must be '2', '4', or null. (Parameter 'tileValue')"
      }
      """

  场景: POST /api/test/generated-tile-value 在 value 缺失时恢复随机出块
    当POST "/api/test/generated-tile-value":
      """
      {}
      """
    那么response should be:
      """
      code= 204
      """
    当POST "/api/games":
      """
      {}
      """
    那么response should be:
      """
      : {
        code= 200
        body.json= {
          id= /^[0-9a-f]{32}$/
          state: {
            score= 0
            win= false
            lose= false
            gameOver= false
            canMove= true
            canSaveRecord= false
            recordSaved= false
            tiles.size= 16
          }
        }
      }
      """

  场景: POST /api/test/games/{id} 在 boardJson 缺失时返回 400
    当POST "/api/test/games/missing-board-json-game":
      """
      {
        "score": 0,
        "win": false,
        "lose": false,
        "scoreRecorded": false,
        "leakedShouldAddTile": false
      }
      """
    那么response should be:
      """
      : {
        code= 400
        body.json.error= "Board data is required. (Parameter 'boardJson')"
      }
      """

  场景: POST /api/test/games/{id} 在 boardJson 长度非法时返回 400
    当POST "/api/test/games/invalid-board-length-game":
      """
      {
        "boardJson": "[\"2\"]",
        "score": 0,
        "win": false,
        "lose": false,
        "scoreRecorded": false,
        "leakedShouldAddTile": false
      }
      """
    那么response should be:
      """
      : {
        code= 400
        body.json.error= "Saved board data is invalid."
      }
      """
    当GET "/api/games/invalid-board-length-game"
    那么response should be:
      """
      : {
        code= 200
        body.json: {
          score= 0
          win= false
          lose= false
          gameOver= false
          canMove= true
          canSaveRecord= false
          recordSaved= false
          tiles.size= 16
        }
      }
      """

  场景: POST /api/games/{id}/load/{slotKey} 在存档棋盘损坏时返回 400 且保留默认新局
    假如存在"存档记录":
      """
      slotKey: "slot1"
      boardJson: ```
                 ["2"]
                 ```
      score: 32
      win: false
      lose: false
      scoreRecorded: false
      leakedShouldAddTile: false
      savedAtUtc: '2026-04-04T00:00:00Z'
      """
    当POST "/api/games/corrupted-save-load-game/load/slot1":
      """
      {}
      """
    那么response should be:
      """
      : {
        code= 400
        body.json.error= "Saved board data is invalid."
      }
      """
    当GET "/api/games/corrupted-save-load-game"
    那么response should be:
      """
      : {
        code= 200
        body.json: {
          score= 0
          win= false
          lose= false
          gameOver= false
          canMove= true
          canSaveRecord= false
          recordSaved= false
          tiles.size= 16
        }
      }
      """

  场景: POST /api/games/{id}/move 在满盘且仅纵向可移动时不会新增方块
    假如存在"已存在的游戏":
      """
      gameId: vertical-only-move-game
      score: 128
      leakedShouldAddTile: true
      boardJson: ```
                 ["2","4","8","16",\
                  "2","32","64","128",\
                  "256","512","1024","4",\
                  "8","16","32","64"]
                 ```
      """
    当POST "/api/games/vertical-only-move-game/move":
      """
      {
        "direction": "left"
      }
      """
    那么response should be:
      """
      : {
        code= 200
        body.json: {
          score= 128
          win= false
          lose= false
          gameOver= false
          canMove= true
          canSaveRecord= false
          recordSaved= false
          tiles: {
            size= 16
            value[]= ['2' '4' '8' '16'
                      '2' '32' '64' '128'
                      '256' '512' '1024' '4'
                      '8' '16' '32' '64']
          }
        }
      }
      """
    那么所有"存档记录"应为:
      """
      = [{
        id= *
        slotKey: "auto"
        boardJson.json: ['2' '4' '8' '16'
                         '2' '32' '64' '128'
                         '256' '512' '1024' '4'
                         '8' '16' '32' '64']
        score: 128
        win: false
        lose: false
        scoreRecorded: false
        leakedShouldAddTile: false
        savedAtUtc= *
      }]
      """
