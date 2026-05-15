# language: zh-CN
功能: 移动 2048 游戏

  场景: POST /api/games/{id}/move 会为不存在的 id 懒创建游戏并刷新 auto 存档
    当POST "/api/games/lazy-move-game/move":
      """
      {
        "direction": "noop"
      }
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

  场景: POST /api/games/{id}/move 在只移动不合并时保持分数并刷新 auto 存档
    假如存在"已存在的游戏":
      """
      gameId: shifting-game
      score: 7
      boardJson: ```
                 ["","","4","",\
                  "","","","",\
                  "","","","",\
                  "","","",""]
                 ```
      """
    当POST "/api/games/shifting-game/move":
      """
      {
        "direction": "left"
      }
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
      body.json.tiles.value[]= +['' '' '' ''
                                 '' '' '' ''
                                 '' '' '' ''
                                 '' '' '2' '4']
      """
    那么所有"存档记录"应为:
      """
      = [{
        id= *
        slotKey: "auto"
        boardJson= *
        score: 7
        win: false
        lose: false
        scoreRecorded: false
        leakedShouldAddTile: true
        savedAtUtc= *
      }]
      """

  场景: POST /api/games/{id}/move 在 direction 为 right 且棋盘已右对齐时保持棋盘不变并刷新 auto 存档
    假如存在"已存在的游戏":
      """
      gameId: right-noop-game
      score: 5
      boardJson: ```
                 ["","2","4","8",\
                  "","","","",\
                  "","","","",\
                  "","","",""]
                 ```
      """
    当POST "/api/games/right-noop-game/move":
      """
      {
        "direction": "right"
      }
      """
    那么response should be:
      """
      : {
        code= 200
        body.json= {
          score= 5
          scoreText= 'Score: 5'
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
      body.json.tiles.value[]= ['' '2' '4' '8'
                                '' '' '' ''
                                '' '' '' ''
                                '' '' '' '']
      """
    那么所有"存档记录"应为:
      """
      = [{
        id= *
        slotKey: "auto"
        boardJson.json: ['' '2' '4' '8'
                         '' '' '' ''
                         '' '' '' ''
                         '' '' '' '']
        score: 5
        win: false
        lose: false
        scoreRecorded: false
        leakedShouldAddTile: false
        savedAtUtc= *
      }]
      """

  场景: POST /api/games/{id}/move 在 direction 为 up 且棋盘已上对齐时保持棋盘不变并刷新 auto 存档
    假如存在"已存在的游戏":
      """
      gameId: up-noop-game
      score: 5
      boardJson: ```
                 ["2","","","",\
                  "4","","","",\
                  "8","","","",\
                  "","","",""]
                 ```
      """
    当POST "/api/games/up-noop-game/move":
      """
      {
        "direction": "up"
      }
      """
    那么response should be:
      """
      : {
        code= 200
        body.json= {
          score= 5
          scoreText= 'Score: 5'
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
      body.json.tiles.value[]= ['2' '' '' ''
                                '4' '' '' ''
                                '8' '' '' ''
                                '' '' '' '']
      """
    那么所有"存档记录"应为:
      """
      = [{
        id= *
        slotKey: "auto"
        boardJson.json: ['2' '' '' ''
                         '4' '' '' ''
                         '8' '' '' ''
                         '' '' '' '']
        score: 5
        win: false
        lose: false
        scoreRecorded: false
        leakedShouldAddTile: false
        savedAtUtc= *
      }]
      """

  场景: POST /api/games/{id}/move 在 direction 为 down 且棋盘已下对齐时保持棋盘不变并刷新 auto 存档
    假如存在"已存在的游戏":
      """
      gameId: down-noop-game
      score: 5
      boardJson: ```
                 ["","","","",\
                  "2","","","",\
                  "4","","","",\
                  "8","","",""]
                 ```
      """
    当POST "/api/games/down-noop-game/move":
      """
      {
        "direction": "down"
      }
      """
    那么response should be:
      """
      : {
        code= 200
        body.json= {
          score= 5
          scoreText= 'Score: 5'
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
      body.json.tiles.value[]= ['' '' '' ''
                                '2' '' '' ''
                                '4' '' '' ''
                                '8' '' '' '']
      """
    那么所有"存档记录"应为:
      """
      = [{
        id= *
        slotKey: "auto"
        boardJson.json: ['' '' '' ''
                         '2' '' '' ''
                         '4' '' '' ''
                         '8' '' '' '']
        score: 5
        win: false
        lose: false
        scoreRecorded: false
        leakedShouldAddTile: false
        savedAtUtc= *
      }]
      """

  场景: POST /api/games/{id}/move 会合并方块并刷新 auto 存档
    假如存在"已存在的游戏":
      """
      gameId: movable-game
      score: 8
      boardJson: ```
                 ["16","32","64","128",\
                  "256","512","1024","2",\
                  "4","8","16","32",\
                  "2","2","4","8"]
                 ```
      """
    当POST "/api/games/movable-game/move":
      """
      {
        "direction": "left"
      }
      """
    那么response should be:
      """
      : {
        code= 200
        body.json= {
          score= 12
          scoreText= 'Score: 12'
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
      body.json.tiles.value[]= ['16' '32' '64' '128'
                                '256' '512' '1024' '2'
                                '4' '8' '16' '32'
                                '4' '4' '8' '2']
      """
    那么所有"存档记录"应为:
      """
      = [{
        id= *
        slotKey: "auto"
        boardJson.json: ['16' '32' '64' '128'
                        '256' '512' '1024' '2'
                        '4' '8' '16' '32'
                        '4' '4' '8' '2']
        score: 12
        win: false
        lose: false
        scoreRecorded: false
        leakedShouldAddTile: true
        savedAtUtc= *
      }]
      """

  场景: POST /api/games/{id}/move 在 direction 为 escape 时重置游戏并刷新 auto 存档
    假如存在"已存在的游戏":
      """
      gameId: resettable-game
      score: 32
      win: true
      scoreRecorded: true
      boardJson: ```
                 ["1024","1024","","",\
                  "","","","",\
                  "","","","",\
                  "","","",""]
                 ```
      """
    当POST "/api/games/resettable-game/move":
      """
      {
        "direction": "escape"
      }
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
      (+body.json.tiles.value[])= ['' '' '' ''
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

  场景: POST /api/games/{id}/move 在合并出 2048 后返回胜利状态并刷新 auto 存档
    假如存在"已存在的游戏":
      """
      gameId: winning-game
      score: 100
      boardJson: ```
                 ["16","32","64","128",\
                  "256","512","2","4",\
                  "8","16","32","64",\
                  "1024","1024","4","8"]
                 ```
      """
    当POST "/api/games/winning-game/move":
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
          score= 2148
          scoreText= 'Score: 2148'
          scoreTextDrawCount= 16
          win= true
          lose= false
          gameOver= true
          canMove= false
          canSaveRecord= true
          recordSaved= false
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
      body.json.tiles.value[]= ['16' '32' '64' '128'
                                '256' '512' '2' '4'
                                '8' '16' '32' '64'
                                '2048' '4' '8' '2']
      """
    那么response should be:
      """
      body.json.messages= ['You won!' 'Press ESC to play again' 'You won!' 'Press ESC to play again' 'You won!' 'Press ESC to play again' 'You won!' 'Press ESC to play again' 'You won!' 'Press ESC to play again' 'You won!' 'Press ESC to play again' 'You won!' 'Press ESC to play again' 'You won!' 'Press ESC to play again' 'You won!' 'Press ESC to play again' 'You won!' 'Press ESC to play again' 'You won!' 'Press ESC to play again' 'You won!' 'Press ESC to play again' 'You won!' 'Press ESC to play again' 'You won!' 'Press ESC to play again' 'You won!' 'Press ESC to play again' 'You won!' 'Press ESC to play again']
      """
    那么所有"存档记录"应为:
      """
      = [{
        id= *
        slotKey: "auto"
        boardJson.json: ['16' '32' '64' '128'
                        '256' '512' '2' '4'
                        '8' '16' '32' '64'
                        '2048' '4' '8' '2']
        score: 2148
        win: true
        lose: false
        scoreRecorded: false
        leakedShouldAddTile: true
        savedAtUtc= *
      }]
      """

  场景: POST /api/games/{id}/move 在无路可走时返回失败状态并刷新 auto 存档
    假如存在"已存在的游戏":
      """
      gameId: stuck-game
      score: 24
      boardJson: ```
                 ["2","4","8","16",\
                  "32","64","128","256",\
                  "512","1024","2","4",\
                  "8","16","32","64"]
                 ```
      """
    当POST "/api/games/stuck-game/move":
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
          score= 24
          scoreText= 'Score: 24'
          scoreTextDrawCount= 16
          win= false
          lose= true
          gameOver= true
          canMove= false
          canSaveRecord= true
          recordSaved= false
          boardBackground= '#bbada0'
          panelWidth= 4
          panelHeight= 4
          tileSize= 64
          tilesMargin= 16
          overlay= true
          messages.size= 48
          tiles.size= 16
        }
      }
      """
    那么response should be:
      """
      body.json.tiles.value[]= ['2' '4' '8' '16'
                                '32' '64' '128' '256'
                                '512' '1024' '2' '4'
                                '8' '16' '32' '64']
      """
    那么response should be:
      """
      body.json.messages= ['Game over!' 'You lose!' 'Press ESC to play again' 'Game over!' 'You lose!' 'Press ESC to play again' 'Game over!' 'You lose!' 'Press ESC to play again' 'Game over!' 'You lose!' 'Press ESC to play again' 'Game over!' 'You lose!' 'Press ESC to play again' 'Game over!' 'You lose!' 'Press ESC to play again' 'Game over!' 'You lose!' 'Press ESC to play again' 'Game over!' 'You lose!' 'Press ESC to play again' 'Game over!' 'You lose!' 'Press ESC to play again' 'Game over!' 'You lose!' 'Press ESC to play again' 'Game over!' 'You lose!' 'Press ESC to play again' 'Game over!' 'You lose!' 'Press ESC to play again' 'Game over!' 'You lose!' 'Press ESC to play again' 'Game over!' 'You lose!' 'Press ESC to play again' 'Game over!' 'You lose!' 'Press ESC to play again' 'Game over!' 'You lose!' 'Press ESC to play again']
      """
    那么所有"存档记录"应为:
      """
      = [{
        id= *
        slotKey: "auto"
        boardJson.json: ['2' '4' '8' '16'
                        '32' '64' '128' '256'
                        '512' '1024' '2' '4'
                        '8' '16' '32' '64']
        score: 24
        win: false
        lose: true
        scoreRecorded: false
        leakedShouldAddTile: false
        savedAtUtc= *
      }]
      """
