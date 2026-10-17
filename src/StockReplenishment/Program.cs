using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using StockReplenishment.Api;
using StockReplenishment.Components;
using StockReplenishment.Data;
using StockReplenishment.Services;
using StockReplenishment.StockCheck;

var builder = WebApplication.CreateBuilder(args);

// Data: EF Core in-memory database (no installation needed).
builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("StockReplenishment"));

// API
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddScoped<RequestService>();

// Background stock check
builder.Services.AddSingleton<IStockCheckQueue, StockCheckQueue>();
builder.Services.AddSingleton<IStockAvailabilityService, SimulatedStockAvailabilityService>();
builder.Services.AddScoped<StockCheckProcessor>();
builder.Services.AddHostedService<StockCheckWorker>();

// UI: Blazor Server + MudBlazor. Pages call the REST API through this HttpClient.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri) });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    SeedData.Seed(scope.ServiceProvider.GetRequiredService<AppDbContext>());
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Stock Replenishment API"));
}

app.UseAntiforgery();
app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

public partial class Program;