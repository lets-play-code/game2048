# language: zh-CN
@ui
功能: 游戏

  场景: 排行榜按最高分排序并为并列成绩共享名次
    假如存在"排行榜记录":
      | playerName | bestScore |
      | Bob        | 256       |
      | Cara       | 256       |
      | Alice      | 128       |
    当用户应该:
      """
      : {
        records: | Rank | Player | Score |
                 | 1    | Bob    | 256   |
                 | 1    | Cara   | 256   |
                 | 3    | Alice  | 128   |
      }
      """

  场景: 创建游戏
    当用户应该:
      """
      : {
        newGame: {
          score: 'Score: 0',
          board= +['' '' '' '' '' '' '' '' '' '' '' '' '' '' '2' '2']
        }
      }
      """

  场景: right 且棋盘已右对齐时保持棋盘不变并刷新 auto 存档
    假如存在"下一个新游戏标识":
      """
      gameId: ui-created-game
      """
    当用户应该:
      """
      newGame: {...}
      """
    假如存在"已存在的游戏":
      """
      gameId: ui-created-game
      score: 5
      boardJson: '["","2","4","8","","","","","","","","","","","",""]'
      """
    当用户应该:
      """
      : {
        right::eventually: {
          score: 'Score: 5',
          board= ['' '2' '4' '8'
                 '' '' '' ''
                 '' '' '' ''
                 '' '' '' '']
        }
      }
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
