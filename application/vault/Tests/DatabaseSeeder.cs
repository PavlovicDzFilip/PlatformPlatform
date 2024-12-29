using Bogus;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Domain;
using Vault.Database;

namespace Vault.Tests;

public sealed class DatabaseSeeder
{
    private readonly Faker _faker = new();

    public DatabaseSeeder(VaultDbContext vaultDbContext)
    {
        OwnerUser = new UserInfo
        {
            Email = "owner@tenant-1.com",
            FirstName = _faker.Person.FirstName,
            LastName = _faker.Person.LastName,
            Id = UserId.NewId(),
            IsAuthenticated = true,
            Locale = "en-US",
            Role = "Owner",
            TenantId = new TenantId("tenant-1")
        };

        vaultDbContext.SaveChanges();
    }

    public UserInfo OwnerUser { get; set; }
}
