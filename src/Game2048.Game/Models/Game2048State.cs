using System.Collections.Generic;

namespace Game2048.Game;

public class Game2048State
{
    public string BoardBackground { get; set; }
    public int PanelWidth { get; set; }
    public int PanelHeight { get; set; }
    public int TileSize { get; set; }
    public int TilesMargin { get; set; }
    public bool Win { get; set; }
    public bool Lose { get; set; }
    public bool GameOver { get; set; }
    public bool CanMove { get; set; }
    public bool CanSaveRecord { get; set; }
    public bool RecordSaved { get; set; }
    public int Score { get; set; }
    public string ScoreText { get; set; }
    public int ScoreTextDrawCount { get; set; }
    public bool Overlay { get; set; }
    public List<string> Messages { get; set; } = new List<string>();
    public List<TileState> Tiles { get; set; } = new List<TileState>();
}
