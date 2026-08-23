using BanterApp.Api.Common;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<AnonymousUser> AnonymousUsers => Set<AnonymousUser>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<CompetitionSeason> CompetitionSeasons => Set<CompetitionSeason>();
    public DbSet<Matchweek> Matchweeks => Set<Matchweek>();
    public DbSet<ClubTeam> ClubTeams => Set<ClubTeam>();
    public DbSet<SeasonTeam> SeasonTeams => Set<SeasonTeam>();
    public DbSet<MatchweekBonus> MatchweekBonuses => Set<MatchweekBonus>();
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<LeagueMember> LeagueMembers => Set<LeagueMember>();
    public DbSet<Pundit> Pundits => Set<Pundit>();
    public DbSet<PunditPrediction> PunditPredictions => Set<PunditPrediction>();
    public DbSet<GeneratedContent> GeneratedContents => Set<GeneratedContent>();
    public DbSet<NewsFeedItem> NewsFeedItems => Set<NewsFeedItem>();
    public DbSet<TournamentBonusPick> TournamentBonusPicks => Set<TournamentBonusPick>();
    public DbSet<TournamentAwardResult> TournamentAwardResults => Set<TournamentAwardResult>();
    public DbSet<ExternalId> ExternalIds => Set<ExternalId>();
    public DbSet<SyncRun> SyncRuns => Set<SyncRun>();
    public DbSet<SyncError> SyncErrors => Set<SyncError>();
    public DbSet<StandingRow> StandingRows => Set<StandingRow>();
    public DbSet<MatchEvent> MatchEvents => Set<MatchEvent>();
    public DbSet<LineupPlayer> LineupPlayers => Set<LineupPlayer>();
    public DbSet<MediaSource> MediaSources => Set<MediaSource>();
    public DbSet<MediaItem> MediaItems => Set<MediaItem>();
    public DbSet<PunditOpinion> PunditOpinions => Set<PunditOpinion>();
    public DbSet<PredictionAggregate> PredictionAggregates => Set<PredictionAggregate>();
    public DbSet<ApplicationErrorLog> ApplicationErrorLogs => Set<ApplicationErrorLog>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
    public DbSet<AuthAuditLog> AuthAuditLogs => Set<AuthAuditLog>();
    public DbSet<ProviderUsageDaily> ProviderUsageDaily => Set<ProviderUsageDaily>();
    public DbSet<JobRegistryState> JobRegistryStates => Set<JobRegistryState>();
    public DbSet<IngestionError> IngestionErrors => Set<IngestionError>();
    public DbSet<OperationalError> OperationalErrors => Set<OperationalError>();
    public DbSet<AppMetric> AppMetrics => Set<AppMetric>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<PlayerStat> PlayerStats => Set<PlayerStat>();
    public DbSet<LeaderboardEntry> LeaderboardEntries => Set<LeaderboardEntry>();
    public DbSet<UserPrediction> UserPredictions => Set<UserPrediction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.DisplayName).HasMaxLength(100);
            e.HasIndex(x => x.IsPlatformAdmin);
            e.HasIndex(x => x.AccountStatus);
        });

        modelBuilder.Entity<AnonymousUser>(e =>
        {
            e.ToTable("anonymous_users");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CookieId).IsUnique();
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.RecoveryCode).HasMaxLength(32);
            e.Property(x => x.CookieId).HasMaxLength(64);
            e.Property(x => x.Username).HasMaxLength(20);
            e.Property(x => x.DeviceFingerprint).HasMaxLength(64).IsRequired(false);
        });

        modelBuilder.Entity<Match>(e =>
        {
            e.ToTable("matches");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.HomeLogoUrl).HasMaxLength(512);
            e.Property(x => x.AwayLogoUrl).HasMaxLength(512);
            e.HasIndex(x => x.KickoffTime);
            e.HasIndex(x => x.MatchweekNumber);
            e.HasIndex(x => x.CompetitionSeasonId);
            e.HasOne(x => x.CompetitionSeason).WithMany(s => s.Matches).HasForeignKey(x => x.CompetitionSeasonId);
            e.HasOne(x => x.Matchweek).WithMany(w => w.Matches).HasForeignKey(x => x.MatchweekId);
        });

        modelBuilder.Entity<Competition>(e =>
        {
            e.ToTable("competitions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.Code);
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.Slug).HasMaxLength(80);
            e.Property(x => x.Code).HasMaxLength(8);
            e.Property(x => x.CountryCode).HasMaxLength(8);
            e.Property(x => x.LogoUrl).HasMaxLength(512);
            e.Property(x => x.Provider).HasMaxLength(32);
            e.Property(x => x.ProviderCompetitionId).HasMaxLength(64);
        });

        modelBuilder.Entity<CompetitionSeason>(e =>
        {
            e.ToTable("competition_seasons");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CompetitionId, x.StartYear }).IsUnique();
            e.HasIndex(x => x.IsCurrent);
            e.Property(x => x.Name).HasMaxLength(32);
            e.Property(x => x.ProviderSeasonId).HasMaxLength(64);
            e.Property(x => x.Status).HasMaxLength(16);
            e.HasOne(x => x.Competition).WithMany(c => c.Seasons).HasForeignKey(x => x.CompetitionId);
        });

        modelBuilder.Entity<Matchweek>(e =>
        {
            e.ToTable("matchweeks");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CompetitionSeasonId, x.Number }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(40);
            e.Property(x => x.Status).HasMaxLength(16);
            e.HasOne(x => x.CompetitionSeason).WithMany(s => s.Matchweeks).HasForeignKey(x => x.CompetitionSeasonId);
        });

        modelBuilder.Entity<ClubTeam>(e =>
        {
            e.ToTable("club_teams");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => new { x.Provider, x.ProviderTeamId });
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.ShortName).HasMaxLength(40);
            e.Property(x => x.Slug).HasMaxLength(80);
            e.Property(x => x.Code).HasMaxLength(8);
            e.Property(x => x.LogoUrl).HasMaxLength(512);
            e.Property(x => x.Provider).HasMaxLength(32);
            e.Property(x => x.ProviderTeamId).HasMaxLength(64);
        });

        modelBuilder.Entity<SeasonTeam>(e =>
        {
            e.ToTable("season_teams");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CompetitionSeasonId, x.TeamId }).IsUnique();
            e.HasOne(x => x.CompetitionSeason).WithMany(s => s.Teams).HasForeignKey(x => x.CompetitionSeasonId);
            e.HasOne(x => x.Team).WithMany(t => t.SeasonTeams).HasForeignKey(x => x.TeamId);
        });

        modelBuilder.Entity<MatchweekBonus>(e =>
        {
            e.ToTable("matchweek_bonuses");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.CompetitionSeasonId, x.MatchweekNumber });
            e.HasIndex(x => new { x.AnonymousUserId, x.CompetitionSeasonId, x.MatchweekNumber });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasOne(x => x.AnonymousUser).WithMany().HasForeignKey(x => x.AnonymousUserId);
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
            e.Property(x => x.Name).HasMaxLength(StringLimits.LeagueName);
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
            e.Property(x => x.DisplayName).HasMaxLength(StringLimits.LeagueMemberDisplayName);
            e.HasOne(x => x.League).WithMany(l => l.Members).HasForeignKey(x => x.LeagueId);
            e.HasOne(x => x.User).WithMany(u => u.LeagueMemberships).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.AnonymousUser).WithMany().HasForeignKey(x => x.AnonymousUserId);
        });

        modelBuilder.Entity<Pundit>(e =>
        {
            e.ToTable("pundits");
            e.HasKey(x => x.Id);
            e.Property(x => x.NormalizedName).HasMaxLength(StringLimits.PunditNormalizedName);
            e.Property(x => x.Role).HasMaxLength(StringLimits.PunditRole);
            e.HasIndex(x => x.NormalizedName);
            e.HasIndex(x => x.Kind);
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
            e.Property(x => x.MatchId).HasMaxLength(64);
            e.Property(x => x.PredictionSummary).HasMaxLength(500);
            e.HasIndex(x => x.MatchId);
            e.HasIndex(x => x.QualityScore);
        });

        modelBuilder.Entity<TournamentBonusPick>(e =>
        {
            e.ToTable("tournament_bonus_picks");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.Category, x.SlotIndex });
            e.HasIndex(x => new { x.AnonymousUserId, x.Category, x.SlotIndex });
            e.Property(x => x.PickValue).HasMaxLength(100);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasOne(x => x.AnonymousUser).WithMany().HasForeignKey(x => x.AnonymousUserId);
        });

        modelBuilder.Entity<TournamentAwardResult>(e =>
        {
            e.ToTable("tournament_award_results");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Category).IsUnique();
            e.Property(x => x.AnswerValue).HasMaxLength(100);
            e.Property(x => x.AnswerDisplay).HasMaxLength(120);
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
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
            e.Property(x => x.MetadataJson).HasColumnType("text");
        });

        modelBuilder.Entity<AdminAuditLog>(e =>
        {
            e.ToTable("admin_audit_logs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.AdminUserId);
            e.Property(x => x.Action).HasMaxLength(64);
            e.Property(x => x.TargetType).HasMaxLength(64);
            e.Property(x => x.TargetId).HasMaxLength(128);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(512);
            e.Property(x => x.MetadataJson).HasColumnType("text");
        });

        modelBuilder.Entity<AuthAuditLog>(e =>
        {
            e.ToTable("auth_audit_logs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OccurredAt);
            e.HasIndex(x => x.EventType);
            e.HasIndex(x => x.Email);
            e.Property(x => x.EventType).HasMaxLength(64);
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(512);
            e.Property(x => x.Details).HasMaxLength(1000);
        });

        modelBuilder.Entity<ProviderUsageDaily>(e =>
        {
            e.ToTable("provider_usage_daily");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Provider, x.UsageDate }).IsUnique();
            e.Property(x => x.Provider).HasMaxLength(64);
        });

        modelBuilder.Entity<JobRegistryState>(e =>
        {
            e.ToTable("job_registry_state");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.JobKey).IsUnique();
            e.Property(x => x.JobKey).HasMaxLength(64);
            e.Property(x => x.Schedule).HasMaxLength(64);
            e.Property(x => x.MetadataJson).HasColumnType("text");
        });

        modelBuilder.Entity<OperationalError>(e =>
        {
            e.ToTable("errors");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Fingerprint);
            e.HasIndex(x => new { x.Fingerprint, x.Status });
            e.HasIndex(x => x.LastSeenAt);
            e.HasIndex(x => new { x.Severity, x.Status });
            e.HasIndex(x => new { x.Source, x.Provider });
            e.HasIndex(x => x.RequestId);
            e.Property(x => x.Fingerprint).HasMaxLength(64);
            e.Property(x => x.RequestId).HasMaxLength(64);
            e.Property(x => x.Source).HasMaxLength(32);
            e.Property(x => x.Environment).HasMaxLength(32);
            e.Property(x => x.Severity).HasMaxLength(16);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.ErrorCode).HasMaxLength(64);
            e.Property(x => x.ErrorType).HasMaxLength(128);
            e.Property(x => x.MessageSafe).HasMaxLength(2000);
            e.Property(x => x.MessageInternal).HasMaxLength(4000);
            e.Property(x => x.StackTrace).HasColumnType("text");
            e.Property(x => x.Route).HasMaxLength(512);
            e.Property(x => x.Method).HasMaxLength(16);
            e.Property(x => x.JobKey).HasMaxLength(64);
            e.Property(x => x.Provider).HasMaxLength(32);
            e.Property(x => x.ProviderRequestId).HasMaxLength(128);
            e.Property(x => x.MetadataJson).HasColumnType("text");
        });

        modelBuilder.Entity<IngestionError>(e =>
        {
            e.ToTable("ingestion_errors");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.LastSeenAt);
            e.HasIndex(x => new { x.Source, x.JobKey, x.Message });
            e.Property(x => x.Source).HasMaxLength(64);
            e.Property(x => x.JobKey).HasMaxLength(64);
            e.Property(x => x.Severity).HasMaxLength(16);
            e.Property(x => x.Message).HasMaxLength(2000);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.MetadataJson).HasColumnType("text");
            e.Property(x => x.StackTrace).HasColumnType("text");
        });

        modelBuilder.Entity<AppMetric>(e =>
        {
            e.ToTable("app_metrics");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MetricKey, x.RecordedAt });
            e.Property(x => x.MetricKey).HasMaxLength(128);
            e.Property(x => x.DimensionsJson).HasColumnType("text");
        });

        modelBuilder.Entity<ApplicationErrorLog>(e =>
        {
            e.ToTable("application_error_logs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OccurredAt);
            e.HasIndex(x => x.Source);
            e.Property(x => x.Source).HasMaxLength(32);
            e.Property(x => x.Category).HasMaxLength(128);
            e.Property(x => x.Message).HasMaxLength(1000);
            e.Property(x => x.RequestMethod).HasMaxLength(16);
            e.Property(x => x.RequestPath).HasMaxLength(512);
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
            e.Property(x => x.Message).HasMaxLength(4000);
            e.HasOne(x => x.SyncRun).WithMany().HasForeignKey(x => x.SyncRunId);
        });

        modelBuilder.Entity<StandingRow>(e =>
        {
            e.ToTable("standing_rows");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.GroupKey, x.TeamCode, x.Provider }).IsUnique();
            e.HasIndex(x => x.CompetitionSeasonId);
            e.Property(x => x.GroupKey).HasMaxLength(8);
            e.Property(x => x.TeamCode).HasMaxLength(8);
            e.Property(x => x.TeamName).HasMaxLength(100);
            e.Property(x => x.LogoUrl).HasMaxLength(512);
            e.Property(x => x.Provider).HasMaxLength(32);
            e.HasOne(x => x.CompetitionSeason).WithMany().HasForeignKey(x => x.CompetitionSeasonId);
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
            e.HasIndex(x => x.ContentHash);
            e.HasIndex(x => new { x.ProcessingStatus, x.ProcessedAt });
            e.Property(x => x.ExternalId).HasMaxLength(128);
            e.Property(x => x.Title).HasMaxLength(300);
            e.Property(x => x.SourceUrl).HasMaxLength(512);
            e.Property(x => x.AudioUrl).HasMaxLength(512);
            e.Property(x => x.Author).HasMaxLength(StringLimits.MediaAuthor);
            e.Property(x => x.Publication).HasMaxLength(StringLimits.MediaPublication);
            e.Property(x => x.ContentHash).HasMaxLength(StringLimits.ContentHash);
            e.Property(x => x.ProcessingStatus).HasMaxLength(StringLimits.ProcessingStatus);
            e.Property(x => x.ProcessingError).HasMaxLength(StringLimits.ProcessingError);
            e.HasOne(x => x.MediaSource).WithMany(s => s.Items).HasForeignKey(x => x.MediaSourceId);
        });

        modelBuilder.Entity<PunditOpinion>(e =>
        {
            e.ToTable("pundit_opinions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Team);
            e.HasIndex(x => x.PunditId);
            e.HasIndex(x => x.NeedsHumanReview);
            e.HasIndex(x => x.ReviewStatus);
            e.HasIndex(x => x.SourceItemId);
            e.Property(x => x.ReviewStatus).HasMaxLength(16);
            e.Property(x => x.ReviewNotes).HasMaxLength(2000);
            e.Property(x => x.Topic).HasMaxLength(StringLimits.OpinionTopic);
            e.Property(x => x.Team).HasMaxLength(StringLimits.OpinionTeam);
            e.Property(x => x.Player).HasMaxLength(StringLimits.OpinionPlayer);
            e.Property(x => x.MatchName).HasMaxLength(StringLimits.OpinionMatchName);
            e.Property(x => x.MatchId).HasMaxLength(64);
            e.Property(x => x.PredictionType).HasMaxLength(StringLimits.PredictionType);
            e.HasIndex(x => x.MatchId);
            e.HasOne(x => x.SourceItem).WithMany(i => i.Opinions).HasForeignKey(x => x.SourceItemId);
            e.HasOne(x => x.Pundit).WithMany(p => p.Opinions).HasForeignKey(x => x.PunditId);
            e.HasOne(x => x.Match).WithMany().HasForeignKey(x => x.MatchId).IsRequired(false);
        });

        modelBuilder.Entity<PredictionAggregate>(e =>
        {
            e.ToTable("prediction_aggregates");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EntityType, x.EntityName, x.PredictionType }).IsUnique();
            e.Property(x => x.EntityType).HasMaxLength(StringLimits.PredictionEntityType);
            e.Property(x => x.EntityName).HasMaxLength(StringLimits.PredictionEntityName);
            e.Property(x => x.PredictionType).HasMaxLength(StringLimits.PredictionType);
        });

        modelBuilder.Entity<Country>(e =>
        {
            e.ToTable("countries");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ExternalProvider, x.ExternalId }).IsUnique()
                .HasFilter("\"ExternalProvider\" IS NOT NULL AND \"ExternalId\" IS NOT NULL");
            e.HasIndex(x => x.Code);
            e.HasIndex(x => x.IsActive);
            e.Property(x => x.ExternalId).HasMaxLength(StringLimits.ExternalId);
            e.Property(x => x.ExternalProvider).HasMaxLength(32);
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.Code).HasMaxLength(8);
            e.Property(x => x.FlagUrl).HasMaxLength(512);
            e.Property(x => x.Continent).HasMaxLength(32);
            e.Property(x => x.MetadataJson).HasColumnType("text");
        });

        modelBuilder.Entity<Player>(e =>
        {
            e.ToTable("players");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ExternalProvider, x.ExternalId }).IsUnique()
                .HasFilter("\"ExternalProvider\" IS NOT NULL AND \"ExternalId\" IS NOT NULL");
            e.HasIndex(x => x.DisplayName);
            e.HasIndex(x => x.Position);
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.CountryId);
            e.Property(x => x.ExternalId).HasMaxLength(StringLimits.ExternalId);
            e.Property(x => x.ExternalProvider).HasMaxLength(32);
            e.Property(x => x.FirstName).HasMaxLength(80);
            e.Property(x => x.LastName).HasMaxLength(80);
            e.Property(x => x.DisplayName).HasMaxLength(120);
            e.Property(x => x.KnownName).HasMaxLength(120);
            e.Property(x => x.Position).HasMaxLength(16);
            e.Property(x => x.PhotoUrl).HasMaxLength(512);
            e.Property(x => x.ClubName).HasMaxLength(120);
            e.Property(x => x.NationalTeamName).HasMaxLength(120);
            e.Property(x => x.MetadataJson).HasColumnType("text");
            e.HasOne(x => x.Country).WithMany(c => c.Players).HasForeignKey(x => x.CountryId);
        });

        modelBuilder.Entity<PlayerStat>(e =>
        {
            e.ToTable("player_stats");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.PlayerId, x.CountryId, x.Competition, x.Season, x.SourceProvider }).IsUnique();
            e.Property(x => x.Competition).HasMaxLength(32);
            e.Property(x => x.Season).HasMaxLength(16);
            e.Property(x => x.SourceProvider).HasMaxLength(32);
            e.Property(x => x.Rating).HasPrecision(4, 2);
            e.Property(x => x.MetadataJson).HasColumnType("text");
            e.HasOne(x => x.Player).WithMany(p => p.Stats).HasForeignKey(x => x.PlayerId);
            e.HasOne(x => x.Country).WithMany(c => c.PlayerStats).HasForeignKey(x => x.CountryId);
        });

        modelBuilder.Entity<LeaderboardEntry>(e =>
        {
            e.ToTable("leaderboard_entries");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.LeaderboardType, x.PlayerId, x.Competition, x.Season, x.SourceProvider }).IsUnique();
            e.HasIndex(x => new { x.LeaderboardType, x.Competition, x.Season, x.Rank });
            e.Property(x => x.LeaderboardType).HasMaxLength(32);
            e.Property(x => x.Competition).HasMaxLength(32);
            e.Property(x => x.Season).HasMaxLength(16);
            e.Property(x => x.SourceProvider).HasMaxLength(32);
            e.Property(x => x.Value).HasPrecision(10, 2);
            e.Property(x => x.MetadataJson).HasColumnType("text");
            e.HasOne(x => x.Player).WithMany(p => p.LeaderboardEntries).HasForeignKey(x => x.PlayerId);
            e.HasOne(x => x.Country).WithMany(c => c.LeaderboardEntries).HasForeignKey(x => x.CountryId);
        });

        modelBuilder.Entity<UserPrediction>(e =>
        {
            e.ToTable("user_predictions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.PredictionType, x.Competition, x.Season }).IsUnique();
            e.HasIndex(x => x.PredictionType);
            e.Property(x => x.PredictionType).HasMaxLength(32);
            e.Property(x => x.Competition).HasMaxLength(32);
            e.Property(x => x.Season).HasMaxLength(16);
            e.Property(x => x.PredictionValue).HasMaxLength(200);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Country).WithMany(c => c.UserPredictions).HasForeignKey(x => x.CountryId);
            e.HasOne(x => x.Player).WithMany(p => p.UserPredictions).HasForeignKey(x => x.PlayerId);
        });
    }
}
