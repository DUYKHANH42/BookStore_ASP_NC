## 2026-09-04 - Fix Path Traversal in FileService.DeleteFileAsync
**Vulnerability:** `FileService.DeleteFileAsync` combined `WebRootPath` with arbitrary user input (`fileUrlOrName`) after replacing slashes, allowing directory traversal sequences (`../`) to delete files outside `WebRootPath`.
**Learning:** Simply using `Path.Combine` does not protect against directory traversal if the second path contains relative path components like `../`.
**Prevention:** Always convert both base path and target path with `Path.GetFullPath`, append a directory separator to the base path, and assert `fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)`.
