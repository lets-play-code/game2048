# language: zh-CN
功能: 排行榜接口

  场景: 排行榜为空时返回空数组
    当GET "/api/leaderboard"
    那么response should be:
    """
    body.json= []
    """

  场景: 排行榜按最高分排序并为并列成绩共享名次
    假如存在"排行榜记录":
      | playerName | bestScore |
      | Bob        | 256       |
      | Cara       | 256       |
      | Alice      | 128       |
    当GET "/api/leaderboard"
    那么response should be:
    """
    body.json= [{
      rank: 1
      playerName: "Bob"
      score: 256
    }{
      rank: 1
      playerName: "Cara"
      score: 256
    }{
      rank: 3
      playerName: "Alice"
      score: 128
    }]
    """
