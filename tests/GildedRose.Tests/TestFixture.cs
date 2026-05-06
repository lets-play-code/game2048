using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GildedRoseApp = GildedRose.App.GildedRose;
using Item = GildedRose.App.Item;

namespace GildedRose.Tests;

public static class TexttestFixture
{
    public static string RenderInventoryReport(string[] args)
    {
        int days = 31;
        if (args.Length > 0)
        {
            days = int.Parse(args[0], CultureInfo.InvariantCulture) + 1;
        }

        return RenderInventoryReport(days);
    }

    public static string RenderInventoryReport(int days = 31)
    {
        IList<Item> items = CreateSampleItems();
        GildedRoseApp app = new GildedRoseApp(items);
        StringBuilder output = new StringBuilder();

        output.AppendLine("OMGHAI!");
        output.AppendLine();

        for (int i = 0; i < days; i++)
        {
            output.AppendLine("-------- day " + i + " --------");
            output.AppendLine("name, sellIn, quality");
            for (int j = 0; j < items.Count; j++)
            {
                output.AppendLine(items[j].Name + ", " + items[j].SellIn + ", " + items[j].Quality);
            }
            output.AppendLine();
            app.UpdateQuality();
        }

        return output.ToString();
    }

    public static List<Item> CreateSampleItems()
    {
        return new List<Item>
        {
            new Item { Name = "+5 Dexterity Vest", SellIn = 10, Quality = 20 },
            new Item { Name = "Aged Brie", SellIn = 2, Quality = 0 },
            new Item { Name = "Elixir of the Mongoose", SellIn = 5, Quality = 7 },
            new Item { Name = "Sulfuras, Hand of Ragnaros", SellIn = 0, Quality = 80 },
            new Item { Name = "Sulfuras, Hand of Ragnaros", SellIn = -1, Quality = 80 },
            new Item
            {
                Name = "Backstage passes to a TAFKAL80ETC concert",
                SellIn = 15,
                Quality = 20
            },
            new Item
            {
                Name = "Backstage passes to a TAFKAL80ETC concert",
                SellIn = 10,
                Quality = 49
            },
            new Item
            {
                Name = "Backstage passes to a TAFKAL80ETC concert",
                SellIn = 5,
                Quality = 49
            },
            new Item { Name = "Conjured Mana Cake", SellIn = 3, Quality = 6 }
        };
    }
}
