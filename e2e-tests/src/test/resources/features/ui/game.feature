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
