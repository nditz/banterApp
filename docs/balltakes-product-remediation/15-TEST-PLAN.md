# Test Plan

## Unit Tests
- current matchweek resolver;
- prediction scoring;
- Aura calculation;
- settlement idempotency;
- pundit comparison;
- story classification;
- receipt generation;
- novelty/repetition filters;
- Studio context assembly.

## Integration Tests
- provider → DB fixture sync;
- results → settlement;
- settlement → receipt;
- receipt → Studio context;
- pundit ingestion → comparison;
- auth → prediction persistence;
- AdSlot consent behavior.

## End-to-End Critical Flows
1. Load homepage.
2. See current fixtures.
3. Make prediction.
4. Persist prediction.
5. Simulate/consume final result.
6. Settle prediction.
7. Create receipt.
8. Compare with pundit.
9. Open receipt in Studio.
10. Generate content pack.
11. Copy/export result.

## Failure Tests
- football API unavailable;
- empty fixture response;
- stale data;
- generation API failure;
- pundit source unavailable;
- ad blocked;
- consent not granted;
- expired session.

## Responsive QA
- 375px;
- 768px;
- 1024px;
- 1440px.

