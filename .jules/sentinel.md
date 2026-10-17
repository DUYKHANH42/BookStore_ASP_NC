## 2026-09-07 - Path Traversal in FileService.DeleteFileAsync
**Vulnerability:** `FileService.DeleteFileAsync` accepted arbitrary file paths (e.g. containing `../`) and constructed target paths using `Path.Combine(WebRootPath, fileUrlOrName)`, allowing arbitrary file deletion outside the web root directory.
**Learning:** `Path.Combine` does not prevent path traversal sequences (`../`) from escaping the root directory when evaluated by the filesystem.
**Prevention:** Always normalize the base directory path and resolved full path using `Path.GetFullPath()`, and verify that `fullPath.StartsWith(basePath)` before performing file system operations.
