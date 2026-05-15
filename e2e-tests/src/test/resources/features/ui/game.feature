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

  场景: left 且棋盘已左对齐时保持棋盘不变并刷新 auto 存档
    假如存在"下一个新游戏标识":
      """
      gameId: ui-left-game
      """
    当用户应该:
      """
      newGame: {...}
      """
    假如存在"已存在的游戏":
      """
      gameId: ui-left-game
      score: 5
      boardJson: '["2","4","8","","","","","","","","","","","","",""]'
      """
    当用户应该:
      """
      : {
        left::eventually: {
          score: 'Score: 5',
          board= ['2' '4' '8' ''
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
        boardJson.json: ['2' '4' '8' ''
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

  场景: up 且棋盘已上对齐时保持棋盘不变并刷新 auto 存档
    假如存在"下一个新游戏标识":
      """
      gameId: ui-up-game
      """
    当用户应该:
      """
      newGame: {...}
      """
    假如存在"已存在的游戏":
      """
      gameId: ui-up-game
      score: 5
      boardJson: '["2","","","","4","","","","8","","","","","","",""]'
      """
    当用户应该:
      """
      : {
        up::eventually: {
          score: 'Score: 5',
          board= ['2' '' '' ''
                 '4' '' '' ''
                 '8' '' '' ''
                 '' '' '' '']
        }
      }
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

  场景: down 且棋盘已下对齐时保持棋盘不变并刷新 auto 存档
    假如存在"下一个新游戏标识":
      """
      gameId: ui-down-game
      """
    当用户应该:
      """
      newGame: {...}
      """
    假如存在"已存在的游戏":
      """
      gameId: ui-down-game
      score: 5
      boardJson: '["","","","","2","","","","4","","","","8","","",""]'
      """
    当用户应该:
      """
      : {
        down::eventually: {
          score: 'Score: 5',
          board= ['' '' '' ''
                 '2' '' '' ''
                 '4' '' '' ''
                 '8' '' '' '']
        }
      }
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

  场景: save 会把当前局面保存到 slot1
    假如存在"下一个新游戏标识":
      """
      gameId: ui-save-game
      """
    当用户应该:
      """
      newGame: {...}
      """
    假如存在"已存在的游戏":
      """
      gameId: ui-save-game
      score: 7
      leakedShouldAddTile: true
      boardJson: '["","","4","","","","","","","","","","","","",""]'
      """
    当用户应该:
      """
      : {
        slot1Save::eventually: {
          score: 'Score: 7',
          board= ['' '' '4' ''
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
        slotKey: "slot1"
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

  场景: load 会从 auto 存档恢复当前局面
    假如存在"下一个新游戏标识":
      """
      gameId: ui-load-game
      """
    当用户应该:
      """
      newGame: {...}
      """
    假如存在"已存在的游戏":
      """
      gameId: ui-load-game
      score: 7
      leakedShouldAddTile: true
      boardJson: '["","","4","","","","","","","","","","","","",""]'
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
    当用户应该:
      """
      : {
        autoLoad::eventually: {
          score: 'Score: 32',
          board= ['2' '4' '' ''
                 '' '' '' ''
                 '' '' '' '16'
                 '' '' '' '']
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
