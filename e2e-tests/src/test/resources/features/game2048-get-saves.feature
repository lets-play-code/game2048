# language: zh-CN
功能: 查询 2048 存档摘要

  场景: GET /api/saves 在没有任何存档时返回四个空槽位
    当GET "/api/saves"
    那么response should be:
      """
      : {
        code= 200
        body.json= [{
          slotKey: "auto"
          hasData: false
          score: null
          savedAtUtc: null
        }{
          slotKey: "slot1"
          hasData: false
          score: null
          savedAtUtc: null
        }{
          slotKey: "slot2"
          hasData: false
          score: null
          savedAtUtc: null
        }{
          slotKey: "slot3"
          hasData: false
          score: null
          savedAtUtc: null
        }]
      }
      """
    那么所有"存档记录"应为:
      """
      = []
      """

  场景: GET /api/saves 会按固定顺序返回稀疏存档摘要且不修改存档
    假如存在"存档记录":
      """
      slotKey: "auto"
      boardJson: ```
                 ["2","","","","","","","","","","","","","","",""]
                 ```
      score: 4
      win: false
      lose: false
      scoreRecorded: false
      leakedShouldAddTile: false
      savedAtUtc: '2026-01-01T00:00:00Z'
      """
    假如存在"存档记录":
      """
      slotKey: "slot2"
      boardJson: ```
                 ["128","","","","","","","","","","","","","","",""]
                 ```
      score: 128
      win: true
      lose: false
      scoreRecorded: true
      leakedShouldAddTile: true
      savedAtUtc: '2026-02-02T00:00:00Z'
      """
    当GET "/api/saves"
    那么response should be:
      """
      : {
        code= 200
        body.json= [{
          slotKey: "auto"
          hasData: true
          score: 4
          savedAtUtc= *
        }{
          slotKey: "slot1"
          hasData: false
          score: null
          savedAtUtc: null
        }{
          slotKey: "slot2"
          hasData: true
          score: 128
          savedAtUtc= *
        }{
          slotKey: "slot3"
          hasData: false
          score: null
          savedAtUtc: null
        }]
      }
      """
    那么所有"存档记录"应为:
      """
      = [{
        id= *
        slotKey: "auto"
        boardJson.json: ['2' '' '' ''
                         '' '' '' ''
                         '' '' '' ''
                         '' '' '' '']
        score: 4
        win: false
        lose: false
        scoreRecorded: false
        leakedShouldAddTile: false
        savedAtUtc: '2026-01-01T00:00:00Z'
      }{
        id= *
        slotKey: "slot2"
        boardJson.json: ['128' '' '' ''
                         '' '' '' ''
                         '' '' '' ''
                         '' '' '' '']
        score: 128
        win: true
        lose: false
        scoreRecorded: true
        leakedShouldAddTile: true
        savedAtUtc: '2026-02-02T00:00:00Z'
      }]
      """
