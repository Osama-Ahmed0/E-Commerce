using ECommerce.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiConfiguration(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddApiServices();

var app = builder.Build();

app.UseApiPipeline();

await app.SeedDatabaseAsync();

app.Run();