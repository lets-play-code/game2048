# language: zh-CN
功能: 查询 2048 游戏

  场景: 首次 GET /api/games/{id} 会返回一个新的游戏状态且不会创建存档
    当GET "/api/games/legacy-e2e-game"
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

        body.json.tiles.x[]= [0 1 2 3 0 1 2 3 0 1 2 3 0 1 2 3]
        body.json.tiles.y[]= [0 0 0 0 1 1 1 1 2 2 2 2 3 3 3 3]
        body.json.tiles.xOffset[]= [16 96 176 256 16 96 176 256 16 96 176 256 16 96 176 256]
        body.json.tiles.yOffset[]= [16 16 16 16 96 96 96 96 176 176 176 176 256 256 256 256]
      }
      """
    那么response should be:
      """
      (+body.json.tiles.value[])= ['' '' '' '' '' '' '' '' '' '' '' '' '' '' '2' '2']
      """

  场景: GET /api/games/{id} 返回一个已存在的游戏状态
    假如存在"已存在的游戏":
      """
      gameId: seeded-existing-game
      score: 32
      win: false
      lose: false
      scoreRecorded: false
      leakedShouldAddTile: false
      boardJson: '["2","4","","","","","","","","","","16","","","",""]'
      """
    当GET "/api/games/seeded-existing-game"
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
          tiles.size= 16
        }
      }
      """
    那么response should be:
      """
      body.json.tiles.value[]= ['2' '4' '' '' '' '' '' '' '' '' '' '16' '' '' '' '']
      """
