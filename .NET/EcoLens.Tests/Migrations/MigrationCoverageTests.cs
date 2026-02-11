using System.Threading.Tasks;
using EcoLens.Api.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EcoLens.Tests.Migrations;

/// <summary>
/// Runs EF Core migrations (InitialCreate, AddPointAwardLogsEf, etc.) against SQL Server LocalDB
/// so that migration code paths are executed and counted in code coverage.
/// Migrations were generated for SQL Server (nvarchar(max), etc.), so they must run on SQL Server.
/// </summary>
public class MigrationCoverageTests
{
	private const string LocalDbConnection = "Server=(localdb)\\mssqllocaldb;Database=EcoLensMigrationCoverage;Trusted_Connection=True;MultipleActiveResultSets=true";

	private static bool LocalDbAvailable()
	{
		try
		{
			var options = new DbContextOptionsBuilder<ApplicationDbContext>()
				.UseSqlServer(LocalDbConnection);
			using var context = new ApplicationDbContext(options.Options);
			return context.Database.CanConnect();
		}
		catch
		{
			return false;
		}
	}

	[Fact]
	public async Task ApplyAllMigrations_InitialCreate_And_AddPointAwardLogsEf_Execute_Successfully()
	{
		if (!LocalDbAvailable())
		{
			return; // LocalDB not available (e.g. Linux CI); skip to avoid failure
		}

		// Use a unique DB name per run so parallel tests don't conflict.
		var dbName = "EcoLensMigrationCoverage_" + Guid.NewGuid().ToString("N")[..8];
		var conn = $"Server=(localdb)\\mssqllocaldb;Database={dbName};Trusted_Connection=True;MultipleActiveResultSets=true";
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseSqlServer(conn)
			.Options;

		await using (var context = new ApplicationDbContext(options))
		{
			await context.Database.MigrateAsync();
		}

		await using (var context = new ApplicationDbContext(options))
		{
			var applied = await context.Database.GetAppliedMigrationsAsync();
			Assert.NotEmpty(applied);
		}

		// Drop so LocalDB doesn't accumulate databases
		await using (var context = new ApplicationDbContext(options))
		{
			await context.Database.EnsureDeletedAsync();
		}
	}

	[Fact]
	public async Task ApplyAllMigrations_Then_EnsureDatabase_CanBeUsed()
	{
		if (!LocalDbAvailable())
		{
			return;
		}

		var dbName = "EcoLensMigrationCoverage_" + Guid.NewGuid().ToString("N")[..8];
		var conn = $"Server=(localdb)\\mssqllocaldb;Database={dbName};Trusted_Connection=True;MultipleActiveResultSets=true";
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseSqlServer(conn)
			.Options;

		await using var context = new ApplicationDbContext(options);
		await context.Database.MigrateAsync();

		var migrations = await context.Database.GetAppliedMigrationsAsync();
		Assert.Contains(migrations, m => m.Contains("InitialCreate"));
		Assert.Contains(migrations, m => m.Contains("AddPointAwardLogsEf"));

		await context.Database.EnsureDeletedAsync();
	}
}
