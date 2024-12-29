using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Vault.Database;

[DbContext(typeof(VaultDbContext))]
[Migration("1_Initial")]
public sealed class DatabaseMigrations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }
}
