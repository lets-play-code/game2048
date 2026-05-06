using System.Collections.Generic;

namespace Game2048.Game.Core;

internal class BoardTile
{
    private static readonly Dictionary<string, string> ColorMap = new Dictionary<string, string>
    {
        { "2", "#eee4da" },
        { "4", "#ede0c8" },
        { "8", "#f2b179" },
        { "16", "#f59563" },
        { "32", "#f67c5f" },
        { "64", "#f65e3b" },
        { "128", "#edcf72" },
        { "256", "#edcc61" },
        { "512", "#edc850" },
        { "1024", "#edc53f" },
        { "2048", "#edc22e" },
        { "4096", "#edc000" },
        { "8192", "#edab32" }
    };

    public BoardTile()
        : this(string.Empty)
    {
    }

    public BoardTile(string value)
    {
        Value = value;
    }

    public string Value { get; set; }

    public bool IsEmpty()
    {
        return Value.Length == 0;
    }

    public string GetForeground()
    {
        return Value.Length == 1 ? "#776e65" : "#f9f6f2";
    }

    public string GetBackground()
    {
        return ColorMap.TryGetValue(Value, out string color) ? color : "#cdc1b4";
    }

    public override bool Equals(object obj)
    {
        return obj is BoardTile tile && Value.Equals(tile.Value);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}
