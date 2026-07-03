using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.UserPredictions;

public sealed class UserPredictionValidator(AppDbContext db)
{
    public async Task<(bool IsValid, string? Error)> ValidateCreateOrUpdateAsync(
        string predictionType,
        Guid? countryId,
        Guid? playerId,
        string? competition,
        string? season,
        UserPrediction? existing,
        CancellationToken cancellationToken = default)
    {
        if (!UserPredictionTypes.All.Contains(predictionType))
        {
            return (false, "Invalid prediction type.");
        }

        if (existing?.IsLocked == true)
        {
            return (false, "This prediction is locked and cannot be edited.");
        }

        if (UserPredictionTypes.RequiresCountry(predictionType))
        {
            if (countryId is null)
            {
                return (false, "A country is required for this prediction type.");
            }

            var country = await db.Countries.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == countryId, cancellationToken);
            if (country is null)
            {
                return (false, "Country not found.");
            }

            if (!country.IsActive)
            {
                return (false, "That country is not available for selection.");
            }
        }

        if (UserPredictionTypes.RequiresPlayer(predictionType))
        {
            if (playerId is null)
            {
                return (false, "A player is required for this prediction type.");
            }

            var player = await db.Players.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);
            if (player is null)
            {
                return (false, "Player not found.");
            }

            if (!player.IsActive)
            {
                return (false, "That player is not available for selection.");
            }
        }

        if (existing is null && !string.IsNullOrWhiteSpace(competition) && !string.IsNullOrWhiteSpace(season))
        {
            // Duplicate check handled at endpoint level with user id
        }

        return (true, null);
    }
}
