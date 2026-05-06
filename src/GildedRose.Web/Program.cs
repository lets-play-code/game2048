using System.Text.Json;
using GildedRose.App;
using GildedRose.Web;
using GildedRoseApp = GildedRose.App.GildedRose;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/sample-items", () => Results.Ok(CloneItems(SampleInventory.Create())));

app.MapPost("/api/update-quality", (UpdateQualityRequest request) =>
{
    if (request.Items == null)
    {
        return Results.BadRequest(new ErrorResponse("Items are required."));
    }

    if (request.Items.Any(item => item == null))
    {
        return Results.BadRequest(new ErrorResponse("Items cannot contain null entries."));
    }

    List<Item> items = CloneItems(request.Items);
    GildedRoseApp legacyApp = new GildedRoseApp(items);
    legacyApp.UpdateQuality();
    return Results.Ok(items);
});

app.MapFallbackToFile("index.html");

app.Run();

static List<Item> CloneItems(IEnumerable<Item> items)
{
    return items.Select(item => new Item
    {
        Name = item.Name ?? string.Empty,
        SellIn = item.SellIn,
        Quality = item.Quality
    }).ToList();
}

public record UpdateQualityRequest(List<Item> Items);
public record ErrorResponse(string Error);
public partial class Program { }
