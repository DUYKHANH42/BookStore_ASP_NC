## 2026-09-15 - Prevent Path Traversal in File Deletion
**Vulnerability:** `FileService.DeleteFileAsync` accepted arbitrary file paths with path traversal characters (e.g. `../`), allowing arbitrary file deletion outside `WebRootPath`.
**Learning:** `Path.Combine` does not automatically neutralize `..` path segments if combined with a relative path.
**Prevention:** Resolve full paths using `Path.GetFullPath` and check `StartsWith` on canonical root path before performing file operations.
