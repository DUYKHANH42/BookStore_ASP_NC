## 2026-09-04 - Fix Path Traversal in FileService
**Vulnerability:** In `FileService.cs`, `DeleteFileAsync` and `SaveFileAsync` were susceptible to Arbitrary File Deletion / Path Traversal attacks via relative path navigation sequences (e.g. `../..`) in `fileUrlOrName` or `folderName`.
**Learning:** `Path.Combine` does not prevent path traversal if the user-supplied string contains `..`. Furthermore, on Windows and Linux, slashes must be properly normalized and validated against `WebRootPath`.
**Prevention:** Always use `Path.GetFullPath` to resolve the absolute canonical path and verify that `fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase)` before deleting or saving files.
