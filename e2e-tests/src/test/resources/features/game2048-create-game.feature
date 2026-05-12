# language: zh-CN
功能: 创建 2048 游戏

  场景: POST /api/games 返回一个新的游戏状态
    当POST "/api/games":
      """ application/json
      {}
      """
    那么response should be:
      """
      : {
        code= 200
        body.json= {
          id= /^[0-9a-f]{32}$/
          state= {
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

        body.json.state.tiles.x[]= [0 1 2 3 0 1 2 3 0 1 2 3 0 1 2 3]
        body.json.state.tiles.y[]= [0 0 0 0 1 1 1 1 2 2 2 2 3 3 3 3]
        body.json.state.tiles.xOffset[]= [16 96 176 256 16 96 176 256 16 96 176 256 16 96 176 256]
        body.json.state.tiles.yOffset[]= [16 16 16 16 96 96 96 96 176 176 176 176 256 256 256 256]
      }
      """
    那么response should be:
      """
      (+body.json.state.tiles.value[])= ['' '' '' '' '' '' '' '' '' '' '' '' '' '' '2' '2']
      """
