# language: zh-CN
功能: 保存 2048 游戏

  场景: POST /api/games/{id}/save/{slotKey} 在 slotKey 非法时返回 400 且不写入存档
    当POST "/api/games/invalid-save-game/save/slot9":
      """
      {}
      """
    那么response should be:
      """
      code= 400
      """
    那么所有"存档记录"应为:
      """
      = []
      """

  场景: POST /api/games/{id}/save/{slotKey} 会为不存在的 id 懒创建游戏并保存到 auto
    当POST "/api/games/lazy-save-game/save/auto":
      """
      {}
      """
    那么response should be:
      """
      : {
        code= 200
        body.json= {
          score= 0
          scoreText= 'Score: 0'
          scoreTextDrawCount= 16
          win= false
          lose= false
          gameOver= false
          canMove= true
          canSaveRecord= false
          recordSaved= false
          boardBackground= '#bbada0'
          panelWidth= 4
          panelHeight= 4
          tileSize= 64
          tilesMargin= 16
          overlay= false
          messages= []
          tiles.size= 16
        }
      }
      """
    那么response should be:
      """
      body.json.tiles.value[]= +['' '' '' ''
                                 '' '' '' ''
                                 '' '' '' ''
                                 '' '' '2' '2']
      """
    那么所有"存档记录"应为:
      """
      = [{
        id= *
        slotKey: "auto"
        boardJson= *
        score: 0
        win: false
        lose: false
        scoreRecorded: false
        leakedShouldAddTile: false
        savedAtUtc= *
      }]
      """

  场景: POST /api/games/{id}/save/{slotKey} 会把已记录成绩的胜利局面保存到手动槽位
    假如存在"已存在的游戏":
      """
      gameId: recorded-winning-save-game
      score: 32
      win: true
      scoreRecorded: true
      leakedShouldAddTile: true
      boardJson: ```
                 ["1024","1024","","",\
                  "","","","",\
                  "","","","",\
                  "","","",""]
                 ```
      """
    当POST "/api/games/recorded-winning-save-game/save/slot2":
      """
      {}
      """
    那么response should be:
      """
      : {
        code= 200
        body.json= {
          score= 32
          scoreText= 'Score: 32'
          scoreTextDrawCount= 16
          win= true
          lose= false
          gameOver= true
          canMove= true
          canSaveRecord= false
          recordSaved= true
          boardBackground= '#bbada0'
          panelWidth= 4
          panelHeight= 4
          tileSize= 64
          tilesMargin= 16
          overlay= true
          messages.size= 32
          tiles.size= 16
        }
      }
      """
    那么response should be:
      """
      body.json.tiles.value[]= ['1024' '1024' '' ''
                                '' '' '' ''
                                '' '' '' ''
                                '' '' '' '']
      """
    那么所有"存档记录"应为:
      """
      = [{
        id= *
        slotKey: "slot2"
        boardJson.json: ['1024' '1024' '' ''
                         '' '' '' ''
                         '' '' '' ''
                         '' '' '' '']
        score: 32
        win: true
        lose: false
        scoreRecorded: true
        leakedShouldAddTile: true
        savedAtUtc= *
      }]
      """

  场景: POST /api/games/{id}/save/{slotKey} 会覆盖已有的手动槽位记录
    假如存在"已存在的游戏":
      """
      gameId: overwrite-save-game
      score: 7
      leakedShouldAddTile: true
      boardJson: ```
                 ["","","4","",\
                  "","","","",\
                  "","","","",\
                  "","","",""]
                 ```
      """
    假如存在"存档记录":
      """
      slotKey: "slot2"
      boardJson: ```
                 ["2","2","","",\
                  "","","","",\
                  "","","","",\
                  "","","",""]
                 ```
      score: 999
      win: true
      lose: false
      scoreRecorded: true
      leakedShouldAddTile: false
      savedAtUtc: '2026-01-01T00:00:00Z'
      """
    当POST "/api/games/overwrite-save-game/save/slot2":
      """
      {}
      """
    那么response should be:
      """
      : {
        code= 200
        body.json= {
          score= 7
          scoreText= 'Score: 7'
          scoreTextDrawCount= 16
          win= false
          lose= false
          gameOver= false
          canMove= true
          canSaveRecord= false
          recordSaved= false
          boardBackground= '#bbada0'
          panelWidth= 4
          panelHeight= 4
          tileSize= 64
          tilesMargin= 16
          overlay= false
          messages= []
          tiles.size= 16
        }
      }
      """
    那么response should be:
      """
      body.json.tiles.value[]= ['' '' '4' ''
                                '' '' '' ''
                                '' '' '' ''
                                '' '' '' '']
      """
    那么所有"存档记录"应为:
      """
      = [{
        id= *
        slotKey: "slot2"
        boardJson.json: ['' '' '4' ''
                         '' '' '' ''
                         '' '' '' ''
                         '' '' '' '']
        score: 7
        win: false
        lose: false
        scoreRecorded: false
        leakedShouldAddTile: true
        savedAtUtc= *
      }]
      """
