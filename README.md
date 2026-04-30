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

# Verify the tool works
palantir test
```

> See [EXAMPLES.md](EXAMPLES.md) for a comprehensive, copy-paste-ready guide to every feature.

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

# Submit button (foreground activation — captures user input with --wait)
palantir -t "Reply" --input "reply;Type here..." --button "Send;submit" --wait
# Output: {"action":"activated","arguments":"button=Send","userInputs":{"reply":"hello"}}

# Protocol activation (opens URL/app)
palantir -t "Update" -m "New version" --button "Download;https://example.com" --button "Later"

# Multiple buttons
palantir -t "Call" -m "Incoming" --button "Answer;tel:+123" --button "Decline"

# Structured key-value format (alternative syntax)
palantir -t "Confirm" --button "label=Yes,action=submit" --button "label=No,action=dismiss"
palantir -t "Open" --button "label=View,action=https://example.com"
```

**Button formats:**

| Format | Example | Behavior |
|---|---|---|
| `"Label"` | `"OK"` | Dismiss button |
| `"Label;dismiss"` | `"Cancel;dismiss"` | Dismiss (explicit) |
| `"Label;submit"` | `"Send;submit"` | Foreground activation (captures inputs) |
| `"Label;uri"` | `"Open;https://..."` | Protocol activation (opens URI) |
| `"label=X,action=Y"` | `"label=OK,action=submit"` | Structured key-value format |

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

Block until the user interacts with the toast. The result is output to stdout.

```bash
# Wait and capture result (JSON by default)
palantir -t "Deploy?" -m "Push to production?" --button "Yes;submit" --button "No" --wait
# Output: {"action":"activated","arguments":"button=Yes"}
# Exit codes: 0=activated, 1=dismissed, 2=failed, 3=cancelled, 4=timedOut

# With timeout (auto-resolves after N seconds)
palantir -t "Confirm" --button "OK;submit" --wait --timeout 30
# Output if timed out: {"action":"timedOut"}

# Text format for easy shell parsing
palantir -t "Reply" --input "msg;Type here" --button "Send;submit" --wait --format text
# Output:
# action=activated
# arguments=button=Send
# input.msg=hello world

# No output, just exit code
palantir -t "Proceed?" --button "Yes;submit" --wait --format none --timeout 10
if ($LASTEXITCODE -eq 0) { Write-Host "User confirmed!" }

# Capture form input
$result = palantir -t "Quick Reply" -m "From: John" `
  --input "reply;Type your reply..." `
  --selection "priority;Low,Normal,High" `
  --button "Send;submit" --button "Ignore" --wait | ConvertFrom-Json
if ($result.action -eq "activated") {
    Write-Host "Reply: $($result.userInputs.reply)"
    Write-Host "Priority: $($result.userInputs.priority)"
}
```

### Replace an Existing Toast

Re-show a toast with entirely new content (requires `--tag`):

```bash
# Show initial toast
palantir -t "Step 1 of 3" -m "Preparing..." --tag "wizard"

# Replace with new content (re-pops the notification)
palantir -t "Step 2 of 3" -m "Building..." --tag "wizard" --replace
palantir -t "Step 3 of 3" -m "Done!" --tag "wizard" --replace
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

### View Active Notifications

```bash
palantir history
#   tag=dl-1             group=-                Downloading | file.zip
#   tag=build-1          group=ci               Build Complete | All tests passed
#
# Total: 2
```

### Test Notification

Verify Palantir is working correctly:

```bash
palantir test
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

## Styling, Layout & Rich Content

Windows toast notifications cannot render arbitrary colors, inline bold/italic,
or hyperlinks inside text — these are platform limitations, not Palantir's.
What you *can* do is layer three opt-in tiers of styling on top of the basics.
**All defaults are unchanged**; everything below is opt-in.

### Tier 1 — Per-line styling

Apply a `hint-style` and `hint-align` to any of the three top-level text lines.

```bash
palantir \
  -t  "Backup complete" --title-style large  --title-align center \
  -m  "12 files in 4.2 s" --message-style dim \
  -b  "Next run at 03:00" --body-style small --body-align right
```

Friendly aliases (case-insensitive):

| Alias    | Schema value (what is emitted) |
|----------|---------------------------------|
| `header` | `header`                        |
| `large`  | `title`                         |
| `normal` | `base`                          |
| `small`  | `caption`                       |
| `dim`    | `baseSubtle`                    |

You can also pass the raw schema value directly (e.g. `--title-style titleNumeral`,
`--message-style subheaderSubtle`). Anything in the toast schema's hint-style
catalog is accepted as-is.

Alignment accepts `left`, `center`, `right`.

### Tier 1.5 — Extra text lines

Append additional `<text>` lines beyond title/message/body. Each `--extra-text-style`
and `--extra-text-align` flag attaches to the **most recent** `--extra-text`.

```bash
palantir -t "Release notes" \
  --extra-text "v2.1.0" \
  --extra-text "10 fixes, 2 features" --extra-text-style dim \
  --extra-text "Released today"        --extra-text-align right
```

### Tier 2 — Multi-column / multi-row layout

Use `--column` (repeatable) for columns and `--column-row` to start a new row.
A column spec is `;`-separated `key=value` pairs:

```bash
palantir -t "Backup" \
  --column "text=Started:;style=dim" \
  --column "text=10:42 AM;align=right" \
  --column-row \
  --column "text=Files:;style=dim" \
  --column "text=1,234;align=right"
```

(For a single row of columns, omit `--column-row` entirely.)

### Tier 3 — Full XML control (escape hatches)

When the friendly options aren't enough, drop down to raw XML:

```bash
# Append a <text> verbatim with any schema attributes
palantir -t "Score" \
  --text-raw '<text hint-style="titleNumeral" hint-align="center">42</text>'

# Inject any element into <binding>, <actions>, or <toast>
palantir -t "Custom" \
  --xml-anchor actions \
  --xml-fragment '<action content="Custom" arguments="x" activationType="foreground"/>'

# Load a fragment from disk
palantir -t "Custom" --xml-fragment "@./fragment.xml"
```

`--xml-anchor` defaults to `binding` and applies to all subsequent
`--xml-fragment` flags until the next `--xml-anchor`.

By default the CLI does **not** validate raw XML (for speed); pass
`--validate-xml` to surface clear errors before sending. The library API
defaults the other way (`ValidateXml = true`).

### Color: emoji shortcodes (opt-in)

You can put emoji directly in any text field (`-t "✅ Done"`). For
script-friendly authoring, the `--expand-shortcodes` flag enables
GitHub-style codes:

```bash
palantir --expand-shortcodes \
  -t ":check: Backup complete" \
  -m ":warn: Disk almost full (:red_circle: 95%)"
```

Built-in shortcodes (curated set, hard-coded):

`:check:` `:x:` `:warn:` `:warning:` `:info:` `:question:` `:exclamation:`
`:red_circle:` `:green_circle:` `:yellow_circle:` `:blue_circle:`
`:white_circle:` `:black_circle:` `:bell:` `:hourglass:` `:rocket:`
`:fire:` `:sparkles:` `:lock:` `:unlock:` `:tada:` `:wave:` `:gear:`
`:wrench:` `:hammer:` `:package:` `:floppy_disk:` `:zap:` `:bug:`
`:mag:` `:eyes:` `:thumbsup:` `:thumbsdown:` `:heart:` `:star:`

Unknown codes are left as literal text. The shortcode dictionary is
hard-coded; to use your own emoji, place them directly in the text.

### What's *not* possible (platform limits)

Windows toasts will silently ignore — or outright reject — these things,
regardless of how Palantir tries:

- Custom RGB / hex colors on text.
- Inline `**bold**` / `*italic*` / underline / strike inside a text line.
- Hyperlinks embedded inside body text.
- HTML / Markdown / XAML rendering.
- Coloring the toast background (Windows controls that; it follows the system accent).

The standard workaround is a colored hero/inline image (`--hero-image`,
`--inline-image`) plus emoji.

## Personalities (Toast App Identity)

Every Windows toast carries an icon and app name in the top corner —
this is the *app* sending the toast (e.g. "Palantir"), set per-application
via Windows' AUMID + Start Menu shortcut mechanism. Palantir lets you define
named **personalities** that switch the corner icon and name **per-toast**.

```bash
# Register a personality (creates Start Menu shortcut + writes config)
palantir personality register \
  --name opencode \
  --display-name "OpenCode" \
  --icon https://opencode.ai/apple-touch-icon.png

# Use it on any toast
palantir --as opencode -t "Task complete" -m "Ready for input"

# Or set as default for all toasts
palantir personality use --name opencode
```

Now every `palantir --as opencode …` (or just `palantir …` if it's the
default) shows up in the Action Center labeled "OpenCode" with the OpenCode
icon at the corner.

### How registration works

- Personalities defined in `palantir.json` auto-register on first use
  (~50–100 ms first time, zero overhead after).
- Each personality gets a derived AUMID (`<aumidPrefix>.<name>`) and a
  Start Menu shortcut. Defaults to `Palantir.<name>`; override with
  `aumidPrefix` in config.
- PNG/JPG icons (local or HTTP) are auto-converted to ICO and cached.

### Lifecycle commands

| Command | Effect |
|---------|--------|
| `palantir personality register --name X --display-name Y --icon Z` | Register one (also writes to config) |
| `palantir personality unregister --name X` | Remove from Windows (config untouched) |
| `palantir personality delete --name X` | Remove from config (Windows untouched) |
| `palantir personality list` | Show config + Windows state side by side |
| `palantir personality register-all` | Register everything from config (idempotent) |
| `palantir personality unregister-all` | Remove all Palantir-managed registrations |
| `palantir personality sync` | Reconcile config ↔ Windows in both directions |
| `palantir personality prune` | Remove Windows entries no longer in config |
| `palantir personality use --name X` | Set default personality |

`unregister-all`, `sync`, and `prune` accept `--yes` (skip confirmation),
`--keep-history` (preserve Action Center entries), and `--dry-run`
(`sync` only).

### One-off overrides (no config required)

```bash
palantir --display-name "OpenCode" --app-icon ./opencode.png \
  -t ":check: Done" -m "Ready"
```

This still uses the personality registration mechanism under the hood
(creating a stable Start Menu shortcut keyed off the display name) so
subsequent calls with the same `--display-name` are zero-overhead.

### Built-in default personality

Every Palantir invocation that doesn't pass `--as`, `--display-name`/`--app-icon`,
or hit a `--preset` with `personality` set, automatically uses a built-in
**`palantir`** personality (display name "Palantir", icon = the Palantir
executable's embedded icon). It's auto-registered on first use and shown
in `personality list` as `[built-in,windows]`.

To customize it, just add a `personalities.palantir` entry to your config:

```json
{
  "personalities": {
    "palantir": {
      "displayName": "My Palantir",
      "icon": "C:\\path\\to\\custom-icon.png"
    }
  }
}
```

Bulk operations (`unregister-all`, `prune`, `sync`) deliberately skip the
built-in default — it would just get auto-recreated on the next toast.

### Config

```json
{
  "aumidPrefix": "Palantir",
  "defaultPersonality": "opencode",
  "personalities": {
    "opencode": {
      "displayName": "OpenCode",
      "icon": "C:\\path\\to\\opencode.ico"
    }
  }
}
```

Edit the JSON freely; run `palantir personality sync` to make Windows match.

## Cache & File Locations

Palantir splits **portable config** from **machine-local state**:

| File | Role | Default |
|------|------|---------|
| `palantir.json` | Definitions (presets, personalities, paths) | `$XDG_CONFIG_HOME/Palantir` or `%AppData%\Palantir` |
| `registry.json` | What this Windows install has registered | `$XDG_STATE_HOME/palantir` or `%LocalAppData%\Palantir\state` |
| Cache (icons, images) | Downloaded/derived artifacts | `$XDG_CACHE_HOME/palantir` or `%LocalAppData%\Palantir\cache` |

You can sync `palantir.json` across machines (it's logical-only); `registry.json` and the cache are recreated automatically on each machine.

### Resolved paths

```bash
palantir cache path
```

shows everything resolved on the current host.

### Override with `paths` in `palantir.json`

All keys optional; when set they win. Sub-paths derive from `cache` when not set explicitly.

```json
{
  "paths": {
    "cache":    "${PALANTIR_CONFIG}/cache",
    "icons":    "${LOCALAPPDATA}/Palantir/icons",
    "images":   "${XDG_CACHE_HOME}/palantir/images",
    "registry": "${XDG_STATE_HOME}/palantir/registry.json"
  }
}
```

### Token expansion

Available everywhere a path is accepted (including `personalities.*.icon`):

| Token | Expands to |
|-------|------------|
| `${ENV_VAR}` | Process environment value (any name) |
| `${PALANTIR_CONFIG}` | Resolved config directory |
| `${PALANTIR_CACHE}` | Resolved cache root |
| Leading `~` | User profile directory |

`%VAR%` Windows-style env expansion also works (via `Environment.ExpandEnvironmentVariables`).

### Resolution order

| Path | Order (first match wins) |
|------|--------------------------|
| Cache | `paths.cache` → `PALANTIR_CACHE_PATH` → `XDG_CACHE_HOME/palantir` → `XDG_CONFIG_HOME/palantir/cache` → `%LocalAppData%\Palantir\cache` |
| Icons | `paths.icons` → `PALANTIR_ICONS_PATH` → `<cache>/icons` |
| Images | `paths.images` → `PALANTIR_IMAGES_PATH` → `<cache>/images` |
| Registry | `paths.registry` → `PALANTIR_REGISTRY_PATH` → `XDG_STATE_HOME/palantir/registry.json` → `%LocalAppData%\Palantir\state\registry.json` |

### Cache management

```bash
palantir cache path                 # show resolved paths
palantir cache clear                # clear icons + images (with confirmation)
palantir cache clear --icons --yes  # targeted clear, no prompt
```

`cache clear` does **not** unregister personalities — those live in Windows.
Run `palantir personality unregister-all` if you also want to remove them.

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
  --button                 Button: "Label", "Label;submit", "Label;uri", or key-value
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
  --wait                   Block until interaction, output result
  --timeout                Timeout in seconds for --wait
  --format                 Output format for --wait: json, text, or none
  --replace                Replace existing toast with same --tag
  --dry-run                Output toast XML without displaying
  --json                   Load options from JSON file (use "-" for stdin)
  --version                Show version information

  Styling, layout & rich content (all opt-in):
  --title-style            Title hint-style (alias or schema value)
  --title-align            Title alignment: left, center, right
  --message-style          Message hint-style
  --message-align          Message alignment
  --body-style             Body hint-style
  --body-align             Body alignment
  --extra-text             Append a text line (repeatable)
  --extra-text-style       Style for the most recent --extra-text
  --extra-text-align       Alignment for the most recent --extra-text
  --column                 Column spec "text=...;style=...;align=..." (repeatable)
  --column-row             Start a new row of columns
  --text-raw               Append a verbatim <text> XML element (repeatable)
  --xml-fragment           Inject raw XML at --xml-anchor (use "@path" for file)
  --xml-anchor             binding (default), actions, or toast
  --validate-xml           Validate raw XML before sending
  --expand-shortcodes      Expand emoji shortcodes (:check: → ✅, etc.)

  Personality (toast app identity):
  --as <name>              Use a configured personality
  --display-name <name>    One-off override for corner app name
  --app-icon <path|url>    One-off override for corner app icon

  -q, --quiet              Suppress informational output

Commands:
  clear                    Clear all toast notification history
  remove                   Remove specific toasts (--tag or --group)
  update                   Update an existing toast's progress data
  preset                   Manage presets (save, list, show, delete)
  personality              Manage personalities (register, list, sync, etc.)
  cache                    Manage cache directories (path, clear)
  history                  List active toast notifications
  test                     Send a test notification
  completions              Generate shell completion scripts
```

## Requirements

- Windows 10 (build 17763) or later
- .NET 10 SDK

## More Examples

See [EXAMPLES.md](EXAMPLES.md) for a comprehensive guide with copy-paste-ready commands covering every feature, real-world scenarios, and PowerShell scripting patterns.

## Agent Skill

An [Agent Skill](https://agentskills.io) is bundled in `Skills/palantir-notifications/` so AI coding agents can learn how to use Palantir. Point your agent's skill directory at it, and it will know how to send notifications, capture user input, track progress, and integrate with your scripts.

## License

[Unlicense](LICENSE) - Public Domain
