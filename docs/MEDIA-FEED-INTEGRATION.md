# Media & news feed integration — setup guide

This guide covers configuring **news RSS**, **podcast RSS**, and **YouTube** sources so users can compare their picks against pro takes — with **source attribution** on every cited personality.

## What works today vs next

| Stage | Status | What you get |
|-------|--------|--------------|
| News RSS ingest | **Live** | Headlines in main feed panel via `NewsIngestJob` + `News:RssFeedUrls` |
| Podcast / YouTube / website RSS discovery | **Live** | Episodes & videos stored in `media_items` via `MediaIngestJob` |
| AI feed reactions | **Live** (needs `Ai:ApiKey`) | Pundit-style banter on news items |
| LLM prediction + soundbite extraction | **Live** | `PunditExtractionJob` → `pundit_opinions` + `prediction_aggregates` via `PunditIngest` |
| User vs pro compare | **Partial** | Studio `vs_pundits` tab + feed contrast when predictions exist |

Soundbites and quotables are stored in:

- `pundit_opinions` with `evidence_quote`, `source_url` (via `media_items`), and `needs_human_review`
- `prediction_aggregates` for consensus queries (`GET /api/predictions/pundits`)
- Optional bridge to `pundit_predictions` for Studio compare (future)

Attribution is enforced in `PunditDisplayResolver` — licensed/scraped takes always show outlet + platform + link.

### Pundit opinion pipeline (new)

Configure **`PunditIngest`** for RSS + YouTube keyword search, then trigger sync:

```powershell
Invoke-RestMethod -Method Post http://localhost:5000/api/integrations/rss/sync
Invoke-RestMethod -Method Post http://localhost:5000/api/integrations/youtube/sync
Invoke-RestMethod http://localhost:5000/api/opinions?team=England
Invoke-RestMethod http://localhost:5000/api/opinions?needsReview=true
Invoke-RestMethod http://localhost:5000/api/predictions/pundits?team=Brazil
```

Hangfire jobs: `rss-opinion-sync`, `youtube-opinion-sync`, `pundit-content-enrich`, `pundit-extraction`, `prediction-aggregate-refresh`.

## Quick start (local)

```powershell
cd C:\banterapp
copy backend\BanterApp.Api\appsettings.Development.json.example backend\BanterApp.Api\appsettings.Development.json
# Edit appsettings.Development.json — DB connection + API keys below
.\scripts\run-migrations.ps1
.\scripts\run-api.ps1
```

Hangfire dashboard: `http://localhost:5000/hangfire`

Manual triggers:

```powershell
Invoke-RestMethod -Method Post http://localhost:5000/api/sync/trigger/news-ingest
Invoke-RestMethod -Method Post http://localhost:5000/api/sync/trigger/media-ingest
```

Check status:

```powershell
Invoke-RestMethod http://localhost:5000/api/sync/status
```

## Required API keys

| Key | Required for | Get it from |
|-----|--------------|-------------|
| `ConnectionStrings:DefaultConnection` | All persisted feed data | Supabase session pooler (5432) |
| `News:ApiKey` | NewsAPI.org headlines (optional — RSS works without it) | https://newsapi.org |
| `YouTube:ApiKey` | YouTube channel video discovery | Google Cloud Console → YouTube Data API v3 |
| `Ai:ApiKey` | Feed AI reactions + future prediction extraction | OpenAI (or compatible endpoint) |
| `SportsData:ApiKey` | Live fixtures/results in feed (optional locally) | API-Football |

RSS-only testing works **without** NewsAPI or YouTube keys. Podcast and website sources need no API key.

## Configuration sections

### 1. Main news panel — `News` + `NewsIngest`

**`News:RssFeedUrls`** — polled by `CompositeNewsProvider` for the live feed API.

**`NewsIngest`** — background job writes articles + match desk items to `news_feed_items`.

```json
"News": {
  "ApiKey": "",
  "RssFeedUrls": [
    "https://feeds.bbci.co.uk/sport/football/world-cup/rss.xml",
    "https://www.espn.com/espn/rss/soccer/news"
  ]
},
"NewsIngest": {
  "Enabled": true,
  "MaxArticlesPerRun": 25,
  "IncludeMatchFixtures": true,
  "IncludeMatchResults": true,
  "IncludeLiveScores": true
}
```

Dev tip: set `BackgroundJobs:NewsIngestIntervalMinutes` to `30` for faster refresh.

### 2. Pro takes — `MediaIngest`

Three source types feed the pundit pipeline:

#### Podcasts (`PodcastSources`)

Use **named** entries so attribution shows the show name, not a raw RSS URL.

```json
"PodcastSources": [
  {
    "Name": "Football Weekly",
    "RssUrl": "https://www.theguardian.com/football/series/footballweekly/podcast.xml",
    "SiteUrl": "https://www.theguardian.com/football/series/footballweekly",
    "StyleSlug": "silky-studio",
    "ExtractPredictions": true
  }
]
```

#### YouTube (`YouTubeChannels`)

Requires `YouTube:ApiKey`. Each channel needs a **display name** for citations.

```json
"YouTube": {
  "ApiKey": "YOUR_GOOGLE_API_KEY"
},
"YouTubeChannels": [
  {
    "Name": "CBS Sports Golazo",
    "ChannelId": "UCxxxxxxxx",
    "SiteUrl": "https://www.youtube.com/@CBSGolazo",
    "StyleSlug": "hot-take-desk",
    "ExtractPredictions": true
  }
]
```

**Finding a channel ID:** open the channel page → View Page Source → search for `"channelId"` or use [YouTube Data API channels.list](https://developers.google.com/youtube/v3/docs/channels/list).

#### Website RSS (`WebsiteSources`)

Article predictions from permitted RSS feeds (no HTML scraping yet).

```json
"WebsiteSources": [
  {
    "Name": "BBC Sport World Cup",
    "Type": "website",
    "RssUrl": "https://feeds.bbci.co.uk/sport/football/world-cup/rss.xml",
    "BaseUrl": "https://www.bbc.co.uk/sport/football/world-cup",
    "CrawlAllowed": true,
    "ExtractPredictions": true
  }
]
```

### 3. `StyleSlug` — mapping to compare-vs-pro desks

Optional slug ties a source to a parody desk in `PunditPersonas` for Studio UX:

| StyleSlug | Desk persona |
|-----------|--------------|
| `touchline-uk` | Side-View Gary (touchline pundit energy) |
| `ex-pro-couch` | Sofa Captain Rio |
| `hot-take-desk` | Screamin' Stephen |
| `silky-studio` | Le Prof Henri |

When real licensed takes are stored, set the linked `Pundit.AttributionMode` to `Licensed` and populate `SourceUrl` / `SourceType` on each `PunditPrediction`.

## Source attribution rules

Every real personality take **must** cite the source:

| Field | Example |
|-------|---------|
| `PunditPrediction.SourceUrl` | `https://www.youtube.com/watch?v=abc123` |
| `PunditPrediction.SourceType` | `youtube`, `podcast`, `article` |
| `PunditPrediction.EvidenceSnippet` | Short quotable line (not full transcript) |
| `PunditPrediction.Speaker` | Host name if known |
| `Pundit.Organization` | Show/outlet name |

UI copy comes from `PunditDisplayResolver`:

- YouTube → *"Take sourced from YouTube · {Organization}."*
- Podcast → *"Take sourced from podcast · {Organization}."*
- Feed source line → `{DeskLabel} · via YouTube`

Fictional parody desks (`AttributionMode: Persona`) show the disclaimer — not used for scraped real takes.

## How data flows to the feed

```
News RSS / NewsAPI ──► NewsIngestJob ──► news_feed_items ──► main feed panel
                                              │
Media RSS / YouTube ──► MediaIngestJob ──► media_items ──► (extraction job, next)
                                              │
                                              ▼
                                    pundit_predictions ──► vs_pundits Studio tab
                                              │              pundit feed cards
                                              │              user vs pro contrast
                                              ▼
                              quotable / soundbite cards (planned category: pundit_quote)
```

Registered users with picks see **personal feed** with pundit contrast lines. Guests see **pundit feed** from seeded or extracted predictions.

## Dev job intervals

Faster polling for local testing (in `appsettings.Development.json`):

```json
"BackgroundJobs": {
  "NewsIngestIntervalMinutes": 30,
  "MediaIngestIntervalMinutes": 60
}
```

## Compliance notes

- Prefer **RSS** over HTML scraping.
- Store **short evidence snippets** only — not full articles or transcripts for republication.
- YouTube captions may require OAuth; do not assume all videos have public captions.
- Only transcribe audio when terms of use allow it.
- Always link back to the original episode/video/article.

## Troubleshooting

| Symptom | Check |
|---------|-------|
| Empty news feed | `News:RssFeedUrls` set? Run `news-ingest`. DB connected? |
| No YouTube videos | `YouTube:ApiKey` set? Valid `ChannelId`? Quota on Google project? |
| Podcasts not appearing | RSS URL loads in browser? Run `media-ingest`. Check `/api/sync/errors` |
| Feed shows parody only | Predictions still seeded — extraction not wired yet |
| Same GIFs everywhere | Separate issue — see AI visual catalog (pinned for later) |

See also: `docs/BACKEND-CONFIGURATION.md`, `docs/WORLD_CUP_DATA_INTEGRATION_GAP_REPORT.md`.
