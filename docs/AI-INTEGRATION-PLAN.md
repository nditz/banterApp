# AI Integration Plan

BanterApp uses AI in three places: **rolling feed reactions**, **broadcast scripts** (Content Studio), and **user-triggered banter** (memes, analysis). This document outlines the phased rollout and required configuration.

## Architecture

```
┌─────────────────┐     every 5 min      ┌──────────────────┐
│  ScoreSyncJob   │ ───────────────────► │  matches table   │
│  (live scores)  │   API-Football live  │  fixtures/scores │
└─────────────────┘                      └────────┬─────────┘
                                                  │
┌─────────────────┐     every 120 min             ▼
│ NewsIngestJob   │ ───────────────────► ┌──────────────────┐
│ sports news +   │   NewsAPI / scrapers │ news_feed_items  │
│ match desk      │   + match fixtures   │ sports_news,     │
└─────────────────┘                      │ match_fixture,   │
                                         │ match_result,    │
                                         │ match_live       │
                                                  │
┌─────────────────┐     every 20 min              ▼
│ AiReactionJob   │ ───────────────────► ┌──────────────────┐
│ pundit posts    │   IContentGenerator  │ ai_reaction items│
└─────────────────┘                      └──────────────────┘

User-facing (on demand):
  POST /api/ai/broadcast-script  →  TV journalist script
  POST /api/ai/*                 →  banter, memes, analysis
```

Jobs are **independent** Hangfire recurring tasks with **staggered** cron schedules so they rarely collide. Configure intervals in `BackgroundJobs` (see `appsettings.json`).

## Phase 1 (current)

| Component | Implementation |
|-----------|----------------|
| AI provider | `StubContentGenerator` — template strings, no API cost |
| Feed reactions | `AiReactionJob` writes `ai_reaction` items linked via `ParentItemId` |
| Live scores | `ScoreSyncJob` merges `fixtures?live=all` from API-Football |
| News ingest | `NewsIngestJob` — NewsAPI + match desk items |

## Phase 2 — OpenAI / ChatGPT

1. Add `OpenAiContentGenerator : IContentGenerator` behind the existing interface.
2. Set `Ai:Provider` to `openai` and `Ai:ApiKey` (or env `AI_API_KEY`).
3. Use `Ai:Model` (`gpt-4o-mini` for cost, `gpt-4o` for quality).
4. Map `GenerateNewsReactionAsync` to Chat Completions with `Ai:NewsReactionSystemPrompt`.
5. Map `GenerateBroadcastScript` (already in `BroadcastScriptComposer`) to optionally call the LLM for richer copy.
6. Add token budgeting per anonymous user (`Ai:AnonymousGenerationLimit`).

### Recommended OpenAI settings

| Setting | Value | Notes |
|---------|-------|-------|
| Model | `gpt-4o-mini` | ~$0.15/1M input tokens — fine for short reactions |
| MaxTokens | 512 | Enough for 2–3 sentence reactions + scripts |
| Temperature | 0.85 | Slightly creative pundit voice |
| System prompt | See `Ai:NewsReactionSystemPrompt` | Tune per personality |

## Phase 3 — Multi-provider & personality

- `Ai:Provider` = `anthropic` → Claude for longer analysis scripts.
- **Personality packs**: store prompt variants (Gary Neville, Stephen A., casual TikToker) in config or DB.
- **Trending linguo**: ingest YouTube/podcast transcripts (`NewsIngest:YouTubeChannelIds`) and few-shot them into the system prompt so reactions mirror online personalities.
- **Video attachment**: link generated scripts to user-uploaded clips in Content Studio.

## Phase 4 — YouTube & podcast ingestion

`NewsIngestJob` already has placeholder arrays:

```json
"NewsIngest": {
  "YouTubeChannelIds": ["UCxxxx"],
  "PodcastFeedUrls": ["https://feeds.example.com/podcast.rss"]
}
```

Implementation path:
1. YouTube Data API v3 → list recent videos → transcript API (or `youtube-transcript-api` style scraper where ToS allows).
2. Podcast RSS → episode audio URL → Whisper API transcription.
3. Store as `Category: "transcript"` feed items → `AiReactionJob` summarises and reacts.

## Configuration reference

All settings live in `appsettings.json` (overridable via environment variables):

| Key | Env override | Purpose |
|-----|--------------|---------|
| `SportsData:Provider` | `SPORTS_API_PROVIDER` | `mock` or `apifootball` |
| `SportsData:ApiKey` | `SPORTS_API_KEY` | API-Football key |
| `NewsIngest:*` | — | What to pull into rolling news |
| `BackgroundJobs:*` | — | Staggered job intervals |
| `Ai:Provider` | `AI_PROVIDER` | `stub`, `openai`, `anthropic` |
| `Ai:ApiKey` | `AI_API_KEY` | LLM API key |
| `Ai:Model` | — | e.g. `gpt-4o-mini` |
| `Ai:BaseUrl` | — | Azure / local proxy |
| `NEWS_API_KEY` | env only | NewsAPI.org |

## Live score APIs (researched)

| API | Endpoint | Free tier | Used by |
|-----|----------|-----------|---------|
| **API-Football** | `GET /fixtures?live=all` | 100 req/day | `ScoreSyncJob` + `NewsIngestJob` |
| football-data.org | `GET /matches/LIVE` | 10 req/min | Future fallback |
| TheSportsDB | events live | 30 req/min | Metadata only |

At 5-minute polling, API-Football free tier is exceeded (~288 req/day). Options:
- Raise `BackgroundJobs:LiveScoresIntervalMinutes` to 15 (96/day), or
- Upgrade to API-Football Pro ($19/mo).

## Monitoring

Development: Hangfire dashboard at `http://localhost:5000/hangfire` shows all three job schedules, last run, and failures.
