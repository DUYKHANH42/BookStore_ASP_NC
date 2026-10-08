## 2026-09-04 - Rate Limiting for Authentication Endpoints
**Vulnerability:** Missing rate limiting on the POST `/api/auth/login` authentication endpoint made the application vulnerable to credential stuffing and brute-force attacks.
**Learning:** Adding rate limiting at the ASP.NET Core middleware level using `AddRateLimiter` and `[EnableRateLimiting("policy")]` ensures traffic is throttled per client IP/session before reaching heavy authentication logic.
**Prevention:** Apply rate limiting policies on all high-risk public authentication endpoints (`/login`, `/register`, `/forgot-password`, `/reset-password`).
