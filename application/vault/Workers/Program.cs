using Vault;
using Vault.Database;
using PlatformPlatform.SharedKernel.Configuration;
using PlatformPlatform.SharedKernel.Database;

// Worker service is using WebApplication.CreateBuilder instead of Host.CreateDefaultBuilder to allow scaling to zero
var builder = WebApplication.CreateBuilder(args);

// Configure storage infrastructure like Database, BlobStorage, Logging, Telemetry, Entity Framework DB Context, etc.
builder
    .AddDevelopmentPort(9399)
    .AddVaultInfrastructure();

// Configure dependency injection services like Repositories, MediatR, Pipelines, FluentValidation validators, etc.
builder.Services
    .AddWorkerServices()
    .AddVaultServices();

builder.Services.AddTransient<DatabaseMigrationService<VaultDbContext>>();

var host = builder.Build();

// Apply migrations to the database (should be moved to GitHub Actions or similar in production)
using var scope = host.Services.CreateScope();
var migrationService = scope.ServiceProvider.GetRequiredService<DatabaseMigrationService<VaultDbContext>>();
migrationService.ApplyMigrations();

await host.RunAsync();
