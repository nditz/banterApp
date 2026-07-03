using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.FootballReference;
using BanterApp.Api.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.UserPredictions;

public static class UserPredictionEndpoints
{
    public static IEndpointRouteBuilder MapUserPredictionEndpoints(this IEndpointRouteBuilder app)
    {
        var userGroup = app.MapGroup("/api/user/predictions").WithTags("User Predictions");
        userGroup.MapGet("/", GetUserPredictions).RequireAuthorization();
        userGroup.MapPost("/", CreateUserPrediction)
            .RequireAuthorization()
            .RequireRateLimiting("write")
            .WithValidation<CreateUserPredictionRequest>();
        userGroup.MapPut("/{id:guid}", UpdateUserPrediction)
            .RequireAuthorization()
            .RequireRateLimiting("write")
            .WithValidation<UpdateUserPredictionRequest>();

        app.MapGet("/api/predictions/aggregates", GetAggregates)
            .WithTags("User Predictions")
            .RequireRateLimiting(RateLimitPolicies.PublicSearch);

        return app;
    }

    private static async Task<IResult> GetUserPredictions(
        AppDbContext db,
        IUserContext user,
        UserPredictionLockService lockService,
        IOptions<FootballReferenceDataOptions> options,
        CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.UserId is null)
        {
            return Results.Unauthorized();
        }

        await lockService.LockPredictionsIfNeededAsync(ct);
        var deadline = await lockService.GetLockDeadlineAsync(ct);
        var isLocked = deadline is not null && deadline <= DateTimeOffset.UtcNow;

        var comp = options.Value.CompetitionCode;
        var season = options.Value.Season;

        var predictions = await db.UserPredictions
            .AsNoTracking()
            .Where(p => p.UserId == user.UserId &&
                        p.Competition == comp &&
                        p.Season == season)
            .Select(p => new UserPredictionResponse(
                p.Id,
                p.PredictionType,
                p.CountryId,
                p.Country != null ? p.Country.Name : null,
                p.Country != null ? p.Country.FlagUrl : null,
                p.PlayerId,
                p.Player != null ? p.Player.DisplayName : null,
                p.Player != null ? p.Player.PhotoUrl : null,
                p.Competition,
                p.Season,
                p.Confidence,
                p.IsLocked,
                p.LockedAt,
                p.CreatedAt,
                p.UpdatedAt))
            .ToListAsync(ct);

        return Results.Ok(new UserPredictionsStatusResponse(
            isLocked,
            deadline,
            !isLocked,
            comp,
            season,
            BuildCategoryInfos(predictions)));
    }

    private static async Task<IResult> CreateUserPrediction(
        CreateUserPredictionRequest request,
        AppDbContext db,
        IUserContext user,
        UserPredictionValidator validator,
        UserPredictionLockService lockService,
        IOptions<FootballReferenceDataOptions> options,
        TurnstileService turnstile,
        HttpContext http,
        CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.UserId is null)
        {
            return Results.Unauthorized();
        }

        await lockService.LockPredictionsIfNeededAsync(ct);
        var deadline = await lockService.GetLockDeadlineAsync(ct);
        if (deadline is not null && deadline <= DateTimeOffset.UtcNow)
        {
            return Results.BadRequest(new { error = "Predictions are locked." });
        }

        var ip = http.Connection.RemoteIpAddress?.ToString();
        if (!await turnstile.VerifyAsync(request.TurnstileToken, ip, ct))
        {
            return Results.BadRequest(new { error = "Human verification failed." });
        }

        var comp = request.Competition ?? options.Value.CompetitionCode;
        var season = request.Season ?? options.Value.Season;
        var predictionType = request.PredictionType.Trim().ToLowerInvariant();

        var existing = await db.UserPredictions.FirstOrDefaultAsync(
            p => p.UserId == user.UserId &&
                 p.PredictionType == predictionType &&
                 p.Competition == comp &&
                 p.Season == season,
            ct);

        if (existing is not null)
        {
            return Results.Conflict(new { error = "You already have a prediction for this category." });
        }

        var (isValid, error) = await validator.ValidateCreateOrUpdateAsync(
            predictionType, request.CountryId, request.PlayerId, comp, season, null, ct);
        if (!isValid)
        {
            return Results.BadRequest(new { error });
        }

        var prediction = new UserPrediction
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId.Value,
            PredictionType = predictionType,
            CountryId = request.CountryId,
            PlayerId = request.PlayerId,
            Competition = comp,
            Season = season,
            PredictionValue = request.PredictionValue,
            Confidence = request.Confidence
        };

        db.UserPredictions.Add(prediction);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/user/predictions/{prediction.Id}", await MapResponseAsync(db, prediction.Id, ct));
    }

    private static async Task<IResult> UpdateUserPrediction(
        Guid id,
        UpdateUserPredictionRequest request,
        AppDbContext db,
        IUserContext user,
        UserPredictionValidator validator,
        UserPredictionLockService lockService,
        TurnstileService turnstile,
        HttpContext http,
        CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.UserId is null)
        {
            return Results.Unauthorized();
        }

        await lockService.LockPredictionsIfNeededAsync(ct);

        var ip = http.Connection.RemoteIpAddress?.ToString();
        if (!await turnstile.VerifyAsync(request.TurnstileToken, ip, ct))
        {
            return Results.BadRequest(new { error = "Human verification failed." });
        }

        var prediction = await db.UserPredictions.FirstOrDefaultAsync(
            p => p.Id == id && p.UserId == user.UserId, ct);

        if (prediction is null)
        {
            return Results.NotFound();
        }

        if (prediction.IsLocked)
        {
            return Results.BadRequest(new { error = "This prediction is locked." });
        }

        var countryId = request.CountryId ?? prediction.CountryId;
        var playerId = request.PlayerId ?? prediction.PlayerId;

        var (isValid, error) = await validator.ValidateCreateOrUpdateAsync(
            prediction.PredictionType, countryId, playerId,
            prediction.Competition, prediction.Season, prediction, ct);
        if (!isValid)
        {
            return Results.BadRequest(new { error });
        }

        prediction.CountryId = countryId;
        prediction.PlayerId = playerId;
        prediction.Confidence = request.Confidence ?? prediction.Confidence;
        prediction.PredictionValue = request.PredictionValue ?? prediction.PredictionValue;
        prediction.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(await MapResponseAsync(db, prediction.Id, ct));
    }

    private static async Task<IResult> GetAggregates(
        UserPredictionAggregateService aggregates,
        string? type,
        string? competition,
        string? season,
        CancellationToken ct)
    {
        var result = await aggregates.GetAggregatesAsync(type, competition, season, ct);
        return Results.Ok(result);
    }

    private static async Task<UserPredictionResponse> MapResponseAsync(
        AppDbContext db,
        Guid id,
        CancellationToken ct)
    {
        return await db.UserPredictions
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new UserPredictionResponse(
                p.Id,
                p.PredictionType,
                p.CountryId,
                p.Country != null ? p.Country.Name : null,
                p.Country != null ? p.Country.FlagUrl : null,
                p.PlayerId,
                p.Player != null ? p.Player.DisplayName : null,
                p.Player != null ? p.Player.PhotoUrl : null,
                p.Competition,
                p.Season,
                p.Confidence,
                p.IsLocked,
                p.LockedAt,
                p.CreatedAt,
                p.UpdatedAt))
            .FirstAsync(ct);
    }

    private static IReadOnlyList<PredictionCategoryInfo> BuildCategoryInfos(
        IReadOnlyList<UserPredictionResponse> predictions)
    {
        return UserPredictionTypes.All.Select(type =>
        {
            var pick = predictions.FirstOrDefault(p =>
                string.Equals(p.PredictionType, type, StringComparison.OrdinalIgnoreCase));
            return new PredictionCategoryInfo(
                type,
                UserPredictionTypeLabels.Label(type),
                UserPredictionTypeLabels.Description(type),
                UserPredictionTypes.RequiresCountry(type),
                UserPredictionTypes.RequiresPlayer(type),
                pick);
        }).ToList();
    }
}

public static class UserPredictionTypeLabels
{
    public static string Label(string type) => type switch
    {
        UserPredictionTypes.WinnerCountry => "Winning Country",
        UserPredictionTypes.FinalistCountry => "Finalist Country",
        UserPredictionTypes.BestPlayer => "Best Player",
        UserPredictionTypes.TopGoalScorer => "Top Goal Scorer",
        UserPredictionTypes.TopAssistProvider => "Top Assist Provider",
        UserPredictionTypes.GoldenBoot => "Golden Boot",
        UserPredictionTypes.BestYoungPlayer => "Best Young Player",
        UserPredictionTypes.PlayerOfTournament => "Player of the Tournament",
        _ => type
    };

    public static string Description(string type) => type switch
    {
        UserPredictionTypes.WinnerCountry => "Which country wins the tournament?",
        UserPredictionTypes.FinalistCountry => "Which country reaches the final?",
        UserPredictionTypes.BestPlayer => "Who is the best player of the tournament?",
        UserPredictionTypes.TopGoalScorer => "Who finishes as top goal scorer?",
        UserPredictionTypes.TopAssistProvider => "Who leads in assists?",
        UserPredictionTypes.GoldenBoot => "Who wins the Golden Boot?",
        UserPredictionTypes.BestYoungPlayer => "Who is the best young player?",
        UserPredictionTypes.PlayerOfTournament => "Who wins Player of the Tournament?",
        _ => string.Empty
    };
}

public sealed record UserPredictionResponse(
    Guid Id,
    string PredictionType,
    Guid? CountryId,
    string? CountryName,
    string? CountryFlagUrl,
    Guid? PlayerId,
    string? PlayerName,
    string? PlayerPhotoUrl,
    string? Competition,
    string? Season,
    int? Confidence,
    bool IsLocked,
    DateTimeOffset? LockedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PredictionCategoryInfo(
    string PredictionType,
    string Label,
    string Description,
    bool RequiresCountry,
    bool RequiresPlayer,
    UserPredictionResponse? Pick);

public sealed record UserPredictionsStatusResponse(
    bool IsLocked,
    DateTimeOffset? LockDeadline,
    bool CanEdit,
    string Competition,
    string Season,
    IReadOnlyList<PredictionCategoryInfo> Categories);

public record CreateUserPredictionRequest(
    string PredictionType,
    Guid? CountryId,
    Guid? PlayerId,
    string? Competition,
    string? Season,
    string? PredictionValue,
    int? Confidence,
    string? TurnstileToken);

public sealed class CreateUserPredictionValidator : AbstractValidator<CreateUserPredictionRequest>
{
    public CreateUserPredictionValidator()
    {
        RuleFor(x => x.PredictionType).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Confidence).InclusiveBetween(1, 100).When(x => x.Confidence.HasValue);
    }
}

public record UpdateUserPredictionRequest(
    Guid? CountryId,
    Guid? PlayerId,
    string? PredictionValue,
    int? Confidence,
    string? TurnstileToken);

public sealed class UpdateUserPredictionValidator : AbstractValidator<UpdateUserPredictionRequest>
{
    public UpdateUserPredictionValidator()
    {
        RuleFor(x => x.Confidence).InclusiveBetween(1, 100).When(x => x.Confidence.HasValue);
    }
}
