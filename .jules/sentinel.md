## 2026-09-04 - HTTP Security Headers Implementation
**Vulnerability:** Absence of standard HTTP security headers (X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy, Content-Security-Policy) exposed the API and served views to potential Clickjacking, MIME-sniffing, and Cross-Site Scripting (XSS) attacks.
**Learning:** Adding ASP.NET Core middleware before static files and route execution ensures all responses automatically inherit defense-in-depth security headers.
**Prevention:** Always register `SecurityHeadersMiddleware` early in the `IApplicationBuilder` pipeline in `Startup.cs`.
