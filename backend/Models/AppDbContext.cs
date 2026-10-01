using Microsoft.EntityFrameworkCore;

namespace Leaderboard.Models;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{

    public DbSet<User> Users { get; set; } = null!;
	public DbSet<Game> Games { get; set; } = null!;
	public DbSet<Score> Scores { get; set; } = null!;
	public DbSet<Role> Roles { get; set; } = null!;
	public DbSet<UserRole> UserRoles { get; set; } = null!;
	public DbSet<GameModerator> GameModerators { get; set; } = null!;
	public DbSet<ApiKey> ApiKeys { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
		{
			throw new InvalidOperationException(
				"DbContextOptions must be configured externally");
		}
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
		modelBuilder.Entity<User>().HasIndex(u => u.ClerkUserId).IsUnique();
		modelBuilder.Entity<Game>().HasIndex(g => g.Name).IsUnique();

		modelBuilder.Entity<Game>()
			.HasOne(g => g.Owner)
			.WithMany()
			.HasForeignKey(g => g.OwnerId)
			.OnDelete(DeleteBehavior.SetNull);

		modelBuilder.Entity<ApiKey>()
			.Property(key => key.KeyHash)
			.HasMaxLength(64)
			.IsRequired();

		modelBuilder.Entity<ApiKey>()
			.HasIndex(key => key.KeyHash)
			.IsUnique();

		modelBuilder.Entity<ApiKey>()
			.HasOne<User>()
			.WithMany()
			.HasForeignKey(key => key.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });

		modelBuilder.Entity<UserRole>()
			.HasOne(ur => ur.User)
			.WithMany(u => u.UserRoles)
			.HasForeignKey(ur => ur.UserId);

		modelBuilder.Entity<UserRole>()
			.HasOne(ur => ur.Role)
			.WithMany(r => r.UserRoles)
			.HasForeignKey(ur => ur.RoleId);

		modelBuilder.Entity<Score>()
			.Property(b => b.DateAchieved)
			.HasDefaultValueSql("NOW(6)");

		// Configure Score status with default value, scores from game clients set Approved on submission
		modelBuilder.Entity<Score>()
			.Property(s => s.Status)
			.HasDefaultValue(ScoreStatus.Pending);

		// The name given to scores submitted without a user account, bounded so it stays a compact display name
		modelBuilder.Entity<Score>()
			.Property(s => s.PlayerName)
			.HasMaxLength(64);

		modelBuilder.Entity<Score>()
			.Property(s => s.SubmissionId)
			.HasMaxLength(128);

		modelBuilder.Entity<Score>()
			.HasIndex(s => new { s.GameId, s.SubmissionId })
			.IsUnique();

		// The score owner is optional, scores from game clients only carry a PlayerName
		modelBuilder.Entity<Score>()
			.HasOne(s => s.User)
			.WithMany()
			.HasForeignKey(s => s.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		// Configure GameModerator entity
		modelBuilder.Entity<GameModerator>()
			.HasOne(gm => gm.Game)
			.WithMany()
			.HasForeignKey(gm => gm.GameId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<GameModerator>()
			.HasOne(gm => gm.User)
			.WithMany()
			.HasForeignKey(gm => gm.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		// Ensure unique game-user moderator assignments
		modelBuilder.Entity<GameModerator>()
			.HasIndex(gm => new { gm.GameId, gm.UserId })
			.IsUnique();
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member