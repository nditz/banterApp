namespace BanterApp.Api.Integrations.FootballBanter;

public static class FootballBanterConfigValidator
{
    public static FootballBanterConfigLoadResult ValidateAndApplyDefaults(FootballBanterConfig config)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(config.Version))
        {
            config.Version = "1.0.0";
            warnings.Add("version was missing; defaulted to 1.0.0.");
        }

        if (string.IsNullOrWhiteSpace(config.Feature))
        {
            config.Feature = "football_banter_engine";
        }

        ApplyYouTubeDefaults(config.Sources.YouTube, errors, warnings);
        ApplyRssDefaults(config.Sources.Rss, errors, warnings);
        ApplyOpenAiDefaults(config.OpenAi, warnings);
        ApplyBanterDefaults(config.Banter, warnings);
        ApplyReviewDefaults(config.ReviewRules);

        if (config.ContentRules.QuotePolicy.StatementTypes.Count == 0)
        {
            config.ContentRules.QuotePolicy.StatementTypes =
            [
                "direct_quote",
                "paraphrase",
                "ai_summary",
                "inferred_prediction"
            ];
        }

        return new FootballBanterConfigLoadResult
        {
            Config = config,
            Errors = errors,
            Warnings = warnings
        };
    }

    private static void ApplyYouTubeDefaults(
        FootballBanterYouTubeSourceConfig youtube,
        List<string> errors,
        List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(youtube.ApiBaseUrl))
        {
            youtube.ApiBaseUrl = "https://www.googleapis.com/youtube/v3";
            warnings.Add("sources.youtube.api_base_url was missing; defaulted to YouTube v3 base URL.");
        }

        if (youtube.MaxResultsPerSearch <= 0)
        {
            youtube.MaxResultsPerSearch = 25;
        }

        if (youtube.SyncIntervalMinutes <= 0)
        {
            youtube.SyncIntervalMinutes = 180;
        }

        if (youtube.Enabled && youtube.SearchTerms.Count == 0)
        {
            warnings.Add("sources.youtube.search_terms is empty while YouTube ingest is enabled.");
        }
    }

    private static void ApplyRssDefaults(
        FootballBanterRssSourceConfig rss,
        List<string> errors,
        List<string> warnings)
    {
        if (rss.SyncIntervalMinutes <= 0)
        {
            rss.SyncIntervalMinutes = 30;
        }

        if (rss.DedupeStrategy.Count == 0)
        {
            rss.DedupeStrategy = ["guid", "link", "content_hash"];
        }

        if (!rss.Enabled)
        {
            return;
        }

        if (rss.Feeds.Count == 0)
        {
            rss.Enabled = false;
            warnings.Add("sources.rss.feeds is empty; RSS ingest has been disabled.");
            return;
        }

        for (var i = 0; i < rss.Feeds.Count; i++)
        {
            var feed = rss.Feeds[i];
            if (string.IsNullOrWhiteSpace(feed.Url))
            {
                errors.Add($"sources.rss.feeds[{i}].url is required.");
            }

            if (string.IsNullOrWhiteSpace(feed.SourceName))
            {
                if (!string.IsNullOrWhiteSpace(feed.Name))
                {
                    feed.SourceName = feed.Name;
                    warnings.Add($"sources.rss.feeds[{i}].source_name was missing; defaulted to name.");
                }
                else
                {
                    errors.Add($"sources.rss.feeds[{i}].source_name is required.");
                }
            }

            if (string.IsNullOrWhiteSpace(feed.Name) && !string.IsNullOrWhiteSpace(feed.SourceName))
            {
                feed.Name = feed.SourceName;
            }
        }
    }

    private static void ApplyOpenAiDefaults(FootballBanterOpenAiConfig openAi, List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(openAi.Model))
        {
            openAi.Model = "gpt-4o-mini";
            warnings.Add("openai.model was missing; defaulted to gpt-4o-mini.");
        }

        if (openAi.MaxOutputTokens <= 0)
        {
            openAi.MaxOutputTokens = 1800;
        }

        if (openAi.Temperature is < 0 or > 2)
        {
            openAi.Temperature = 0.8;
            warnings.Add("openai.temperature was out of range; defaulted to 0.8.");
        }
    }

    private static void ApplyBanterDefaults(FootballBanterStyleConfig banter, List<string> warnings)
    {
        var min = 1;
        var max = 10;
        if (banter.AllowedIntensityRange.Count >= 2)
        {
            min = banter.AllowedIntensityRange[0];
            max = banter.AllowedIntensityRange[1];
        }
        else
        {
            banter.AllowedIntensityRange = [1, 10];
            warnings.Add("banter.allowed_intensity_range was missing; defaulted to [1, 10].");
        }

        if (banter.DefaultIntensity < min || banter.DefaultIntensity > max)
        {
            banter.DefaultIntensity = Math.Clamp(7, min, max);
            warnings.Add("banter.default_intensity was out of range; clamped to allowed range.");
        }

        if (banter.Tone.Count == 0)
        {
            banter.Tone = ["fun", "gen_z", "football_twitter", "group_chat", "banter"];
        }
    }

    private static void ApplyReviewDefaults(FootballBanterReviewRulesConfig review)
    {
        if (review.NeedsHumanReviewWhen.Count > 0)
        {
            return;
        }

        review.NeedsHumanReviewWhen =
        [
            "pundit_name_missing",
            "source_text_incomplete",
            "quote_is_inferred_not_direct",
            "confidence_below_0_7",
            "multiple_speakers_mixed",
            "prediction_is_vague",
            "source_url_missing"
        ];
    }
}
