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
	public DbSet<DietTemplate> DietTemplates => Set<DietTemplate>();
	public DbSet<DietTemplateItem> DietTemplateItems => Set<DietTemplateItem>();
	public DbSet<BarcodeReference> BarcodeReferences => Set<BarcodeReference>(); // 新增
	public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();
	public DbSet<FoodRecord> FoodRecords => Set<FoodRecord>();
	public DbSet<DietRecord> DietRecords => Set<DietRecord>();
	public DbSet<PointAwardLog> PointAwardLogs => Set<PointAwardLog>();

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

		// UtilityBill -> ApplicationUser
		modelBuilder.Entity<UtilityBill>()
			.HasOne(b => b.User!)
			.WithMany()
			.HasForeignKey(b => b.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		// ActivityLog -> CarbonReference
		modelBuilder.Entity<ActivityLog>()
			.HasOne(a => a.CarbonReference!)
			.WithMany()
			.HasForeignKey(a => a.CarbonReferenceId)
			.OnDelete(DeleteBehavior.Restrict);

		// FoodRecord -> ApplicationUser（显式绑定 User 导航，避免生成 UserId1 影子列）
		modelBuilder.Entity<FoodRecord>()
			.HasOne(f => f.User)
			.WithMany()
			.HasForeignKey(f => f.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		// DietRecord -> ApplicationUser
		modelBuilder.Entity<DietRecord>()
			.HasOne(d => d.User)
			.WithMany()
			.HasForeignKey(d => d.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		// BarcodeReference -> CarbonReference
		modelBuilder.Entity<BarcodeReference>()
			.HasOne(b => b.CarbonReference!)
			.WithMany()
			.HasForeignKey(b => b.CarbonReferenceId)
			.OnDelete(DeleteBehavior.SetNull);

		// Decimal precisions (mirror annotations, explicit for safety)
		modelBuilder.Entity<ApplicationUser>()
			.Property(p => p.TotalCarbonSaved)
			.HasColumnType("decimal(18,2)");

		modelBuilder.Entity<ApplicationUser>()
			.Property(p => p.TotalCarbonEmission)
			.HasColumnType("decimal(18,4)");

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

		// FoodRecord precisions
		modelBuilder.Entity<FoodRecord>()
			.Property(p => p.EmissionFactor)
			.HasColumnType("decimal(18,4)");
		modelBuilder.Entity<FoodRecord>()
			.Property(p => p.Emission)
			.HasColumnType("decimal(18,4)");

		// DietRecord precisions
		modelBuilder.Entity<DietRecord>()
			.Property(p => p.EmissionFactor)
			.HasColumnType("decimal(18,4)");
		modelBuilder.Entity<DietRecord>()
			.Property(p => p.Emission)
			.HasColumnType("decimal(18,4)");

		modelBuilder.Entity<StepRecord>()
			.Property(p => p.CarbonOffset)
			.HasColumnType("decimal(18,4)");

		// SystemSettings single row
		modelBuilder.Entity<SystemSettings>()
			.HasData(new SystemSettings { Id = 1, ConfidenceThreshold = 80, VisionModel = "default", WeeklyDigest = true, MaintenanceMode = false });

		// UtilityBill precision
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
			.Property(p => p.ElectricityCost)
			.HasColumnType("decimal(18,2)");
		modelBuilder.Entity<UtilityBill>()
			.Property(p => p.WaterCost)
			.HasColumnType("decimal(18,2)");
		modelBuilder.Entity<UtilityBill>()
			.Property(p => p.GasCost)
			.HasColumnType("decimal(18,2)");

		// UtilityBill carbon emission precisions
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
			.HasColumnType("decimal(18,4)");

		// CarbonReference unique constraint for (LabelName, Category, Region)
		modelBuilder.Entity<CarbonReference>()
			.HasIndex(c => new { c.LabelName, c.Category, c.Region })
			.IsUnique();

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
			.OnDelete(DeleteBehavior.NoAction);

		// PB-008: 全局过滤软删除帖子
		modelBuilder.Entity<Post>()
			.HasQueryFilter(p => !p.IsDeleted);

		// 与 Post 的全局筛选匹配，避免必需端被过滤造成的异常
		modelBuilder.Entity<Comment>()
			.HasQueryFilter(c => c.Post != null && !c.Post.IsDeleted);

		modelBuilder.Entity<Comment>()
			.HasOne(c => c.User!)
			.WithMany()
			.HasForeignKey(c => c.UserId)
			.OnDelete(DeleteBehavior.Restrict);

		// Social follow relations
		modelBuilder.Entity<UserFollow>()
			.HasOne(f => f.Follower!)
			.WithMany()
			.HasForeignKey(f => f.FollowerId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<UserFollow>()
			.HasOne(f => f.Followee!)
			.WithMany()
			.HasForeignKey(f => f.FolloweeId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<UserFollow>()
			.HasIndex(f => new { f.FollowerId, f.FolloweeId })
			.IsUnique();

		// DietTemplate relations
		modelBuilder.Entity<DietTemplate>()
			.HasMany(t => t.Items)
			.WithOne(i => i.DietTemplate!)
			.HasForeignKey(i => i.DietTemplateId)
			.OnDelete(DeleteBehavior.Cascade);

		// === Performance Indexes (Added to fix slow queries) ===
		modelBuilder.Entity<FoodRecord>()
			.HasIndex(f => new { f.UserId, f.CreatedAt });

		modelBuilder.Entity<TravelLog>()
			.HasIndex(t => new { t.UserId, t.CreatedAt });

		modelBuilder.Entity<ActivityLog>()
			.HasIndex(a => new { a.UserId, a.CreatedAt });

		modelBuilder.Entity<UtilityBill>()
			.HasIndex(u => new { u.UserId, u.YearMonth });

		// === Critical Indexes for Lookups and Leaderboard ===
		modelBuilder.Entity<ApplicationUser>()
			.HasIndex(u => u.Email)
			.IsUnique(); // 邮箱必须唯一且加索引

		modelBuilder.Entity<ApplicationUser>()
			.HasIndex(u => u.CurrentPoints); // Speed up leaderboard sorting

		modelBuilder.Entity<ApplicationUser>()
			.HasIndex(u => u.TotalCarbonSaved); // Speed up total carbon saved sorting

		modelBuilder.Entity<PointAwardLog>()
			.HasIndex(p => new { p.UserId, p.AwardedAt }); // Leaderboard today/monthly points aggregation

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
				LabelName = "Water",
				Category = Models.Enums.CarbonCategory.Utility,
				Co2Factor = 0.35m,
				Unit = "kgCO2/m3"
			},
			new CarbonReference
			{
				Id = 5,
				LabelName = "Gas",
				Category = Models.Enums.CarbonCategory.Utility,
				Co2Factor = 2.3m,
				Unit = "kgCO2/unit"
			}
		);
	}
}

