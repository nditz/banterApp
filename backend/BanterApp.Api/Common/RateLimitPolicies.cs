namespace BanterApp.Api.Common;

public static class RateLimitPolicies
{
    public const string Api = "api";
    public const string Write = "write";
    public const string Auth = "auth";

    public const string AuthLogin = "auth.login";
    public const string AuthSignup = "auth.signup";
    public const string AuthPasswordReset = "auth.password_reset";
    public const string AuthSession = "auth.session";

    public const string PublicFeed = "public.feed";
    public const string PublicSearch = "public.search";
    public const string PublicArticle = "public.article";
    public const string PublicPredictions = "public.predictions";
    public const string PublicReactions = "public.comments_or_reactions";

    public const string OpenAiGenerate = "openai.banter.generate";

    public const string AdminJobsRun = "admin.jobs.run";
    public const string AdminJobsPauseResume = "admin.jobs.pause_resume";
    public const string AdminErrorsRetry = "admin.errors.retry";
    public const string AdminReviewUpdate = "admin.review.update";
    public const string AdminUsersManage = "admin.users.manage";

    public const string AnalyticsIngest = "analytics.ingest";
    public const string ConsentUpdate = "consent.update";

    public const string RssSyncTrigger = "rss.sync.trigger";
    public const string YoutubeSyncTrigger = "youtube.sync.trigger";
    public const string ClientErrorReport = "client.error.report";
}
