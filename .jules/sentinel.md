## 2026-09-13 - Sensitive Administrative Config Exposed in Public WebRoot
**Vulnerability:** The admin settings controller saved custom administrative settings (`AdminAppSettings`) directly into `wwwroot/admin/appsettings.json`, making sensitive configuration files accessible publicly over HTTP without authentication.
**Learning:** Storing dynamic configuration files inside `wwwroot` exposes them to anonymous web users via direct static file requests.
**Prevention:** Always place writable configuration files, data exports, or private app files outside the web root (`WebRootPath`), using `ContentRootPath` (e.g., `App_Data/`) or secure data persistence mechanisms instead.
