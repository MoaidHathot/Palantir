---
name: palantir-notifications
description: Sends rich Windows Toast Notifications from the command line using the Palantir CLI tool. Use when the user wants to show notifications, display alerts, prompt for confirmation, capture user input via toasts, track progress with toast progress bars, or integrate notifications into scripts and CI/CD pipelines. Requires Windows 10+ and the Palantir .NET global tool.
compatibility: Requires Windows 10 build 17763+, .NET 10 SDK, and the Palantir global tool (dotnet tool install --global Palantir)
---

# Palantir — Windows Toast Notifications CLI

Palantir is a CLI tool for displaying rich Windows Toast Notifications. It supports text, images, buttons, input forms, audio, progress bars, presets, and interactive wait-for-response patterns.

## Installation

```bash
dotnet tool install --global Palantir
```

Verify it works:

```bash
palantir test
```

## Core Usage

### Simple notifications

```bash
palantir -t "Title" -m "Message"
palantir -t "Build OK" -m "All tests passed" --attribution "Via CI" -a mail
```

### Key options

| Option | Short | Purpose |
|---|---|---|
| `--title` | `-t` | Bold first line |
| `--message` | `-m` | Second line |
| `--body` | `-b` | Third line |
| `--attribution` | | Small text at bottom |
| `--audio` | `-a` | Sound: `default`, `im`, `mail`, `reminder`, `sms`, `alarm`, `call` |
| `--silent` | `-s` | No sound |
| `--duration` | | `short` (~5s) or `long` (~25s) |
| `--image` | `-i` | App logo (file path or URL) |
| `--hero-image` | | Large banner image |
| `--quiet` | `-q` | Suppress console output |

For the complete options list, see [references/options.md](references/options.md).

## Buttons

Three button types:

| Format | Example | Behavior |
|---|---|---|
| `"Label"` | `--button "OK"` | Dismiss |
| `"Label;submit"` | `--button "Send;submit"` | Foreground activation (captures input) |
| `"Label;uri"` | `--button "Open;https://..."` | Opens URI |

**Submit buttons are required to capture user input.** Dismiss buttons discard input.

Structured alternative: `--button "label=Send,action=submit,arguments=send-reply"`

## Input Forms

```bash
# Text input
--input "id;placeholder text"

# Selection box (combo box)
--selection "id;Option A,Option B,Option C"
```

Inputs are only useful with a submit button and `--wait`:

```bash
palantir -t "Reply" \
  --input "reply;Type here..." \
  --button "Send;submit" --button "Cancel" \
  --wait
```

Output when user types and clicks Send:

```json
{"action":"activated","arguments":"button=Send","userInputs":{"reply":"hello"}}
```

## Wait for Interaction

`--wait` blocks until the user interacts. Returns JSON to stdout.

```bash
palantir -t "Deploy?" --button "Yes;submit" --button "No" --wait
```

### Exit codes

| Code | Meaning |
|------|---------|
| 0 | activated (user clicked submit button or toast body) |
| 1 | dismissed (user swiped away / clicked X) |
| 2 | failed |
| 3 | cancelled (Ctrl+C) |
| 4 | timed out |

### Timeout

```bash
palantir -t "Confirm" --button "OK;submit" --wait --timeout 30
```

### Output formats

| Format | Flag | Output |
|---|---|---|
| JSON | `--format json` | `{"action":"activated","arguments":"button=OK"}` |
| Text | `--format text` | `action=activated\narguments=button=OK` |
| None | `--format none` | No output, just exit code |

### PowerShell scripting pattern

```powershell
$result = palantir -t "Continue?" -m "Deploy to prod?" `
  --button "Deploy;submit" --button "Cancel" `
  --wait --timeout 60 | ConvertFrom-Json

switch ($result.action) {
    "activated" { Write-Host "Deploying..." }
    "dismissed" { Write-Host "Cancelled" }
    "timedOut"  { Write-Host "No response" }
}
```

### Capturing form input

```powershell
$result = palantir -t "Bug Report" `
  --input "desc;Describe the issue" `
  --selection "severity;Critical,High,Medium,Low" `
  --button "Submit;submit" --button "Cancel" `
  --wait | ConvertFrom-Json

if ($result.action -eq "activated") {
    $desc = $result.userInputs.desc
    $sev = $result.userInputs.severity
}
```

## Progress Bars

### Show progress

```bash
palantir -t "Downloading" \
  --progress-title "file.zip" \
  --progress-value 0.6 \
  --progress-status "60%..." \
  --tag "dl-1"
```

Use `--progress-value indeterminate` for a spinning indicator.

### Update progress (no new popup)

```bash
palantir update --tag "dl-1" --progress-value 0.8 --progress-status "80%..."
palantir update --tag "dl-1" --progress-value 1.0 --progress-status "Complete!"
```

### Replace with final result

```bash
palantir -t "Download Complete" -m "file.zip" --tag "dl-1" --replace -a default
```

## Presets

Three built-in presets: `alarm`, `reminder`, `call`.

```bash
palantir -t "Alert!" --preset alarm        # alarm sound, looping, long duration
palantir -t "Meeting soon" --preset reminder  # reminder sound, long duration
```

### Custom presets

```bash
# Save
palantir preset save my-alert '{"audio":"mail","duration":"long","attribution":"Via CI"}'

# Use
palantir -t "Done" --preset my-alert

# List / show / delete
palantir preset list
palantir preset show my-alert
palantir preset delete my-alert
```

CLI options always override preset values.

Config file location (first match): `$PALANTIR_CONFIG_PATH`, `$XDG_CONFIG_HOME/Palantir/`, `%APPDATA%\Palantir/`. File: `palantir.json`.

## Tagging & Headers

```bash
# Tag for later update/remove/replace
palantir -t "Build" -m "Running..." --tag "build-1" --group "ci"

# Group in Action Center under a header
palantir -t "PR merged" --header-id "github" --header-title "GitHub"
```

## Subcommands

| Command | Purpose |
|---|---|
| `palantir clear` | Clear all notification history |
| `palantir remove --tag X` | Remove a specific toast |
| `palantir remove --group X` | Remove all toasts in a group |
| `palantir update --tag X ...` | Update progress data |
| `palantir history` | List active notifications |
| `palantir test` | Send a test notification |
| `palantir preset save/list/show/delete` | Manage presets |
| `palantir completions powershell` | Generate shell completions |

## Other Features

| Feature | How |
|---|---|
| Stdin pipe | `echo "text" \| palantir -t "Title" -m -` |
| JSON input | `palantir --json notification.json` or `--json -` for stdin |
| Dry run | `palantir -t "Test" --dry-run` (outputs XML, no toast) |
| On-click command | `palantir -t "Done" --on-click "explorer ."` (implies --wait) |
| Launch URI | `palantir -t "Click me" --launch "https://example.com"` |
| Version | `palantir --version` |

## Common Patterns

### CI/CD build result

```powershell
if ($LASTEXITCODE -eq 0) {
    palantir -t "Build OK" -m "$project succeeded" -a default -q
} else {
    palantir -t "Build Failed" -m "$project has errors" --preset alarm -q
}
```

### Confirmation dialog with timeout

```powershell
palantir -t "Destructive Action" -m "Delete all temp files?" `
  --button "Delete;submit" --button "Cancel" `
  --wait --format none --timeout 30

if ($LASTEXITCODE -eq 0) { Remove-Item .\temp\* -Recurse }
```

### Progress tracking loop

```powershell
palantir -t "Processing" --progress-title "Files" `
  --progress-value 0.0 --progress-status "Starting..." --tag "proc" -q

for ($i = 1; $i -le $total; $i++) {
    # ... do work ...
    palantir update --tag "proc" --progress-value ($i/$total) `
      --progress-status "$i/$total done" -q
}

palantir -t "Complete" -m "All files processed" --tag "proc" --replace -q
```

For more examples including multi-step workflows, monitoring dashboards, and scheduled reminders, see [EXAMPLES.md](../../EXAMPLES.md).
