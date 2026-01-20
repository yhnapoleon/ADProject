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
	}
}

