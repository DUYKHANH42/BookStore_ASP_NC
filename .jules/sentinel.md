## 2026-09-03 - Eliminating Hardcoded JWT Secret Fallbacks
**Vulnerability:** `AuthRepository.cs` had a fallback hardcoded secret (`Chuoi_Bi_Mat_Sieu_Cap_Vip_Pro_2024_@123`) used when `JWT:Secret` was null or empty during token generation.
**Learning:** Even if `Startup.cs` validates startup configuration, repository methods with defensive fallback defaults can reintroduce hardcoded credentials and allow signature forgery if configuration fails at runtime.
**Prevention:** Enforce fail-fast configuration checks by throwing `InvalidOperationException` when required security keys are missing instead of falling back to default strings in code.
