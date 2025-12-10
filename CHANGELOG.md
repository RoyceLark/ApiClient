# Changelog

## [1.1.0] - 2024-12-10

### ✨ Added
- **CancellationToken Support**: All API methods now support CancellationToken for request cancellation
- **File Upload**: `[Multipart]` attribute and `FileContent` class for file uploads
- **Multi-File Upload**: `MultipartContent` class for uploading multiple files with form fields
- **Form Data**: `[Form]` attribute for application/x-www-form-urlencoded submissions
- **Response Headers**: `ApiResponse<T>` wrapper to access response headers and metadata
- **Per-Request Timeout**: `TimeoutSeconds` property on HTTP method attributes
- **PATCH Support**: `[Patch]` HTTP method attribute
- **Timeout Handling**: Separate handling for cancellation vs timeout exceptions

### 🐛 Fixed
- **CRITICAL**: Fixed `[Query]` attribute bug - query parameters were incorrectly treated as route parameters
- Improved parameter detection logic in RequestBuilder
- Better handling of null/optional parameters

### 🔧 Changed
- Enhanced RequestBuilder with multipart and form-encoded content support
- Improved ApiClientProxy with CancellationToken propagation
- Better timeout handling with per-method overrides
- Enhanced error messages and logging

### 📚 Documentation
- Updated README with all new features
- Added comprehensive examples for new features
- Enhanced QUICK_REFERENCE with new patterns
- Updated version to 1.1.0

## [1.0.0] - 2024-12-09

### Initial Release
- Dynamic proxy-based API client generation
- HTTP method attributes (Get, Post, Put, Delete)
- Parameter attributes (Body, Query, Header)
- Automatic retry with Polly
- Request/response logging
- HttpClientFactory support
- Dependency injection
- Custom JSON serialization
- Comprehensive error handling
