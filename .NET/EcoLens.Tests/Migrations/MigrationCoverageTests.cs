using System.Threading.Tasks;
using EcoLens.Api.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace EcoLens.Tests.Migrations;

/// <summary>
/// Runs EF Core migrations (InitialCreate, AddPointAwardLogsEf, etc.) against SQL Server
/// so that migration code paths are executed and counted in code coverage.
/// Uses LocalDB when available (Windows); otherwise uses Testcontainers.MsSql (e.g. Linux CI).
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

	/// <summary>
	/// Returns a SQL Server connection string and an optional disposable (container), or null when no SQL Server is available.
	/// When LocalDB is available uses it; otherwise starts a MsSql container (e.g. CI with Docker). Returns null if neither is available.
	/// </summary>
	private static async Task<(string ConnectionString, IAsyncDisposable? Container)?> GetSqlServerConnectionAsync()
	{
		if (LocalDbAvailable())
		{
			var dbName = "EcoLensMigrationCoverage_" + Guid.NewGuid().ToString("N")[..8];
			var conn = $"Server=(localdb)\\mssqllocaldb;Database={dbName};Trusted_Connection=True;MultipleActiveResultSets=true";
			return (conn, null);
		}

		try
		{
			var container = new MsSqlBuilder().Build();
			await container.StartAsync();
			return (container.GetConnectionString(), container);
		}
		catch (Exception)
		{
			// Docker not available (e.g. local dev); skip test so build does not fail. CI (ubuntu) has Docker.
			return null;
		}
	}

	[Fact]
	public async Task ApplyAllMigrations_InitialCreate_And_AddPointAwardLogsEf_Execute_Successfully()
	{
		var connection = await GetSqlServerConnectionAsync();
		if (connection == null)
			return; // No LocalDB and no Docker; skip (e.g. local dev). CI has Docker.
		var (conn, container) = connection.Value;
		try
		{
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

			await using (var context = new ApplicationDbContext(options))
			{
				await context.Database.EnsureDeletedAsync();
			}
		}
		finally
		{
			if (container != null)
				await container.DisposeAsync();
		}
	}

	[Fact]
	public async Task ApplyAllMigrations_Then_EnsureDatabase_CanBeUsed()
	{
		var connection = await GetSqlServerConnectionAsync();
		if (connection == null)
			return; // No LocalDB and no Docker; skip. CI has Docker.
		var (conn, container) = connection.Value;
		try
		{
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
		finally
		{
			if (container != null)
				await container.DisposeAsync();
		}
	}
}
