using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.EntityFramework;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace Vault.Database;

public sealed class VaultDbContext(DbContextOptions<VaultDbContext> options, IExecutionContext executionContext)
    : SharedKernelDbContext<VaultDbContext>(options, executionContext);
