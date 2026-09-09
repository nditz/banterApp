# Observability & Admin

## Goal
Production failures should be visible before users report them.

## Admin Data Health
Add/reuse dashboard views for:
- competitions;
- teams;
- players;
- fixtures;
- results;
- standings;
- pundit ingestion;
- receipts;
- banter generation;
- Studio generation;
- ad system state where appropriate.

## Background Jobs
For every job show:
- enabled/disabled;
- schedule;
- last run;
- duration;
- result;
- processed records;
- last error;
- trigger now action where safe.

## Alerts / Logging
Ensure logs capture:
- provider failures;
- missing current matchweek;
- zero fixtures when fixtures are expected;
- settlement failures;
- generation failures;
- repeated job failures;
- AdSense initialization failures;
- auth/session errors.

## Metrics
Product metrics to support:
- visitor → prediction;
- prediction → return after result;
- receipt views;
- Studio opens;
- content generations;
- copy/export actions;
- league joins;
- pundit follows.

