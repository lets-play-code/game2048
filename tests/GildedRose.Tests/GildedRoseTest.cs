using System.Collections.Generic;
using GildedRoseApp = GildedRose.App.GildedRose;
using Item = GildedRose.App.Item;
using Xunit;

namespace GildedRose.Tests;

public class GildedRoseTest
{
    [Fact]
    public void foo_example_from_source_becomes_a_basic_smoke_test()
    {
        IList<Item> items = new List<Item>
        {
            new Item { Name = "foo", SellIn = 0, Quality = 0 }
        };

        GildedRoseApp app = new GildedRoseApp(items);

        app.UpdateQuality();

        Item item = Assert.Single(items);
        Assert.Equal("foo", item.Name);
        Assert.Equal(-1, item.SellIn);
        Assert.Equal(0, item.Quality);
    }
}
