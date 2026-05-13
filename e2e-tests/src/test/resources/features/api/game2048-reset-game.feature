# language: zh-CN
功能: 重置 2048 游戏

  场景: POST /api/games/{id}/reset 会为不存在的 id 懒创建新游戏且不写入存档
    当POST "/api/games/reset-created-game/reset":
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
      (+body.json.tiles.value[])= ['' '' '' ''
                                   '' '' '' ''
                                   '' '' '' ''
                                   '' '' '2' '2']
      """
    那么所有"存档记录"应为:
      """
      = []
      """

  场景: POST /api/games/{id}/reset 会清空旧局面的终局状态且不刷新存档
    假如存在"已存在的游戏":
      """
      gameId: resettable-game
      score: 32
      win: true
      scoreRecorded: true
      boardJson: '["1024","1024","","","","","","","","","","","","","",""]'
      """
    当POST "/api/games/resettable-game/reset":
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
      (+body.json.tiles.value[])= ['' '' '' ''
                                   '' '' '' ''
                                   '' '' '' ''
                                   '' '' '2' '2']
      """
    那么所有"存档记录"应为:
      """
      = []
      """
