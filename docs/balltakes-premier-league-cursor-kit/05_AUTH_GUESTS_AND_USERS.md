# Authentication, Anonymous Users and Account Management

## Principle

Anonymous play remains a first-class feature. Do not require registration before users can make predictions.

## Guest journey

Visit -> anonymous session -> recovery mechanism -> predictions -> private leagues -> scores/history -> optional account creation.

## Recovery keys

Retain the existing recovery-key approach but audit it for:

- entropy;
- hashing/storage;
- raw key exposure;
- expiry policy if any;
- duplicate recovery attempts;
- session restoration;
- logging leakage.

Avoid storing raw recovery keys unnecessarily.

## Guest session model

Use/adapt an equivalent model:

GuestSession
- Id
- RecoveryKeyHash
- CreatedAt
- LastSeenAt
- ClaimedByUserId nullable

## Supabase authentication

Complete/standardize Supabase as the durable account identity layer.

Initial providers:

- email/password;
- Google OAuth.

Do not duplicate password management in the BallTakes domain database.

Domain user should reference the Supabase auth identity.

Suggested fields:

User
- Id
- SupabaseUserId
- Username
- DisplayName
- AvatarUrl
- CountryCode nullable
- CreatedAt
- UpdatedAt

## Guest-to-account claim

This is a critical migration flow.

When a guest creates an account:

1. authenticate through Supabase;
2. resolve guest session;
3. create/link domain User;
4. attach existing predictions;
5. attach private-league membership;
6. attach scores/history;
7. attach generated content;
8. mark guest session claimed;
9. preserve audit information.

The operation must be idempotent and transactional where possible.

Never duplicate predictions or league memberships during retry.

## Account management

Provide:

- profile;
- username/display name;
- avatar;
- login provider information;
- subscription state;
- AI credit/usage visibility;
- prediction statistics;
- private league memberships;
- content history;
- logout;
- account deletion.

## Security

Review:

- Supabase JWT validation;
- authorization boundaries;
- guest session ownership;
- recovery endpoints;
- rate limiting;
- admin authorization;
- secrets and logging.
