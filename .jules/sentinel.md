## 2026-09-04 - Path Traversal in FileService Delete Functionality

**Vulnerability:** `FileService.DeleteFileAsync` allowed unvalidated file paths (such as `../../appsettings.json`), enabling arbitrary file deletion (Path Traversal / CWE-22) outside the designated `wwwroot` directory.

**Learning:** Using simple string concatenation or replacement like `Path.Combine(contentPath, fileUrlOrName.Replace("/", "\\"))` without resolving absolute paths (`Path.GetFullPath`) allows relative path navigation markers (`..`) to escape the target directory.

**Prevention:** Always sanitize relative file paths using `Path.GetFullPath` and verify that the resulting absolute path starts with the expected base root directory before performing file system operations (e.g., `fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)`).
