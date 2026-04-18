# Palantir

A CLI tool for showing rich Windows Toast Notifications. Named after the seeing stones of Middle-earth.

## Installation

```bash
dotnet tool install --global Palantir
```

## Quick Start

```bash
# Simple notification
palantir -t "Hello" -m "World"

# With all three text lines
palantir -t "Title" -m "Message" -b "Extra detail"

# With attribution
palantir -t "Build Complete" -m "All tests passed" --attribution "Via CI Pipeline"

# Pipe command output into a toast
git log -1 --oneline | palantir -t "Latest Commit" -m -
```

## Features

### Text Content

| Option | Short | Description |
|---|---|---|
| `--title` | `-t` | Toast title (first line, bold) |
| `--message` | `-m` | Body message (second line) |
| `--body` | `-b` | Additional text (third line) |
| `--attribution` | | Attribution text at the bottom |

Use `"-"` as the value for `--title`, `--message`, or `--body` to read from stdin:

```bash
echo "Build succeeded" | palantir -t "CI" -m -
```

### Images

```bash
# App logo override
palantir -t "Alert" -m "Check this out" --image ./logo.png

# Circular app logo
palantir -t "User" -m "John Doe" --image ./avatar.png --crop-circle

# Hero image (large banner at top)
palantir -t "Photo" -m "New upload" --hero-image ./banner.jpg

# Inline image (in the body)
palantir -t "Preview" -m "See below" --inline-image ./preview.png

# Remote image
palantir -t "News" -m "Breaking" --hero-image "https://example.com/banner.jpg"
```

| Option | Short | Description |
|---|---|---|
| `--image` | `-i` | App logo override (file path or URL) |
| `--crop-circle` | | Crop app logo as circle |
| `--hero-image` | | Hero image at the top |
| `--inline-image` | | Inline image in the body |

### Buttons

```bash
# Dismiss button
palantir -t "Info" -m "Noted" --button "OK"

# Protocol activation (opens URL/app)
palantir -t "Update" -m "New version available" --button "Download;https://example.com" --button "Later"

# Multiple buttons
palantir -t "Call" -m "Incoming" --button "Answer;tel:+123" --button "Decline"
```

Format: `"Label"` for dismiss, or `"Label;uri"` for protocol activation.

### Input Fields

```bash
# Text input
palantir -t "Reply" -m "Quick response" --input "reply;Type your reply..."

# Selection box
palantir -t "Choose" -m "Pick an option" --selection "choice;Option A,Option B,Option C"
```

### Audio

```bash
# Named sounds: default, im, mail, reminder, sms
palantir -t "Email" -m "New message" -a mail

# Looping alarm sounds: alarm, alarm2-alarm10, call, call2-call10
palantir -t "Alarm" -m "Wake up!" -a alarm --loop --duration long

# Silent notification
palantir -t "Quiet" -m "No sound" -s
```

| Option | Short | Description |
|---|---|---|
| `--audio` | `-a` | Sound name or file path |
| `--silent` | `-s` | No sound |
| `--loop` | | Loop the audio (use with `--duration long`) |

### Behavior

```bash
# Long duration (~25 seconds instead of ~5)
palantir -t "Important" -m "Read this" --duration long

# Alarm scenario (stays on screen, looping audio)
palantir -t "ALARM" -m "Server down!" --scenario alarm -a alarm --loop

# Reminder scenario
palantir -t "Meeting" -m "Standup in 5 minutes" --scenario reminder

# Expires after 10 minutes
palantir -t "Temp" -m "This will expire" --expiration 10

# Custom timestamp
palantir -t "Scheduled" -m "Was scheduled for earlier" --timestamp "2025-01-01T09:00:00"
```

| Option | Description |
|---|---|
| `--duration` | `short` (~5s) or `long` (~25s) |
| `--scenario` | `default`, `alarm`, `reminder`, or `incomingCall` |
| `--expiration` | Auto-expire after N minutes |
| `--timestamp` | Custom display timestamp (ISO 8601) |

### Presets

Presets apply saved defaults to your toasts. Three built-in presets are always available, and you can create your own.

**Built-in presets:**

```bash
# Alarm preset: scenario=alarm, audio=alarm, loop=true, duration=long
palantir -t "WAKE UP" -m "It's 7 AM!" --preset alarm

# Reminder preset: scenario=reminder, audio=reminder, duration=long
palantir -t "Meeting" -m "Standup in 5 minutes" --preset reminder

# Call preset: scenario=incomingCall, audio=call, loop=true, duration=long
palantir -t "Incoming Call" -m "John Doe" --preset call

# Explicit CLI options always override preset values
palantir -t "Custom Alarm" --preset alarm -a mail
```

**Creating custom presets:**

```bash
# Save a preset from inline JSON
palantir preset save my-deploy '{"scenario":"reminder","audio":"mail","duration":"long","attribution":"Via CI"}'

# Save from a JSON file
palantir preset save my-deploy preset-template.json

# Save from stdin
echo '{"audio":"im","duration":"short"}' | palantir preset save quick-ping

# Use your custom preset
palantir -t "Deployed!" -m "v2.1.0 is live" --preset my-deploy
```

**Managing presets:**

```bash
# List all presets (built-in + user)
palantir preset list

# Show a preset's full configuration
palantir preset show my-deploy

# Delete a user preset
palantir preset delete my-deploy

# Override a built-in preset
palantir preset save alarm '{"scenario":"alarm","audio":"alarm2","loop":true,"duration":"long","attribution":"Custom Alarm"}'

# Delete your override to restore the built-in default
palantir preset delete alarm
```

**Config file location** (first match wins):
1. `PALANTIR_CONFIG_PATH` environment variable (directory path)
2. `$XDG_CONFIG_HOME/Palantir/`
3. `%APPDATA%\Palantir\`

The config file (`palantir.json`) stores your presets alongside any future configuration:

```json
{
  "presets": {
    "my-deploy": {
      "scenario": "reminder",
      "audio": "mail",
      "duration": "long",
      "attribution": "Via CI Pipeline",
      "buttons": ["View;https://app.example.com", "Dismiss"]
    }
  }
}
```

### Progress Bar

```bash
# Determinate progress
palantir -t "Downloading" --progress-title "file.zip" --progress-value 0.6 --progress-status "Downloading..."

# With custom value string
palantir -t "Songs" --progress-title "Playlist" --progress-value 0.3 --progress-value-string "3/10 songs" --progress-status "Syncing"

# Indeterminate progress
palantir -t "Processing" --progress-title "Please wait" --progress-value indeterminate --progress-status "Working..."
```

### Launch Action

```bash
# Open URL when toast body is clicked
palantir -t "Article" -m "New blog post" --launch "https://example.com/blog"
```

### Wait for Interaction

Block until the user interacts with the toast. The result is output as JSON to stdout.

```bash
# Wait and capture result
palantir -t "Deploy?" -m "Push to production?" --button "Yes" --button "No" --wait
# Output: {"action":"activated","arguments":"..."}
# Exit codes: 0=activated, 1=dismissed, 2=failed, 3=cancelled

# Use in scripts
$result = palantir -t "Confirm" --button "OK" --wait | ConvertFrom-Json
if ($result.action -eq "activated") { Write-Host "User confirmed!" }
```

### On-Click Command

Execute a shell command when the toast is activated (implies `--wait`):

```bash
palantir -t "Build Done" -m "Open folder?" --on-click "explorer ."
```

### Headers (Action Center Grouping)

Group related toasts under a header in Action Center:

```bash
palantir -t "Email 1" -m "Subject A" --header-id "emails" --header-title "New Emails"
palantir -t "Email 2" -m "Subject B" --header-id "emails" --header-title "New Emails"
```

### Tagging & Grouping

```bash
# Tag a toast for later updates
palantir -t "Download" -m "Starting..." --tag "download-1" --group "downloads"
```

### Update an Existing Toast

Update a previously shown toast's progress bar without creating a new notification:

```bash
# Show initial toast with tag
palantir -t "Downloading" --progress-title "file.zip" --progress-value 0.0 --progress-status "Starting..." --tag "dl-1"

# Update progress
palantir update --tag "dl-1" --progress-value 0.5 --progress-status "50%..."
palantir update --tag "dl-1" --progress-value 1.0 --progress-status "Complete!"
```

### Remove Toasts

```bash
# Remove a specific toast by tag
palantir remove --tag "dl-1"

# Remove all toasts in a group
palantir remove --group "downloads"
```

### Clear All History

```bash
palantir clear
```

### JSON Input

Load toast options from a JSON file or stdin:

```bash
# From file
palantir --json notification.json

# From stdin
echo '{"title":"Hello","message":"World","audio":"mail"}' | palantir --json -

# CLI options override JSON values
palantir --json notification.json -t "Override Title"
```

Example JSON file:

```json
{
  "title": "Deployment",
  "message": "Production v2.1.0 deployed",
  "attribution": "Via GitHub Actions",
  "audio": "reminder",
  "duration": "long",
  "buttons": ["View;https://app.example.com", "Dismiss"]
}
```

### Dry Run (Preview XML)

Output the generated toast XML without displaying it:

```bash
palantir -t "Test" -m "Hello" --button "OK" --dry-run
```

### Quiet Mode

```bash
# Suppress informational output
palantir -t "Hello" -m "World" -q

# Also works with subcommands
palantir clear -q
```

### Version

```bash
palantir --version
```

### Shell Completions

Generate tab-completion scripts for your shell:

```bash
# PowerShell — add to your $PROFILE
palantir completions powershell >> $PROFILE
```

## Kitchen Sink Example

A complex toast combining multiple features:

```bash
palantir \
  -t "Deployment Complete" \
  -m "Production v2.1.0 is live" \
  -b "All 142 tests passed" \
  --attribution "Via GitHub Actions" \
  --hero-image "https://example.com/banner.png" \
  --button "View;https://app.example.com" \
  --button "Dismiss" \
  -a reminder \
  --duration long \
  --launch "https://github.com/org/repo/releases" \
  --header-id "deployments" \
  --header-title "Deployments" \
  --tag "deploy-v2.1.0"
```

## All Options

```
Options:
  -t, --title              Toast title text (first line, bold)
  -m, --message            Toast body message text (second line)
  -b, --body               Additional body text (third line)
  --attribution            Attribution text at the bottom
  -i, --image              App logo override image (file path or URL)
  --crop-circle            Crop the app logo as a circle
  --hero-image             Hero image at the top of the toast
  --inline-image           Inline image in the toast body
  --button                 Button: "Label" or "Label;uri"
  --input                  Text input: "id" or "id;placeholder"
  --selection              Selection box: "id;Option A,Option B,Option C"
  -a, --audio              Audio sound name or file path
  -s, --silent             Suppress audio
  --loop                   Loop audio
  --duration               short or long
  --scenario               default, alarm, reminder, incomingCall
  --expiration             Auto-expire after N minutes
  --timestamp              Custom timestamp (ISO 8601)
  --progress-title         Progress bar title
  --progress-value         Progress value (0.0-1.0 or "indeterminate")
  --progress-value-string  Progress value display override
  --progress-status        Progress status text
  --tag                    Toast tag for updates
  --group                  Toast group for updates
  --header-id              Header ID for Action Center grouping
  --header-title           Header display title
  --header-arguments       Header activation arguments
  --launch                 URI to open on toast click
  --on-click               Shell command to run on activation (implies --wait)
  --preset                 Apply a named preset (use 'palantir preset list')
  --wait                   Block until interaction, output result as JSON
  --dry-run                Output toast XML without displaying
  --json                   Load options from JSON file (use "-" for stdin)
  --version                Show version information
  -q, --quiet              Suppress informational output

Commands:
  clear                    Clear all toast notification history
  remove                   Remove specific toasts (--tag or --group)
  update                   Update an existing toast's progress data
  preset                   Manage presets (save, list, show, delete)
  completions              Generate shell completion scripts
```

## Requirements

- Windows 10 (build 17763) or later
- .NET 10 SDK

## License

[Unlicense](LICENSE) - Public Domain
