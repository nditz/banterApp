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
    public DbSet<BracketPick> BracketPicks => Set<BracketPick>();
    public DbSet<ExternalId> ExternalIds => Set<ExternalId>();
    public DbSet<SyncRun> SyncRuns => Set<SyncRun>();
    public DbSet<SyncError> SyncErrors => Set<SyncError>();
    public DbSet<StandingRow> StandingRows => Set<StandingRow>();
    public DbSet<MatchEvent> MatchEvents => Set<MatchEvent>();
    public DbSet<LineupPlayer> LineupPlayers => Set<LineupPlayer>();
    public DbSet<MediaSource> MediaSources => Set<MediaSource>();
    public DbSet<MediaItem> MediaItems => Set<MediaItem>();

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
            e.Property(x => x.DeviceFingerprint).HasMaxLength(64).IsRequired(false);
        });

        modelBuilder.Entity<Match>(e =>
        {
            e.ToTable("matches");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.HasIndex(x => x.KickoffTime);
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
            e.HasIndex(x => new { x.Kind, x.CountryCode });
            e.Property(x => x.InviteCode).HasMaxLength(12);
            e.Property(x => x.CountryCode).HasMaxLength(2);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId);
            e.HasOne(x => x.CreatedByAnonymousUser).WithMany().HasForeignKey(x => x.CreatedByAnonymousUserId);
        });

        modelBuilder.Entity<LeagueMember>(e =>
        {
            e.ToTable("league_members");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.LeagueId, x.UserId });
            e.HasIndex(x => new { x.LeagueId, x.AnonymousUserId });
            e.Property(x => x.DisplayName).HasMaxLength(40);
            e.HasOne(x => x.League).WithMany(l => l.Members).HasForeignKey(x => x.LeagueId);
            e.HasOne(x => x.User).WithMany(u => u.LeagueMemberships).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.AnonymousUser).WithMany().HasForeignKey(x => x.AnonymousUserId);
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
            e.Property(x => x.MatchId).HasMaxLength(64).IsRequired(false);
            e.HasOne(x => x.Pundit).WithMany(p => p.Predictions).HasForeignKey(x => x.PunditId);
            e.HasOne(x => x.Match).WithMany(m => m.PunditPredictions).HasForeignKey(x => x.MatchId)
                .IsRequired(false);
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

        modelBuilder.Entity<BracketPick>(e =>
        {
            e.ToTable("bracket_picks");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.SlotId });
            e.HasIndex(x => new { x.AnonymousUserId, x.SlotId });
            e.Property(x => x.SlotId).HasMaxLength(32);
            e.Property(x => x.MatchId).HasMaxLength(64);
            e.Property(x => x.WinnerTeamCode).HasMaxLength(8);
            e.HasOne(x => x.User).WithMany(u => u.BracketPicks).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.AnonymousUser).WithMany(a => a.BracketPicks).HasForeignKey(x => x.AnonymousUserId);
            e.HasOne(x => x.Match).WithMany().HasForeignKey(x => x.MatchId);
        });

        modelBuilder.Entity<ExternalId>(e =>
        {
            e.ToTable("external_ids");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Provider, x.ProviderExternalId, x.EntityType }).IsUnique();
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.Property(x => x.EntityType).HasMaxLength(32);
            e.Property(x => x.EntityId).HasMaxLength(64);
            e.Property(x => x.Provider).HasMaxLength(32);
            e.Property(x => x.ProviderExternalId).HasMaxLength(128);
        });

        modelBuilder.Entity<SyncRun>(e =>
        {
            e.ToTable("sync_runs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.StartedAt);
            e.HasIndex(x => new { x.Provider, x.JobName });
            e.Property(x => x.Provider).HasMaxLength(32);
            e.Property(x => x.JobName).HasMaxLength(64);
            e.Property(x => x.Status).HasMaxLength(16);
        });

        modelBuilder.Entity<SyncError>(e =>
        {
            e.ToTable("sync_errors");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OccurredAt);
            e.Property(x => x.Provider).HasMaxLength(32);
            e.Property(x => x.JobName).HasMaxLength(64);
            e.Property(x => x.EntityType).HasMaxLength(32);
            e.Property(x => x.EntityId).HasMaxLength(64);
            e.HasOne(x => x.SyncRun).WithMany().HasForeignKey(x => x.SyncRunId);
        });

        modelBuilder.Entity<StandingRow>(e =>
        {
            e.ToTable("standing_rows");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.GroupKey, x.TeamCode, x.Provider }).IsUnique();
            e.Property(x => x.GroupKey).HasMaxLength(8);
            e.Property(x => x.TeamCode).HasMaxLength(8);
            e.Property(x => x.TeamName).HasMaxLength(100);
            e.Property(x => x.Provider).HasMaxLength(32);
        });

        modelBuilder.Entity<MatchEvent>(e =>
        {
            e.ToTable("match_events");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MatchId, x.ProviderEventId }).IsUnique();
            e.Property(x => x.MatchId).HasMaxLength(64);
            e.Property(x => x.Type).HasMaxLength(32);
            e.Property(x => x.TeamCode).HasMaxLength(8);
            e.Property(x => x.PlayerName).HasMaxLength(100);
            e.Property(x => x.Provider).HasMaxLength(32);
            e.Property(x => x.ProviderEventId).HasMaxLength(64);
            e.HasOne(x => x.Match).WithMany().HasForeignKey(x => x.MatchId);
        });

        modelBuilder.Entity<LineupPlayer>(e =>
        {
            e.ToTable("lineup_players");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MatchId, x.TeamCode, x.PlayerName, x.IsSubstitute });
            e.Property(x => x.MatchId).HasMaxLength(64);
            e.Property(x => x.TeamCode).HasMaxLength(8);
            e.Property(x => x.PlayerName).HasMaxLength(100);
            e.Property(x => x.Position).HasMaxLength(8);
            e.Property(x => x.Provider).HasMaxLength(32);
            e.HasOne(x => x.Match).WithMany().HasForeignKey(x => x.MatchId);
        });

        modelBuilder.Entity<MediaSource>(e =>
        {
            e.ToTable("media_sources");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.SourceType).HasMaxLength(32);
            e.Property(x => x.ExternalId).HasMaxLength(128);
            e.Property(x => x.RssUrl).HasMaxLength(512);
            e.Property(x => x.SiteUrl).HasMaxLength(512);
        });

        modelBuilder.Entity<MediaItem>(e =>
        {
            e.ToTable("media_items");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MediaSourceId, x.ExternalId }).IsUnique();
            e.Property(x => x.ExternalId).HasMaxLength(128);
            e.Property(x => x.Title).HasMaxLength(300);
            e.Property(x => x.SourceUrl).HasMaxLength(512);
            e.Property(x => x.AudioUrl).HasMaxLength(512);
            e.HasOne(x => x.MediaSource).WithMany(s => s.Items).HasForeignKey(x => x.MediaSourceId);
        });
    }
}
