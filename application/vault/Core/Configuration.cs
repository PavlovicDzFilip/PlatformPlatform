using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.SharedKernel.Configuration;
using Vault.Database;

namespace Vault;

public static class Configuration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static IHostApplicationBuilder AddVaultInfrastructure(this IHostApplicationBuilder builder)
    {
        // Infrastructure is configured separately from other Infrastructure services to allow mocking in tests
        return builder.AddSharedInfrastructure<VaultDbContext>("vault-database");
    }

    public static IServiceCollection AddVaultServices(this IServiceCollection services)
    {
        return services.AddSharedServices<VaultDbContext>(Assembly);
    }
}
