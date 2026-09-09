# Fixtures, Predictions & Results

## Goal
Restore and harden the core football interaction loop.

## Fixture Experience
A user should see:
- competition;
- matchweek;
- kickoff date/time;
- home team;
- away team;
- crests;
- status;
- prediction state;
- lock deadline.

## Prediction Types
Support the types already implemented first.
Do not invent incompatible scoring.

Target model may include:
- match outcome;
- exact score;
- season calls.

## Prediction Lifecycle
Draft
→ saved
→ locked
→ match live
→ final
→ settled
→ receipt created

## Rules
- predictions become immutable at configured lock time;
- settlement must be idempotent;
- score changes/corrections must support safe recalculation;
- Aura updates must be traceable;
- result settlement should generate candidate stories/receipts.

## UX
Returning users should quickly see:
- picks completed / total;
- next fixture;
- unresolved predictions;
- recent results;
- latest receipts.

