## 2026-09-04 - Revoke User Token Version On Admin Password Reset
**Vulnerability:** When an administrator resets a customer's password, active JWT tokens and Redis refresh tokens were not revoked, allowing potentially compromised access to persist with existing valid JWT tokens.
**Learning:** Updating or resetting user credentials must always invalidate active token sessions by incrementing `TokenVersion` and purging cached refresh tokens from Redis.
**Prevention:** Always pair password changes and administrative resets with `TokenVersion` increments and Redis token cache purges.
