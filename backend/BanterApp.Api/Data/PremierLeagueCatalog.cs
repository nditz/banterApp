namespace BanterApp.Api.Data;

public static class PremierLeagueCatalog
{
    public const string Name = "Premier League";
    public const string Slug = "premier-league";
    public const string Code = "PL";
    public const string CountryCode = "GB";
    public const string Provider = "api_football";
    public const string ProviderCompetitionId = "39";
    public const int ApiFootballLeagueId = 39;
    public const int CurrentSeasonStartYear = 2026;
    public const string CurrentSeasonName = "2026/27";

    public static readonly Guid CompetitionId = Guid.Parse("00000000-0000-0000-0000-000000000039");
    public static readonly Guid SeasonId = Guid.Parse("00000000-0000-0000-2026-000000000039");
}
