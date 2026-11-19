using Itacr.Api.Common;
using Itacr.Application;
using Itacr.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.AddJsonConsole();
}

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddApiBase();
builder.Services.AddAuthFeature();
builder.Services.AddPublicOrders();
builder.Services.AddRegistration();

var app = builder.Build();

app.UseApiBase();

await app.RunAsync();
