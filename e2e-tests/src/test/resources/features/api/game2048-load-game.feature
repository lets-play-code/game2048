# language: zh-CN
功能: 读取 2048 游戏存档

  场景: POST /api/games/{id}/load/{slotKey} 在 slotKey 非法时返回 400 且不修改存档
    假如存在"存档记录":
      """
      slotKey: "slot1"
      boardJson: ```
                 ["2","4","","",\
                  "","","","",\
                  "","","","16",\
                  "","","",""]
                 ```
      score: 32
      win: false
      lose: false
      scoreRecorded: false
      leakedShouldAddTile: false
      savedAtUtc: '2026-01-01T00:00:00Z'
      """
    当POST "/api/games/invalid-load-game/load/slot9":
      """
      {}
      """
    那么response should be:
      """
      : {
        code= 400
        body.json= {
          error= "Unknown save slot. (Parameter 'slotKey')"
        }
      }
      """
    那么所有"存档记录"应为:
      """
      = [{
        id= *
        slotKey: "slot1"
        boardJson.json: ['2' '4' '' ''
                         '' '' '' ''
                         '' '' '' '16'
                         '' '' '' '']
        score: 32
        win: false
        lose: false
        scoreRecorded: false
        leakedShouldAddTile: false
        savedAtUtc: '2026-01-01T00:00:00Z'
      }]
      """

  场景: POST /api/games/{id}/load/{slotKey} 在槽位为空时返回 400，但仍为该 id 懒创建默认游戏
    当POST "/api/games/empty-load-game/load/slot3":
      """
      {}
      """
    那么response should be:
      """
      : {
        code= 400
        body.json= {
          error= 'Save slot is empty.'
        }
      }
      """
    那么所有"存档记录"应为:
      """
      = []
      """
    当GET "/api/games/empty-load-game"
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
          tiles: {
            size= 16
            value[]= +['' '' '' ''
                       '' '' '' ''
                       '' '' '' ''
                       '' '' '2' '2']
          }
        }
      }
      """

  场景: POST /api/games/{id}/load/{slotKey} 会为不存在的 id 懒创建游戏并加载手动槽位
    假如存在"存档记录":
      """
      slotKey: "slot2"
      boardJson: ```
                 ["1024","1024","","",\
                  "","","","",\
                  "","","","",\
                  "","","",""]
                 ```
      score: 32
      win: true
      lose: false
      scoreRecorded: true
      leakedShouldAddTile: true
      savedAtUtc: '2026-02-02T00:00:00Z'
      """
    当POST "/api/games/lazy-load-game/load/slot2":
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
          tiles: {
            size= 16
            value[]= ['1024' '1024' '' ''
                      '' '' '' ''
                      '' '' '' ''
                      '' '' '' '']
          }
        }
      }
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
        savedAtUtc: '2026-02-02T00:00:00Z'
      }]
      """

  场景: POST /api/games/{id}/load/{slotKey} 会用 auto 槽位覆盖已存在的内存游戏
    假如存在"已存在的游戏":
      """
      gameId: overwrite-loaded-game
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
      slotKey: "auto"
      boardJson: ```
                 ["2","4","","",\
                  "","","","",\
                  "","","","16",\
                  "","","",""]
                 ```
      score: 32
      win: false
      lose: false
      scoreRecorded: false
      leakedShouldAddTile: false
      savedAtUtc: '2026-03-03T00:00:00Z'
      """
    当POST "/api/games/overwrite-loaded-game/load/auto":
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
          tiles: {
            size= 16
            value[]= ['2' '4' '' ''
                      '' '' '' ''
                      '' '' '' '16'
                      '' '' '' '']
          }
        }
      }
      """
    那么所有"存档记录"应为:
      """
      = [{
        id= *
        slotKey: "auto"
        boardJson.json: ['2' '4' '' ''
                         '' '' '' ''
                         '' '' '' '16'
                         '' '' '' '']
        score: 32
        win: false
        lose: false
        scoreRecorded: false
        leakedShouldAddTile: false
        savedAtUtc: '2026-03-03T00:00:00Z'
      }]
      """
    当GET "/api/games/overwrite-loaded-game"
    那么response should be:
      """
      : {
        code= 200
        body.json= {
          score= 32
          scoreText= 'Score: 32'
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
          tiles: {
            size= 16
            value[]= ['2' '4' '' ''
                      '' '' '' ''
                      '' '' '' '16'
                      '' '' '' '']
          }
        }
      }
      """
