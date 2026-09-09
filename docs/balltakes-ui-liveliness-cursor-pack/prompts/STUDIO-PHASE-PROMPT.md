Implement the approved Studio UI remediation only.

Studio is the core creator workspace.
It should start from Ball Takes context, not a blank prompt.

Build around:
- story sources/receipts
- user vs pundit comparisons
- content type selection
- tone/energy selection
- structured content-pack result
- copy/export/save/regenerate actions

Respect existing API capabilities. If a desired field is not yet available, create a clean UI boundary and document the missing data dependency instead of hardcoding fake production data.

Desktop may use workspace panes; mobile must use a staged flow.
Do not add final media-generation integrations unless already approved in the backend plan.
