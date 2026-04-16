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
```

## Features

### Text Content

| Option | Short | Description |
|---|---|---|
| `--title` | `-t` | Toast title (first line, bold) |
| `--message` | `-m` | Body message (second line) |
| `--body` | `-b` | Additional text (third line) |
| `--attribution` | | Attribution text at the bottom |

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
palantir -t "Email" -m "New message" --audio mail

# Looping alarm sounds: alarm, alarm2-alarm10, call, call2-call10
palantir -t "Alarm" -m "Wake up!" --audio alarm --loop --duration long

# Silent notification
palantir -t "Quiet" -m "No sound" --silent
```

| Option | Description |
|---|---|
| `--audio` | Sound name or file path |
| `--silent` | No sound |
| `--loop` | Loop the audio (use with `--duration long`) |

### Behavior

```bash
# Long duration (~25 seconds instead of ~5)
palantir -t "Important" -m "Read this" --duration long

# Alarm scenario (stays on screen, looping audio)
palantir -t "ALARM" -m "Server down!" --scenario alarm --audio alarm --loop

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

### Tagging & Grouping

```bash
# Tag a toast for later updates
palantir -t "Download" -m "Starting..." --tag "download-1" --group "downloads"
```

### Clear History

```bash
palantir clear
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
  --audio                  Audio sound name or file path
  --silent                 Suppress audio
  --loop                   Loop audio
  --duration               short or long
  --scenario               default, alarm, reminder, incomingCall
  --expiration             Auto-expire after N minutes
  --timestamp              Custom timestamp (ISO 8601)
  --progress-title         Progress bar title
  --progress-value         Progress value (0.0-1.0 or "indeterminate")
  --progress-value-string  Progress value display override
  --progress-status        Progress status text
  --app-id                 Application User Model ID
  --tag                    Toast tag for updates
  --group                  Toast group for updates
  --launch                 URI to open on toast click

Commands:
  clear                    Clear all toast notification history
```

## Requirements

- Windows 10 (build 17763) or later
- .NET 10 SDK

## License

[Unlicense](LICENSE) - Public Domain
