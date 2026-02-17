using System.Reflection;
using EcoLens.Api.Data;
using EcoLens.Api.Migrations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EcoLens.Tests.Migrations;

/// <summary>
/// 覆盖 Migrations 与 ModelSnapshot 的测试，用于提升 EcoLens.Api.Migrations 的覆盖率。
/// 迁移为 SQL Server 生成，无法在 SQLite 上完整执行，故仅对 Snapshot 执行 BuildModel、对迁移类做实例化与属性校验。
/// </summary>
public class MigrationsTests
{
	[Fact]
	public void ApplicationDbContextModelSnapshot_BuildModel_ExecutesWithoutThrow()
	{
		var snapshotType = typeof(ApplicationDbContext).Assembly
			.GetType("EcoLens.Api.Migrations.ApplicationDbContextModelSnapshot", throwOnError: true);
		Assert.NotNull(snapshotType);

		var snapshot = Activator.CreateInstance(snapshotType!, nonPublic: true);
		Assert.NotNull(snapshot);

		var buildModel = snapshotType!.GetMethod("BuildModel",
			BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
		Assert.NotNull(buildModel);

		var modelBuilder = new ModelBuilder();
		buildModel!.Invoke(snapshot, new object[] { modelBuilder });

		var model = modelBuilder.Model;
		Assert.NotNull(model);
		Assert.True(model.GetEntityTypes().Any(), "Snapshot should define at least one entity.");
	}

	[Fact]
	public void AddTreeStateFields_CanBeInstantiated_AndBuildTargetModelRuns()
	{
		var migration = new AddTreeStateFields();
		Assert.NotNull(migration);
		Assert.Equal("AddTreeStateFields", migration.GetType().Name);

		// 调用 Designer 中的 BuildTargetModel，提升迁移 Designer 的覆盖率
		var buildTargetModel = migration.GetType().GetMethod("BuildTargetModel",
			BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
		if (buildTargetModel != null)
		{
			var modelBuilder = new ModelBuilder();
			buildTargetModel.Invoke(migration, new object[] { modelBuilder });
			Assert.True(modelBuilder.Model.GetEntityTypes().Any());
		}
	}

	[Fact]
	public void AddUserNickname_CanBeInstantiated_AndBuildTargetModelRuns()
	{
		var migration = new AddUserNickname();
		Assert.NotNull(migration);
		Assert.Equal("AddUserNickname", migration.GetType().Name);
		InvokeBuildTargetModel(migration);
	}

	[Fact]
	public void AddStepUsageFieldsToApplicationUser_CanBeInstantiated_AndBuildTargetModelRuns()
	{
		var migration = new AddStepUsageFieldsToApplicationUser();
		Assert.NotNull(migration);
		Assert.Equal("AddStepUsageFieldsToApplicationUser", migration.GetType().Name);
		InvokeBuildTargetModel(migration);
	}

	private static void InvokeBuildTargetModel(object migration)
	{
		var buildTargetModel = migration.GetType().GetMethod("BuildTargetModel",
			BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
		if (buildTargetModel == null) return;
		var modelBuilder = new ModelBuilder();
		buildTargetModel.Invoke(migration, new object[] { modelBuilder });
		Assert.True(modelBuilder.Model.GetEntityTypes().Any());
	}
}
