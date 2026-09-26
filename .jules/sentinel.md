## 2026-09-03 - JWT Secret Hardcoded Fallback Removal
**Vulnerability:** Use of a hardcoded fallback secret (`"Chuoi_Bi_Mat_Sieu_Cap_Vip_Pro_2024_@123"`) when `JWT:Secret` was not present in the application configuration.
**Learning:** Having fallback default secrets in source code allows JWT tokens to be generated/signed with known weak keys if configuration is omitted or misconfigured, leading to potential authentication bypass or forgery.
**Prevention:** Enforce fail-fast behavior by throwing an `InvalidOperationException` if critical secrets like `JWT:Secret` are missing from configuration.
