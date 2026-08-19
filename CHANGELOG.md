## [1.0.0] - 2026-08-19

### Added
- Core domain models and interfaces for clipboard monitoring, storage, and classification.
- Windows native clipboard monitoring via Win32 `AddClipboardFormatListener` and `RemoveClipboardFormatListener` (zero CPU polling).
- Windows DPAPI user-scoped encryption for stored payloads and disk-persisted image blobs.
- Heuristic sensitive data detection for Passwords, API Keys (OpenAI, GitHub, AWS, Stripe, Google AI, Slack), JWT tokens, and Credit Cards (Luhn check).
- SQLite storage layer with WAL mode, indexing, and automatic history retention management.
- Quick Paste floating overlay (`Ctrl+Shift+V`) with instant search, 1-9 shortcuts, and direct paste simulation.
- Developer Workbench for JSON formatting/minifying, Base64/URL/HTML encoding/decoding, and line transformations.
- In-app text and snippet editor with live find/replace and statistics.
- Settings view for application exclusions, retention policies, privacy mode, and custom hotkeys.
- System tray background execution with context menu controls.
- Comprehensive unit test suite with 100% pass rate.
