using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<AnonymousUser> AnonymousUsers => Set<AnonymousUser>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<LeagueMember> LeagueMembers => Set<LeagueMember>();
    public DbSet<Pundit> Pundits => Set<Pundit>();
    public DbSet<PunditPrediction> PunditPredictions => Set<PunditPrediction>();
    public DbSet<GeneratedContent> GeneratedContents => Set<GeneratedContent>();
    public DbSet<NewsFeedItem> NewsFeedItems => Set<NewsFeedItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.DisplayName).HasMaxLength(100);
        });

        modelBuilder.Entity<AnonymousUser>(e =>
        {
            e.ToTable("anonymous_users");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CookieId).IsUnique();
            e.Property(x => x.RecoveryCode).HasMaxLength(32);
            e.Property(x => x.CookieId).HasMaxLength(64);
        });

        modelBuilder.Entity<Match>(e =>
        {
            e.ToTable("matches");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
        });

        modelBuilder.Entity<Prediction>(e =>
        {
            e.ToTable("predictions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.MatchId, x.PredictionType });
            e.HasIndex(x => new { x.AnonymousUserId, x.MatchId, x.PredictionType });
            e.HasOne(x => x.User).WithMany(u => u.Predictions).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.AnonymousUser).WithMany(a => a.Predictions).HasForeignKey(x => x.AnonymousUserId);
            e.HasOne(x => x.Match).WithMany(m => m.Predictions).HasForeignKey(x => x.MatchId);
        });

        modelBuilder.Entity<League>(e =>
        {
            e.ToTable("leagues");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.InviteCode).IsUnique();
            e.Property(x => x.InviteCode).HasMaxLength(12);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId);
        });

        modelBuilder.Entity<LeagueMember>(e =>
        {
            e.ToTable("league_members");
            e.HasKey(x => new { x.LeagueId, x.UserId });
            e.HasOne(x => x.League).WithMany(l => l.Members).HasForeignKey(x => x.LeagueId);
            e.HasOne(x => x.User).WithMany(u => u.LeagueMemberships).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Pundit>(e =>
        {
            e.ToTable("pundits");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<PunditPrediction>(e =>
        {
            e.ToTable("pundit_predictions");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Pundit).WithMany(p => p.Predictions).HasForeignKey(x => x.PunditId);
            e.HasOne(x => x.Match).WithMany(m => m.PunditPredictions).HasForeignKey(x => x.MatchId);
        });

        modelBuilder.Entity<GeneratedContent>(e =>
        {
            e.ToTable("generated_content");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User).WithMany(u => u.GeneratedContents).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.AnonymousUser).WithMany(a => a.GeneratedContents).HasForeignKey(x => x.AnonymousUserId);
        });

        modelBuilder.Entity<NewsFeedItem>(e =>
        {
            e.ToTable("news_feed_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
        });
    }
}
