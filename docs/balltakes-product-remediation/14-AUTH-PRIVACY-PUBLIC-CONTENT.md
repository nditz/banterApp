# Auth, Privacy & Public Content

## Anonymous First
Do not force authentication before the user can understand or try the core prediction interaction unless existing architecture requires it.

Where practical:
- allow limited anonymous interaction;
- prompt account creation after user investment;
- preserve picks where technically safe.

## Public Timeline Privacy
Never surface identifiable private user predictions/content publicly by default.

Audit:
- user profile visibility;
- league privacy;
- receipt visibility;
- generated content ownership;
- pundit/source attribution;
- deletion behavior.

## Authentication
Reuse Supabase Auth implementation where already present.
Do not implement custom authentication unless clearly necessary.

