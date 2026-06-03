using Game2048.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseAutofac();

await builder.AddApplicationAsync<Game2048WebModule>();

var app = builder.Build();

await app.InitializeApplicationAsync();

await app.RunAsync();

public partial class Program { }
