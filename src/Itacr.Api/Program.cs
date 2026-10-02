using Itacr.Api.Common;
using Itacr.Api.Features.Catalog;
using Itacr.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.AddJsonConsole();
}

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddApiBase();
builder.Services.AddCatalogFeature();

var app = builder.Build();

app.UseApiBase();

app.Run();

// Makes the entry point visible to WebApplicationFactory<Program> in the test project.
public partial class Program;
