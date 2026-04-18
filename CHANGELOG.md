# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **`remove` subcommand** — Remove specific toasts by `--tag` or `--group` from notification history
- **`update` subcommand** — Update an existing toast's progress bar data without showing a new toast
- **`history` subcommand** — List active toast notifications with their tags, groups, and content
- **`test` subcommand** — Send a test notification to verify Palantir is working
- **`completions` subcommand** — Generate shell completion scripts (PowerShell)
- **`--wait` flag** — Block until the toast is dismissed or activated; outputs interaction result to stdout
- **`--timeout` option** — Timeout in seconds for `--wait` (implies `--wait`); auto-resolves with `"timedOut"` action
- **`--format` option** — Output format for `--wait`: `json` (default), `text` (key=value lines), or `none` (exit code only)
- **`--replace` flag** — Replace an existing toast with the same `--tag` (re-shows the popup)
- **`--dry-run` flag** — Output the toast XML without displaying it, useful for debugging
- **`--json` option** — Load toast options from a JSON file or stdin (`--json -`)
- **`--preset` option** — Apply built-in or user-defined preset configurations
- **`preset` subcommand** — Full preset management: `preset save`, `preset list`, `preset show`, `preset delete`
- **User-defined presets** — Save custom presets as JSON to a config file; any `ToastOptions` field can be preset
- **Config file** — Extensible `palantir.json` with location resolution: `PALANTIR_CONFIG_PATH` env var, `$XDG_CONFIG_HOME/Palantir/`, or `%APPDATA%\Palantir\`
- **Submit buttons** — `"Label;submit"` creates foreground activation buttons that capture user input with `--wait`
- **Structured button syntax** — `"label=X,action=Y"` key-value format as alternative to semicolon format
- **`--on-click` option** — Execute a shell command when the toast is activated (implies `--wait`)
- **`--header-id`, `--header-title`, `--header-arguments`** — Group related toasts under a header in Action Center
- **`--version` flag** — Display the installed version
- **Stdin pipe support** — Use `"-"` as the value for `--title`, `--message`, or `--body` to read from stdin
- **Short aliases** — `-a` for `--audio`, `-s` for `--silent`
- **Input validation warnings** — Invalid values for `--duration`, `--scenario`, `--timestamp`, `--progress-value`, and `--expiration` now emit warnings to stderr instead of being silently ignored
- **URI validation** — Button URIs and launch URIs are validated before use with clear error messages
- **HTTP image warnings** — Loading images over insecure HTTP now emits a warning suggesting HTTPS
- **Image file existence check** — Missing local image files now emit a warning
- **Malformed input warnings** — Buttons with empty labels, inputs with empty IDs, and malformed selections now produce clear warnings
- **Unit tests** — Comprehensive test suite covering audio/image URI resolution, validation, presets, XML generation, and JSON serialization
- **CI/CD pipelines** — GitHub Actions workflows for continuous integration and automated NuGet releases on tags
- **`.editorconfig`** — Code style enforcement for consistent formatting

### Changed

- **Sound map** is now a static readonly field instead of being rebuilt on every `ResolveAudioUri` call (performance improvement)
- **Exception handling** now catches specific exception types (`InvalidOperationException`, `COMException`) before the general `Exception` catch
- **`clear` subcommand** now has proper error handling with try/catch
- **Progress value** is validated and clamped to the 0.0–1.0 range with a warning
- **Default progress status** text `"In progress"` extracted to a named constant

### Removed

- **`--app-id` option** — Was accepted but never used (dead code). The underlying library manages the AUMID automatically.

### Fixed

- Invalid `--duration` values (e.g., `--duration medium`) silently defaulting to `"short"` — now warns the user
- Invalid `--scenario` values silently defaulting to `"default"` — now warns the user
- Invalid `--timestamp` values being silently ignored — now warns the user
- `--expiration 0` and negative values being silently ignored — now warns the user
- Button URIs like `--button "Click;not-a-uri"` could throw unhandled `UriFormatException` — now validates and falls back to dismiss button with a warning
- Launch URI like `--launch "bad uri"` could throw unhandled `UriFormatException` — now validates with a warning
