using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Data;

public class ApplicationDbContext : DbContext
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
	{
	}

	public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
	public DbSet<CarbonReference> CarbonReferences => Set<CarbonReference>();
	public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
	public DbSet<AiInsight> AiInsights => Set<AiInsight>();
	public DbSet<StepRecord> StepRecords => Set<StepRecord>();
	public DbSet<Post> Posts => Set<Post>();
	public DbSet<Comment> Comments => Set<Comment>();
	public DbSet<UserFollow> UserFollows => Set<UserFollow>();
	public DbSet<TravelLog> TravelLogs => Set<TravelLog>();
	public DbSet<UtilityBill> UtilityBills => Set<UtilityBill>();

    public override int SaveChanges()
	{
		ApplyTimestamps();
		return base.SaveChanges();
	}

	public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		ApplyTimestamps();
		return base.SaveChangesAsync(cancellationToken);
	}

	private void ApplyTimestamps()
	{
		var utcNow = DateTime.UtcNow;
		foreach (var entry in ChangeTracker.Entries().Where(e => e.Entity is BaseEntity))
		{
			if (entry.State == EntityState.Added)
			{
				((BaseEntity)entry.Entity).CreatedAt = utcNow;
				((BaseEntity)entry.Entity).UpdatedAt = utcNow;
			}
			else if (entry.State == EntityState.Modified)
			{
				((BaseEntity)entry.Entity).UpdatedAt = utcNow;
			}
		}
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// ApplicationUser relations
		modelBuilder.Entity<ApplicationUser>()
			.HasMany(u => u.ActivityLogs)
			.WithOne(a => a.User!)
			.HasForeignKey(a => a.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<ApplicationUser>()
			.HasMany(u => u.AiInsights)
			.WithOne(i => i.User!)
			.HasForeignKey(i => i.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<ApplicationUser>()
			.HasMany(u => u.StepRecords)
			.WithOne(s => s.User!)
			.HasForeignKey(s => s.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<ApplicationUser>()
			.HasMany<TravelLog>()
			.WithOne(t => t.User!)
			.HasForeignKey(t => t.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<ApplicationUser>()
			.HasMany<UtilityBill>()
			.WithOne(u => u.User!)
			.HasForeignKey(u => u.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		// ActivityLog -> CarbonReference
		modelBuilder.Entity<ActivityLog>()
			.HasOne(a => a.CarbonReference!)
			.WithMany()
			.HasForeignKey(a => a.CarbonReferenceId)
			.OnDelete(DeleteBehavior.Restrict);

		// Decimal precisions (mirror annotations, explicit for safety)
		modelBuilder.Entity<ApplicationUser>()
			.Property(p => p.TotalCarbonSaved)
			.HasColumnType("decimal(18,2)");

		modelBuilder.Entity<ApplicationUser>()
			.Property(p => p.IsActive)
			.HasDefaultValue(true);

		modelBuilder.Entity<CarbonReference>()
			.Property(p => p.Co2Factor)
			.HasColumnType("decimal(18,4)");

		modelBuilder.Entity<ActivityLog>()
			.Property(p => p.Quantity)
			.HasColumnType("decimal(18,4)");

		modelBuilder.Entity<ActivityLog>()
			.Property(p => p.TotalEmission)
			.HasColumnType("decimal(18,4)");

		modelBuilder.Entity<StepRecord>()
			.Property(p => p.CarbonOffset)
			.HasColumnType("decimal(18,4)");

		// TravelLog decimal precisions
		modelBuilder.Entity<TravelLog>()
			.Property(p => p.CarbonEmission)
			.HasColumnType("decimal(18,4)");

		modelBuilder.Entity<TravelLog>()
			.Property(p => p.DistanceKilometers)
			.HasColumnType("decimal(10,2)");

		modelBuilder.Entity<TravelLog>()
			.Property(p => p.OriginLatitude)
			.HasColumnType("decimal(10,7)");

		modelBuilder.Entity<TravelLog>()
			.Property(p => p.OriginLongitude)
			.HasColumnType("decimal(10,7)");

		modelBuilder.Entity<TravelLog>()
			.Property(p => p.DestinationLatitude)
			.HasColumnType("decimal(10,7)");

		modelBuilder.Entity<TravelLog>()
			.Property(p => p.DestinationLongitude)
			.HasColumnType("decimal(10,7)");

		// UtilityBill decimal precisions
		modelBuilder.Entity<UtilityBill>()
			.Property(p => p.ElectricityUsage)
			.HasColumnType("decimal(18,4)");

		modelBuilder.Entity<UtilityBill>()
			.Property(p => p.WaterUsage)
			.HasColumnType("decimal(18,4)");

		modelBuilder.Entity<UtilityBill>()
			.Property(p => p.GasUsage)
			.HasColumnType("decimal(18,4)");

		modelBuilder.Entity<UtilityBill>()
			.Property(p => p.ElectricityCarbonEmission)
			.HasColumnType("decimal(18,4)");

		modelBuilder.Entity<UtilityBill>()
			.Property(p => p.WaterCarbonEmission)
			.HasColumnType("decimal(18,4)");

		modelBuilder.Entity<UtilityBill>()
			.Property(p => p.GasCarbonEmission)
			.HasColumnType("decimal(18,4)");

		modelBuilder.Entity<UtilityBill>()
			.Property(p => p.TotalCarbonEmission)
			.HasColumnType("decimal(18,4)");

		modelBuilder.Entity<UtilityBill>()
			.Property(p => p.OcrConfidence)
			.HasColumnType("decimal(5,4)");

		// Community relations
		modelBuilder.Entity<Post>()
			.HasOne(p => p.User!)
			.WithMany()
			.HasForeignKey(p => p.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Post>()
			.HasMany(p => p.Comments)
			.WithOne(c => c.Post!)
			.HasForeignKey(c => c.PostId)
			.OnDelete(DeleteBehavior.Cascade);

		// PB-008: 全局过滤软删除帖子
		modelBuilder.Entity<Post>()
			.HasQueryFilter(p => !p.IsDeleted);

		modelBuilder.Entity<Comment>()
			.HasOne(c => c.User!)
			.WithMany()
			.HasForeignKey(c => c.UserId)
			.OnDelete(DeleteBehavior.NoAction);

		// Social follow relations
		modelBuilder.Entity<UserFollow>()
			.HasOne(f => f.Follower!)
			.WithMany()
			.HasForeignKey(f => f.FollowerId)
			.OnDelete(DeleteBehavior.NoAction);

		modelBuilder.Entity<UserFollow>()
			.HasOne(f => f.Followee!)
			.WithMany()
			.HasForeignKey(f => f.FolloweeId)
			.OnDelete(DeleteBehavior.NoAction);

		modelBuilder.Entity<UserFollow>()
			.HasIndex(f => new { f.FollowerId, f.FolloweeId })
			.IsUnique();

		// DietTemplate relations
		modelBuilder.Entity<DietTemplate>()
			.HasMany(t => t.Items)
			.WithOne(i => i.DietTemplate!)
			.HasForeignKey(i => i.DietTemplateId)
			.OnDelete(DeleteBehavior.Cascade);

		// Seed data for CarbonReferences
		modelBuilder.Entity<CarbonReference>().HasData(
			new CarbonReference
			{
				Id = 1,
				LabelName = "Beef",
				Category = Models.Enums.CarbonCategory.Food,
				Co2Factor = 27.0m,
				Unit = "kgCO2"
			},
			new CarbonReference
			{
				Id = 2,
				LabelName = "Subway",
				Category = Models.Enums.CarbonCategory.Transport,
				Co2Factor = 0.03m,
				Unit = "kgCO2/km"
			},
			new CarbonReference
			{
				Id = 3,
				LabelName = "Electricity",
				Category = Models.Enums.CarbonCategory.Utility,
				Co2Factor = 0.5m,
				Unit = "kgCO2/kWh"
			},
			new CarbonReference
			{
				Id = 4,
				LabelName = "Walking",
				Category = Models.Enums.CarbonCategory.Transport,
				Co2Factor = 0m,
				Unit = "kgCO2/km"
			},
			new CarbonReference
			{
				Id = 5,
				LabelName = "Bicycle",
				Category = Models.Enums.CarbonCategory.Transport,
				Co2Factor = 0m,
				Unit = "kgCO2/km"
			},
			new CarbonReference
			{
				Id = 6,
				LabelName = "ElectricBike",
				Category = Models.Enums.CarbonCategory.Transport,
				Co2Factor = 0.02m,
				Unit = "kgCO2/km"
			},
			new CarbonReference
			{
				Id = 7,
				LabelName = "Bus",
				Category = Models.Enums.CarbonCategory.Transport,
				Co2Factor = 0.05m,
				Unit = "kgCO2/km"
			},
			new CarbonReference
			{
				Id = 8,
				LabelName = "Taxi",
				Category = Models.Enums.CarbonCategory.Transport,
				Co2Factor = 0.2m,
				Unit = "kgCO2/km"
			},
			new CarbonReference
			{
				Id = 9,
				LabelName = "CarGasoline",
				Category = Models.Enums.CarbonCategory.Transport,
				Co2Factor = 0.2m,
				Unit = "kgCO2/km"
			},
			new CarbonReference
			{
				Id = 10,
				LabelName = "CarElectric",
				Category = Models.Enums.CarbonCategory.Transport,
				Co2Factor = 0.05m,
				Unit = "kgCO2/km"
			},
			new CarbonReference
			{
				Id = 11,
				LabelName = "Train",
				Category = Models.Enums.CarbonCategory.Transport,
				Co2Factor = 0.04m,
				Unit = "kgCO2/km"
			},
			new CarbonReference
			{
				Id = 12,
				LabelName = "Plane",
				Category = Models.Enums.CarbonCategory.Transport,
				Co2Factor = 0.25m,
				Unit = "kgCO2/km"
			},
			new CarbonReference
			{
				Id = 13,
				LabelName = "Electricity_SG",
				Category = Models.Enums.CarbonCategory.Utility,
				Co2Factor = 0.4057m,
				Unit = "kgCO2/kWh"
			},
			new CarbonReference
			{
				Id = 14,
				LabelName = "Water_SG",
				Category = Models.Enums.CarbonCategory.Utility,
				Co2Factor = 0.419m,
				Unit = "kgCO2/m³"
			},
			new CarbonReference
			{
				Id = 15,
				LabelName = "Gas_SG",
				Category = Models.Enums.CarbonCategory.Utility,
				Co2Factor = 0.184m,
				Unit = "kgCO2/kWh"
			}
		);
	}
}

