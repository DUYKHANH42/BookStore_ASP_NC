# Sentinel Security Journal

## 2026-03-30 - Path Traversal Prevention in Local File Storage Operations
**Vulnerability:** In `FileService.cs`, path inputs (such as `folderName` and `fileUrlOrName`) were used in `Path.Combine` without canonical path validation. An attacker could pass traversal sequences like `../` to access, overwrite, or delete arbitrary files outside the intended `wwwroot` or `uploads` directories.
**Learning:** `Path.Combine` does not prevent path traversal if any input segment contains relative directory sequences (`../`). Simply concatenating or replacing slashes is insufficient to secure file system operations.
**Prevention:** Always convert combined paths to canonical absolute paths using `Path.GetFullPath(...)` and enforce that the resulting full path starts with the expected base directory (`WebRootPath` or `uploadsBasePath`) using `StringComparison.OrdinalIgnoreCase` before performing file I/O operations.
