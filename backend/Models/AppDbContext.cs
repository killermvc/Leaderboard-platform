using Microsoft.EntityFrameworkCore;

namespace Leaderboard.Models;

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
		modelBuilder.Entity<Game>().HasIndex(g => g.Name).IsUnique();

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

		// Configure Score status with default value
		modelBuilder.Entity<Score>()
			.Property(s => s.Status)
			.HasDefaultValue(ScoreStatus.Pending);

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