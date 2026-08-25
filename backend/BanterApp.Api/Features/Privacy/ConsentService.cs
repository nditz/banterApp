using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.Privacy;

public interface IConsentService
{
    Task<ConsentState> GetAsync(IUserContext user, CancellationToken ct = default);

    Task<ConsentState> SaveAsync(
        IUserContext user,
        bool analyticsAllowed,
        bool marketingAllowed,
        CancellationToken ct = default);

    /// <summary>
    /// Whether the caller has an active, current-version grant for product analytics.
    /// A caller with no identity or no stored record is never treated as consenting.
    /// </summary>
    Task<bool> IsAnalyticsAllowedAsync(IUserContext user, CancellationToken ct = default);
}

public sealed class ConsentService(
    AppDbContext db,
    IOptions<PrivacyOptions> options) : IConsentService
{
    private readonly PrivacyOptions _options = options.Value;

    public async Task<ConsentState> GetAsync(IUserContext user, CancellationToken ct = default)
    {
        var record = await FindAsync(user, tracked: false, ct);

        if (record is null)
        {
            return ConsentState.NotRecorded(_options);
        }

        return new ConsentState(
            true,
            record.ConsentVersion,
            record.AnalyticsAllowed && _options.AnalyticsEnabled,
            record.MarketingAllowed && _options.MarketingCategoryEnabled,
            record.UpdatedAt,
            IsCurrentVersion(record.ConsentVersion),
            _options.ConsentVersion,
            _options.AnalyticsEnabled,
            _options.MarketingCategoryEnabled);
    }

    public async Task<ConsentState> SaveAsync(
        IUserContext user,
        bool analyticsAllowed,
        bool marketingAllowed,
        CancellationToken ct = default)
    {
        // A category that is disabled for the environment can never be granted, even if
        // the client asks for it.
        var analytics = analyticsAllowed && _options.AnalyticsEnabled;
        var marketing = marketingAllowed && _options.MarketingCategoryEnabled;

        var now = DateTimeOffset.UtcNow;
        var record = await FindAsync(user, tracked: true, ct);

        if (record is null)
        {
            if (user.UserId is null && user.AnonymousUserId is null)
            {
                // Nothing to key the record on. The browser keeps its local mirror and
                // the choice is persisted once the session is identified.
                return new ConsentState(
                    false,
                    _options.ConsentVersion,
                    analytics,
                    marketing,
                    now,
                    true,
                    _options.ConsentVersion,
                    _options.AnalyticsEnabled,
                    _options.MarketingCategoryEnabled);
            }

            record = new ConsentPreference
            {
                Id = Guid.NewGuid(),
                UserId = user.UserId,
                AnonymousUserId = user.UserId is null ? user.AnonymousUserId : null,
                CreatedAt = now
            };
            db.ConsentPreferences.Add(record);
        }

        record.ConsentVersion = _options.ConsentVersion;
        record.AnalyticsAllowed = analytics;
        record.MarketingAllowed = marketing;
        record.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        return new ConsentState(
            true,
            _options.ConsentVersion,
            analytics,
            marketing,
            now,
            true,
            _options.ConsentVersion,
            _options.AnalyticsEnabled,
            _options.MarketingCategoryEnabled);
    }

    public async Task<bool> IsAnalyticsAllowedAsync(IUserContext user, CancellationToken ct = default)
    {
        if (!_options.AnalyticsEnabled)
        {
            return false;
        }

        var record = await FindAsync(user, tracked: false, ct);
        return record is not null &&
               record.AnalyticsAllowed &&
               IsCurrentVersion(record.ConsentVersion);
    }

    private Task<ConsentPreference?> FindAsync(IUserContext user, bool tracked, CancellationToken ct)
    {
        var query = tracked
            ? db.ConsentPreferences.AsQueryable()
            : db.ConsentPreferences.AsNoTracking();

        if (user.UserId is { } userId)
        {
            return query.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        }

        if (user.AnonymousUserId is { } anonymousId)
        {
            return query.FirstOrDefaultAsync(c => c.AnonymousUserId == anonymousId, ct);
        }

        return Task.FromResult<ConsentPreference?>(null);
    }

    private bool IsCurrentVersion(string version) =>
        string.Equals(version, _options.ConsentVersion, StringComparison.Ordinal);
}

public sealed record ConsentState(
    bool Recorded,
    string ConsentVersion,
    bool AnalyticsAllowed,
    bool MarketingAllowed,
    DateTimeOffset? UpdatedAt,
    bool IsCurrentVersion,
    string CurrentConsentVersion,
    bool AnalyticsCategoryEnabled,
    bool MarketingCategoryEnabled)
{
    public static ConsentState NotRecorded(PrivacyOptions options) =>
        new(
            false,
            options.ConsentVersion,
            false,
            false,
            null,
            false,
            options.ConsentVersion,
            options.AnalyticsEnabled,
            options.MarketingCategoryEnabled);
}
