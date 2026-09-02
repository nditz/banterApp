namespace BanterApp.Api.Integrations.Banter;

public enum BanterScenario
{
    Overconfidence,
    Heartbreak,
    Comeback,
    BottledIt,
    SmugWinner,
    LuckyEscape,
    DominantWin,
    UnderdogUpset,
    LastMinuteDrama,
    DrawFrustration,
    PredictionAgedBadly,
    PredictionNailedIt,
    RivalMockery,
    RefereeControversy,
    GenericWin,
    GenericDraw,
    GenericLoss,
    GenericNews
}

public enum BanterContentType
{
    Gif,
    Image,
    MemeTemplate
}

public enum MatchOutcomeKind
{
    Unknown,
    HomeWin,
    AwayWin,
    Draw
}

public enum PredictionOutcomeKind
{
    Unknown,
    HomeWin,
    AwayWin,
    Draw
}
