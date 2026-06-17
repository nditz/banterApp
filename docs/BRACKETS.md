# Tournament bracket architecture

## Recommended libraries (research summary)

| Library | Best for | Group → knockout | React | Notes |
|---------|----------|------------------|-------|-------|
| [brackets-manager.js](https://github.com/Drarig29/brackets-manager.js) | Tournament logic | Yes (round-robin → elimination) | Via [brackets-viewer.js](https://github.com/Drarig29/brackets-viewer.js) | Logic-only; pair with your storage. Ideal if you move bracket state to JS. |
| [@g-loot/react-tournament-brackets](https://github.com/g-loot/react-tournament-brackets) | SVG knockout tree UI | No (display only) | Yes | Supply your own data; good for polished connector lines. |
| [react-brackets](https://www.npmjs.com/package/react-brackets) | Simple elimination UI | No | Yes | Lightweight columns; no standings engine. |

**BanterApp choice:** Custom C# `BracketEngine` + React UI. This keeps kickoff locking, guest sessions, CSRF, and rate limits in one backend while supporting group-stage qualification rules specific to the World Cup.

## Data sources (fixtures)

| Provider | Env | Coverage |
|----------|-----|----------|
| **API-Football** (recommended) | `SPORTS_API_PROVIDER=apifootball`, `SPORTS_API_KEY` | World Cup `league=1`, `season=2026` — fixtures, groups, standings |
| **Mock** (default) | unset key | 8 groups × 6 matches + knockout shell |
| [openfootball/worldcup.json](https://github.com/openfootball/worldcup.json) | optional future | Free static JSON, no key |

Set in Vercel/hosting secrets:

```env
SPORTS_API_PROVIDER=apifootball
SPORTS_API_KEY=your-api-football-key
```

## How the bracket fills recursively

1. **Group stage** — one slot per group fixture (`grp-{matchId}`). User picks a winner.
2. **Standings** — `GroupStandingsService` scores picks (+3 for predicted win) and merges finished real results.
3. **Round of 32** — 16 slots: 8× winner vs runner-up (other groups), 8× winner vs third-placed team. Third-placers ranked globally (pts → GD → GF); FIFA **Annex C** (`Data/fifa-2026-annex-c-assignments.json`, 495 rows) maps the qualifying eight groups to R32 opponents.
4. **Round of 16 → Final** — winners propagate; third-place play-off uses semi-final losers.

## Flags

Frontend uses [flagcdn.com](https://flagcdn.com) with FIFA code → ISO mapping in `frontend/src/lib/team-flags.ts`.
