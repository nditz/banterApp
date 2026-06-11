# Phase 2 — AI Cost Estimates

Rough token and cost projections for enabling live LLM providers in BanterApp. Assumes **PG-rated, short-form sports banter** with moderate context (prediction + match result/stats).

## Assumptions per generation type

| Content type | Typical input tokens | Typical output tokens | Notes |
|--------------|---------------------:|----------------------:|-------|
| Banter | 150 | 80 | Prediction vs result, tone modifier |
| Analysis | 400 | 250 | Prediction + match statistics JSON |
| Meme caption | 100 | 40 | Short social caption |
| Video script | 300 | 200–600 | Scales with duration (15s ≈ 200 out, 60s ≈ 600 out) |

**Blended average per generation:** ~350 input + ~200 output tokens (~550 total).

Provider pricing below uses published list rates (approximate, mid-2025). Actual costs vary by model tier, caching, and batch discounts.

## Per-generation cost (USD)

| Provider | Model (example) | Input / 1M tokens | Output / 1M tokens | Est. cost per generation |
|----------|-----------------|------------------:|-------------------:|-------------------------:|
| OpenAI | GPT-4o mini | $0.15 | $0.60 | ~$0.00017 |
| OpenAI | GPT-4o | $2.50 | $10.00 | ~$0.0029 |
| Anthropic | Claude 3.5 Haiku | $0.80 | $4.00 | ~$0.0011 |
| Anthropic | Claude 3.5 Sonnet | $3.00 | $15.00 | ~$0.0041 |
| Google | Gemini 2.0 Flash | $0.10 | $0.40 | ~$0.00012 |
| Google | Gemini 2.5 Pro | $1.25 | $10.00 | ~$0.0024 |

*Calculation: `(input × input_rate + output × output_rate) / 1_000_000`*

## Daily volume scenarios

Estimates use **GPT-4o mini** as the baseline cost-efficient model (~$0.00017/generation). Multiply by ~17× for GPT-4o-class quality.

### All content types combined (equal mix of 4 types)

| Daily generations | Tokens/day (approx.) | GPT-4o mini / day | GPT-4o / day | Claude Haiku / day | Gemini Flash / day |
|------------------:|---------------------:|------------------:|-------------:|-------------------:|-------------------:|
| 1,000 | ~550K | $0.17 | $2.90 | $1.10 | $0.12 |
| 10,000 | ~5.5M | $1.70 | $29.00 | $11.00 | $1.20 |
| 100,000 | ~55M | $17.00 | $290.00 | $110.00 | $12.00 |

### By content type at 10,000 daily generations each (40,000 total/day)

| Content type | Generations/day | GPT-4o mini / day | GPT-4o / day |
|--------------|----------------:|------------------:|-------------:|
| Banter | 10,000 | $1.10 | $19.00 |
| Analysis | 10,000 | $2.80 | $48.00 |
| Meme caption | 10,000 | $0.55 | $9.50 |
| Video script (30s avg) | 10,000 | $2.20 | $38.00 |

### By content type at 100,000 daily generations each (400,000 total/day)

| Content type | Generations/day | GPT-4o mini / day | GPT-4o / day |
|--------------|----------------:|------------------:|-------------:|
| Banter | 100,000 | $11.00 | $190.00 |
| Analysis | 100,000 | $28.00 | $480.00 |
| Meme caption | 100,000 | $5.50 | $95.00 |
| Video script (30s avg) | 100,000 | $22.00 | $380.00 |

## Cost control recommendations (Phase 2)

1. **Default to a flash/mini tier** (GPT-4o mini, Claude Haiku, Gemini Flash) for banter and meme captions; reserve larger models for analysis and long video scripts.
2. **Cache match-level analysis** — identical stats + prediction combos can reuse output for 24h.
3. **Enforce anonymous limit** (3 generations) already in Phase 1 stub; extend rate limits for registered users by tier.
4. **Batch off-peak sync** — pre-generate leaderboard banter after match finalization rather than on every page view.
5. **Monitor per-endpoint token usage** — tag generations by type for accurate forecasting.

## Disclaimer

Prices change frequently. Re-validate against current provider pricing pages before budgeting. Image generation (memes) and TTS (video) are **not** included — add separately if Phase 2 expands beyond text.
