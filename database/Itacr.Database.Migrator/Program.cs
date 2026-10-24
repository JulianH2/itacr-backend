using Itacr.Database.Migrator;
using Itacr.Database.Migrator.Seeding;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<SeedOptions>().BindConfiguration(SeedOptions.SectionName);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPasswordHasher<Employee>, PasswordHasher<Employee>>();
builder.Services.AddSingleton<EmployeeSeedPlan>();
builder.Services.AddSingleton<EmployeeSeeder>();
builder.Services.AddSingleton<DatabaseMigrator>();

using var host = builder.Build();
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

var migrator = host.Services.GetRequiredService<DatabaseMigrator>();
var succeeded = args switch
{
    ["status", ..] => await migrator.ShowStatusAsync(cancellation.Token),
    ["rollback", var script, ..] => await migrator.RollbackAsync(script, cancellation.Token),
    _ => await migrator.RunAsync(cancellation.Token),
};
return succeeded ? 0 : 1;
