using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using Item = GildedRose.App.Item;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GildedRose.Tests;

public class GildedRoseWebTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public GildedRoseWebTest(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task sample_items_match_the_text_fixture_inventory()
    {
        using HttpClient client = factory.CreateClient();

        List<Item> items = await client.GetFromJsonAsync<List<Item>>("/api/sample-items");

        Assert.NotNull(items);
        Assert.Equal(ToSnapshot(TexttestFixture.CreateSampleItems()), ToSnapshot(items));
    }

    [Fact]
    public async Task update_quality_applies_one_legacy_update_to_client_supplied_items()
    {
        using HttpClient client = factory.CreateClient();
        List<Item> items = new()
        {
            new Item { Name = "+5 Dexterity Vest", SellIn = 10, Quality = 20 },
            new Item { Name = "Aged Brie", SellIn = 2, Quality = 0 }
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/update-quality", new { items });

        response.EnsureSuccessStatusCode();
        List<Item> updatedItems = await response.Content.ReadFromJsonAsync<List<Item>>();

        Assert.NotNull(updatedItems);
        Assert.Collection(updatedItems,
            first =>
            {
                Assert.Equal("+5 Dexterity Vest", first.Name);
                Assert.Equal(9, first.SellIn);
                Assert.Equal(19, first.Quality);
            },
            second =>
            {
                Assert.Equal("Aged Brie", second.Name);
                Assert.Equal(1, second.SellIn);
                Assert.Equal(1, second.Quality);
            });
    }

    [Fact]
    public async Task update_quality_rejects_requests_without_items()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/update-quality", new { items = (List<Item>)null! });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string[] ToSnapshot(IEnumerable<Item> items)
    {
        return items.Select(item => $"{item.Name}|{item.SellIn}|{item.Quality}").ToArray();
    }
}
