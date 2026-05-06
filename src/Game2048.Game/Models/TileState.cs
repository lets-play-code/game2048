namespace Game2048.Game;

public class TileState
{
    public int X { get; set; }
    public int Y { get; set; }
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public string Value { get; set; }
    public string Background { get; set; }
    public string Foreground { get; set; }
    public string FontName { get; set; }
    public int FontSize { get; set; }
    public bool IsEmpty { get; set; }
}
