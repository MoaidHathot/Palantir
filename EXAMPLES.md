# Palantir Examples

A hands-on guide to every Palantir feature. Copy-paste these commands to experiment.

---

## Table of Contents

- [Basic Notifications](#basic-notifications)
- [Text Content](#text-content)
- [Images](#images)
- [Buttons](#buttons)
- [Input Forms](#input-forms)
- [Audio](#audio)
- [Behavior & Timing](#behavior--timing)
- [Progress Bars](#progress-bars)
- [Wait for Interaction](#wait-for-interaction)
- [Timeout & Output Format](#timeout--output-format)
- [On-Click Commands](#on-click-commands)
- [Presets](#presets)
- [Tagging, Grouping & Headers](#tagging-grouping--headers)
- [Update, Replace & Remove](#update-replace--remove)
- [History](#history)
- [JSON Input](#json-input)
- [Stdin Pipe Support](#stdin-pipe-support)
- [Dry Run (Preview XML)](#dry-run-preview-xml)
- [Shell Completions](#shell-completions)
- [Styling, Layout & Rich Content](#styling-layout--rich-content)
- [Real-World Scenarios](#real-world-scenarios)

---

## Basic Notifications

```powershell
# Minimal — just a title
palantir -t "Hello World"

# Title + message
palantir -t "Hello" -m "World"

# Three lines of text
palantir -t "Title" -m "Message body" -b "Additional detail"

# With attribution (small text at the bottom)
palantir -t "Build Complete" -m "All tests passed" --attribution "Via CI Pipeline"

# Verify the tool works
palantir test
```

---

## Text Content

```powershell
# Title only (bold first line)
palantir -t "Alert!"

# Title + message
palantir -t "New Email" -m "You have 3 unread messages"

# All three text lines
palantir -t "Server Alert" -m "CPU usage at 95%" -b "Consider scaling up"

# Attribution text (appears small at the bottom)
palantir -t "Deploy" -m "Production updated" --attribution "Via GitHub Actions"

# Quiet mode — suppress console output, only show the toast
palantir -t "Stealth" -m "No console output" -q
```

---

## Images

```powershell
# App logo override (square)
palantir -t "Alert" -m "Check this" --image ./logo.png

# Circular app logo (great for avatars)
palantir -t "John Doe" -m "Online" --image ./avatar.png --crop-circle

# Hero image (large banner across the top)
palantir -t "Vacation" -m "Photo uploaded" --hero-image ./beach.jpg

# Inline image (in the toast body)
palantir -t "Chart" -m "Daily report" --inline-image ./chart.png

# Remote image via URL
palantir -t "News" -m "Breaking story" --hero-image "https://picsum.photos/364/180"

# Combine multiple images
palantir -t "User Profile" -m "Updated" `
  --image ./avatar.png --crop-circle `
  --hero-image ./banner.jpg
```

---

## Buttons

### Dismiss Buttons

```powershell
# Simple dismiss button
palantir -t "Info" -m "Acknowledged" --button "OK"

# Explicit dismiss keyword
palantir -t "Notice" -m "Read this" --button "Got it;dismiss"

# Multiple dismiss buttons
palantir -t "Choice" -m "Pick one" --button "Yes" --button "No" --button "Maybe"
```

### Submit Buttons (Foreground Activation)

Submit buttons trigger the `Activated` event — this is how you capture user input.

```powershell
# Simple submit button (use with --wait to capture the result)
palantir -t "Confirm" -m "Are you sure?" --button "Yes;submit" --button "No" --wait

# Submit with input capture
palantir -t "Reply" -m "From: John" `
  --input "reply;Type your reply..." `
  --button "Send;submit" --button "Ignore" --wait

# Submit with selection capture
palantir -t "Feedback" -m "Rate this build" `
  --selection "rating;Great,Good,OK,Bad" `
  --button "Submit;submit" --wait
```

### Protocol Buttons (Open URI)

```powershell
# Open a URL
palantir -t "Update" -m "New version available" `
  --button "Download;https://example.com" --button "Later"

# Open an app via protocol
palantir -t "Call" -m "Incoming" --button "Answer;tel:+1234567890" --button "Decline"

# Open a file
palantir -t "Report" -m "Ready" --button "Open;file:///C:/Reports/daily.pdf"
```

### Structured Key-Value Format

Alternative syntax for complex buttons.

```powershell
# Submit with custom arguments
palantir -t "Action" `
  --button "label=Approve,action=submit,arguments=approved" `
  --button "label=Reject,action=submit,arguments=rejected" `
  --wait

# Mix of button types
palantir -t "Options" `
  --button "label=Save,action=submit" `
  --button "label=Open Docs,action=https://docs.example.com" `
  --button "label=Cancel,action=dismiss"
```

---

## Input Forms

### Text Input

```powershell
# Single text input
palantir -t "Quick Note" --input "note;Type a note..." --button "Save;submit" --wait

# Multiple text inputs
palantir -t "Login" -m "Enter credentials" `
  --input "user;Username" `
  --input "pass;Password" `
  --button "Login;submit" --wait

# Input with placeholder
palantir -t "Search" --input "query;Search for..." --button "Go;submit" --wait
```

### Selection (Combo Box)

```powershell
# Simple selection
palantir -t "Choose" -m "Pick a color" `
  --selection "color;Red,Green,Blue,Yellow" `
  --button "OK;submit" --wait

# Selection + text input together
palantir -t "Bug Report" `
  --input "description;Describe the issue..." `
  --selection "severity;Critical,High,Medium,Low" `
  --selection "component;Frontend,Backend,Database,API" `
  --button "Submit;submit" --button "Cancel" --wait
```

### Capturing Input Results

```powershell
# Capture as JSON and process in PowerShell
$result = palantir -t "Quick Reply" -m "Message from Alice" `
  --input "reply;Type here..." `
  --selection "priority;Low,Normal,Urgent" `
  --button "Send;submit" --button "Ignore" `
  --wait | ConvertFrom-Json

if ($result.action -eq "activated") {
    Write-Host "Reply: $($result.userInputs.reply)"
    Write-Host "Priority: $($result.userInputs.priority)"
}

# Using text format for shell parsing
palantir -t "Name" --input "name;Your name" --button "OK;submit" --wait --format text
# Output:
#   action=activated
#   arguments=button=OK
#   input.name=John
```

---

## Audio

```powershell
# Named sounds
palantir -t "Default" -m "Default sound" -a default
palantir -t "IM" -m "Instant message sound" -a im
palantir -t "Mail" -m "You've got mail" -a mail
palantir -t "Reminder" -m "Don't forget" -a reminder
palantir -t "SMS" -m "New text message" -a sms

# Alarm sounds (alarm, alarm2 through alarm10)
palantir -t "Alarm" -m "Wake up!" -a alarm
palantir -t "Alarm 5" -m "Alternative alarm" -a alarm5

# Call sounds (call, call2 through call10)
palantir -t "Ringing" -m "Incoming call" -a call
palantir -t "Call 3" -m "Alternative ring" -a call3

# Looping audio (pair with --duration long)
palantir -t "ALERT" -m "Server down!" -a alarm --loop --duration long

# Silent notification (no sound at all)
palantir -t "Quiet" -m "No disturbance" -s

# Raw ms-winsoundevent URI
palantir -t "Custom" -m "Raw URI" -a "ms-winsoundevent:Notification.Looping.Alarm"
```

---

## Behavior & Timing

```powershell
# Short duration (~5 seconds, default)
palantir -t "Quick" -m "Gone in 5 seconds"

# Long duration (~25 seconds)
palantir -t "Read This" -m "Important info" --duration long

# Alarm scenario (stays on screen until dismissed)
palantir -t "ALARM" -m "Server is on fire!" --scenario alarm -a alarm --loop

# Reminder scenario (displays persistently)
palantir -t "Meeting" -m "Standup in 5 minutes" --scenario reminder

# Incoming call scenario
palantir -t "John Doe" -m "Incoming call" --scenario incomingCall -a call --loop

# Auto-expire after N minutes
palantir -t "Temp" -m "Expires in 1 minute" --expiration 1

# Custom display timestamp
palantir -t "Scheduled" -m "Originally planned for 9 AM" --timestamp "2025-06-15T09:00:00"
```

---

## Progress Bars

### Show Progress

```powershell
# Determinate progress (0.0 to 1.0)
palantir -t "Downloading" `
  --progress-title "file.zip" `
  --progress-value 0.6 `
  --progress-status "Downloading..."

# With custom value string
palantir -t "Syncing" `
  --progress-title "Playlist" `
  --progress-value 0.3 `
  --progress-value-string "3/10 songs" `
  --progress-status "Syncing..."

# Indeterminate progress (spinning)
palantir -t "Processing" `
  --progress-title "Please wait" `
  --progress-value indeterminate `
  --progress-status "Working..."
```

### Update Progress (without new popup)

```powershell
# Step 1: Show initial toast with a tag
palantir -t "Downloading" `
  --progress-title "file.zip" `
  --progress-value 0.0 `
  --progress-status "Starting..." `
  --tag "dl-1"

# Step 2: Update progress silently
palantir update --tag "dl-1" --progress-value 0.25 --progress-status "25%..."
palantir update --tag "dl-1" --progress-value 0.50 --progress-status "50%..."
palantir update --tag "dl-1" --progress-value 0.75 --progress-status "75%..."
palantir update --tag "dl-1" --progress-value 1.0 --progress-status "Complete!"
```

### Simulated Download (PowerShell Script)

```powershell
# Show toast
palantir -t "Downloading" `
  --progress-title "big-file.zip" `
  --progress-value 0.0 `
  --progress-status "Starting..." `
  --tag "sim-dl" -q

# Simulate progress
for ($i = 1; $i -le 10; $i++) {
    Start-Sleep -Milliseconds 500
    $val = $i / 10.0
    palantir update --tag "sim-dl" `
      --progress-value $val `
      --progress-status "$($i * 10)% complete" `
      --progress-value-string "$i/10 MB" -q
}
```

---

## Wait for Interaction

### Basic Wait

```powershell
# Wait for the user to click or dismiss
palantir -t "Confirm" -m "Continue?" --button "Yes;submit" --button "No" --wait

# JSON output (default):
# {"action":"activated","arguments":"button=Yes"}
# or
# {"action":"dismissed","reason":"UserCanceled"}
```

### Exit Codes

```powershell
# Exit codes for scripting:
#   0 = activated (user clicked a submit button or toast body)
#   1 = dismissed (user swiped away or clicked X)
#   2 = failed (toast failed to display)
#   3 = cancelled (Ctrl+C)
#   4 = timedOut (--timeout expired)

palantir -t "Proceed?" --button "Go;submit" --wait --format none
switch ($LASTEXITCODE) {
    0 { Write-Host "User confirmed" }
    1 { Write-Host "User dismissed" }
    4 { Write-Host "Timed out" }
}
```

---

## Timeout & Output Format

### Timeout

```powershell
# Auto-resolve after 10 seconds
palantir -t "Quick question" --button "Yes;submit" --wait --timeout 10

# Timeout with on-click (only runs if user clicks before timeout)
palantir -t "Open report?" --button "Open;submit" --wait --timeout 15 `
  --on-click "explorer C:\Reports"
```

### Output Formats

```powershell
# JSON (default) — best for PowerShell / programmatic use
palantir -t "Test" --button "OK;submit" --wait --format json
# {"action":"activated","arguments":"button=OK"}

# Text — key=value lines, one per line
palantir -t "Test" --input "name;Name" --button "OK;submit" --wait --format text
# action=activated
# arguments=button=OK
# input.name=John

# None — no output, just exit code
palantir -t "Confirm?" --button "OK;submit" --wait --format none --timeout 5
echo "Exit code: $LASTEXITCODE"
```

---

## On-Click Commands

Execute a shell command when the toast is activated (implies `--wait`).

```powershell
# Open a folder
palantir -t "Build Done" -m "Open output?" --on-click "explorer .\bin\Release"

# Open a URL in the browser
palantir -t "Report Ready" -m "View results?" --on-click "start https://example.com/report"

# Run a script
palantir -t "Deploy Complete" -m "Run smoke tests?" --on-click "powershell -File tests.ps1"
```

---

## Presets

### Built-in Presets

```powershell
# Alarm: scenario=alarm, audio=alarm, loop=true, duration=long
palantir -t "WAKE UP!" -m "It's 7 AM" --preset alarm

# Reminder: scenario=reminder, audio=reminder, duration=long
palantir -t "Meeting" -m "Standup in 5 minutes" --preset reminder

# Call: scenario=incomingCall, audio=call, loop=true, duration=long
palantir -t "Incoming Call" -m "John Doe" --preset call

# Override a preset value
palantir -t "Custom Alarm" --preset alarm -a mail   # Uses mail sound instead of alarm
```

### Create Custom Presets

```powershell
# From inline JSON
palantir preset save deploy '{"scenario":"reminder","audio":"mail","duration":"long","attribution":"Via CI/CD"}'

# From a file
palantir preset save my-alert alert-template.json

# From stdin
echo '{"audio":"im","duration":"short"}' | palantir preset save quick-ping

# Preset with buttons
palantir preset save confirm '{"buttons":["Yes;submit","No"],"wait":true,"timeout":30}'
```

### Manage Presets

```powershell
# List all presets
palantir preset list

# Show a preset's configuration
palantir preset show deploy

# Use a custom preset
palantir -t "Deployed!" -m "v2.1.0 is live" --preset deploy

# Delete a custom preset
palantir preset delete deploy

# Override a built-in preset (your version takes precedence)
palantir preset save alarm '{"scenario":"alarm","audio":"alarm3","loop":true,"duration":"long"}'

# Restore the built-in by deleting your override
palantir preset delete alarm
```

### Config File

The config file is located at (first match wins):
1. `$env:PALANTIR_CONFIG_PATH\palantir.json`
2. `$env:XDG_CONFIG_HOME\Palantir\palantir.json`
3. `$env:APPDATA\Palantir\palantir.json`

```powershell
# Find your config file location
palantir preset list   # Shows path at the bottom
```

Example `palantir.json`:

```json
{
  "presets": {
    "deploy": {
      "scenario": "reminder",
      "audio": "mail",
      "duration": "long",
      "attribution": "Via CI/CD"
    },
    "quick-ping": {
      "audio": "im",
      "duration": "short"
    },
    "urgent": {
      "scenario": "alarm",
      "audio": "alarm",
      "loop": true,
      "duration": "long"
    }
  }
}
```

---

## Tagging, Grouping & Headers

### Tags (Identify Individual Toasts)

```powershell
# Tag a toast so you can update/remove it later
palantir -t "Download" -m "Starting..." --tag "dl-1"

# Tag + group
palantir -t "Build #101" -m "Running..." --tag "build-101" --group "ci"
palantir -t "Build #102" -m "Queued" --tag "build-102" --group "ci"
```

### Headers (Action Center Grouping)

```powershell
# Group related toasts under a header in Action Center
palantir -t "PR #42 merged" --header-id "github" --header-title "GitHub"
palantir -t "Issue #15 closed" --header-id "github" --header-title "GitHub"
palantir -t "New release v3.0" --header-id "github" --header-title "GitHub"

# Different headers for different categories
palantir -t "Meeting at 3 PM" --header-id "calendar" --header-title "Calendar"
palantir -t "New message from Alice" --header-id "chat" --header-title "Chat"
```

### Replace (Re-show with New Content)

```powershell
# Show a wizard-style sequence
palantir -t "Step 1 of 3" -m "Preparing environment..." --tag "wizard"
Start-Sleep 2
palantir -t "Step 2 of 3" -m "Building project..." --tag "wizard" --replace
Start-Sleep 2
palantir -t "Step 3 of 3" -m "All done!" --tag "wizard" --replace
```

---

## Update, Replace & Remove

### Update (Data Binding Only — No New Popup)

```powershell
# Update progress bar data without re-showing the toast
palantir update --tag "dl-1" --progress-value 0.5 --progress-status "Halfway there"
palantir update --tag "dl-1" --progress-value 1.0 --progress-status "Done!"

# Update within a group
palantir update --tag "build-101" --group "ci" --progress-status "Tests passed"
```

### Replace (Full Content Replacement — Shows New Popup)

```powershell
# Replace entire toast content
palantir -t "Status: Running" --tag "job-1"
Start-Sleep 3
palantir -t "Status: Complete" -m "All checks passed" --tag "job-1" --replace
```

### Remove (Dismiss Programmatically)

```powershell
# Remove a specific toast
palantir remove --tag "dl-1"

# Remove all toasts in a group
palantir remove --group "ci"
```

### Clear All

```powershell
palantir clear
palantir clear -q   # No console output
```

---

## History

```powershell
# List all active notifications
palantir history
#   tag=dl-1             group=-                Downloading | file.zip
#   tag=build-101        group=ci               Build #101 | Running...
#
# Total: 2
```

---

## JSON Input

### From File

```powershell
# Load all options from a JSON file
palantir --json notification.json
```

Example `notification.json`:

```json
{
  "title": "Deployment Complete",
  "message": "v2.1.0 deployed to production",
  "attribution": "Via GitHub Actions",
  "audio": "mail",
  "duration": "long",
  "buttons": ["View;https://app.example.com", "Dismiss"],
  "headerId": "deployments",
  "headerTitle": "Deployments"
}
```

### From Stdin

```powershell
echo '{"title":"Hello","message":"From stdin","audio":"im"}' | palantir --json -
```

### CLI Overrides JSON

```powershell
# Title from CLI overrides the one in JSON
palantir --json notification.json -t "Custom Title"
```

---

## Stdin Pipe Support

Use `"-"` as the value for `--title`, `--message`, or `--body` to read from stdin.

```powershell
# Pipe a command's output into the message
git log -1 --oneline | palantir -t "Latest Commit" -m -

# Pipe into title
hostname | palantir -t - -m "System notification"

# Pipe a file's content
Get-Content .\status.txt | palantir -t "Status Update" -m -

# Pipe error output
dotnet build 2>&1 | Select-Object -Last 5 | Out-String | palantir -t "Build Output" -m -
```

---

## Dry Run (Preview XML)

Inspect the generated toast XML without actually displaying anything.

```powershell
# See the XML that would be sent
palantir -t "Test" -m "Hello" --button "OK" --dry-run

# Preview a complex toast
palantir -t "Deploy" -m "v2.0" `
  --button "View;https://app.example.com" `
  --button "Dismiss" `
  -a reminder --duration long `
  --progress-title "Deploy" --progress-value 0.5 `
  --dry-run

# Pipe to a file for inspection
palantir -t "Test" --dry-run > toast.xml
```

---

## Shell Completions

```powershell
# Generate and install PowerShell completions
palantir completions powershell >> $PROFILE

# Or evaluate directly in the current session
palantir completions powershell | Invoke-Expression
```

---

## Styling, Layout & Rich Content

Windows toasts can't render arbitrary colors or inline-bold text — that's a
platform limit. What Palantir gives you is three opt-in tiers of control on
top of the basics. **Defaults don't change**; everything below is opt-in.

### Per-line text styling

```bash
# Friendly aliases: header, large, normal, small, dim
palantir \
  -t "Backup Complete" --title-style large --title-align center \
  -m "12 files in 4.2 s" --message-style dim \
  -b "Next run: 03:00" --body-style small --body-align right
```

You can also pass any raw toast schema value directly:

```bash
palantir -t "42" --title-style titleNumeral --title-align center
```

### Extra text lines

`--extra-text-style` and `--extra-text-align` attach to the **most recent**
`--extra-text` (left-to-right).

```bash
palantir -t "Release v2.1.0" \
  --extra-text "10 fixes, 2 features" --extra-text-style dim \
  --extra-text "Released today"        --extra-text-align right
```

### Multi-column / multi-row layout

```bash
palantir -t "Backup" \
  --column "text=Started:;style=dim" \
  --column "text=10:42 AM;align=right" \
  --column-row \
  --column "text=Files:;style=dim" \
  --column "text=1,234;align=right"
```

For a single row, just omit `--column-row`.

### Full XML control (escape hatches)

```bash
# A verbatim <text> with any schema attributes
palantir -t "Score" \
  --text-raw '<text hint-style="titleNumeral" hint-align="center">42</text>'

# Inject custom XML at any anchor
palantir -t "Custom" \
  --xml-anchor actions \
  --xml-fragment '<action content="Snooze" arguments="snooze" activationType="background"/>'

# Load a fragment from disk
palantir -t "Custom" --xml-fragment "@./fragment.xml"

# Validate raw XML before sending (CLI off by default; library on by default)
palantir -t "Score" --validate-xml \
  --text-raw '<text hint-style="titleNumeral">42</text>'
```

### Emoji shortcodes (opt-in)

```bash
palantir --expand-shortcodes \
  -t ":check: Backup complete" \
  -m ":warn: Disk almost full (:red_circle: 95%)"
```

Available codes: `:check:` `:x:` `:warn:` `:info:` `:question:` `:exclamation:`
`:red_circle:` `:green_circle:` `:yellow_circle:` `:blue_circle:` `:white_circle:`
`:black_circle:` `:bell:` `:hourglass:` `:rocket:` `:fire:` `:sparkles:` `:lock:`
`:unlock:` `:tada:` `:wave:` `:gear:` `:wrench:` `:hammer:` `:package:`
`:floppy_disk:` `:zap:` `:bug:` `:mag:` `:eyes:` `:thumbsup:` `:thumbsdown:`
`:heart:` `:star:`

To use your own emoji, just put them directly in any text field —
no flag required.

### What's *not* possible

- Custom RGB/hex text colors
- Inline `**bold**` / `*italic*` / underline
- Hyperlinks inside body text
- HTML / Markdown / XAML

Workaround: use a colored `--hero-image` or `--inline-image` plus emoji.

---

## Personalities (Toast App Identity)

Personalities change the corner icon and app name Windows shows on the toast.
Register once, switch per toast.

```bash
# Register
palantir personality register \
  --name opencode \
  --display-name "OpenCode" \
  --icon https://opencode.ai/apple-touch-icon.png

# Use per-toast
palantir --as opencode -t "Task complete" -m "Ready for input"

# Or set as default
palantir personality use --name opencode
palantir -t "All toasts now branded"   # uses opencode by default

# One-off, no config (still cached)
palantir --display-name "OpenCode" --app-icon ./opencode.png \
  -t ":check: Done" -m "Ready"
```

### Inspecting + lifecycle

```bash
palantir personality list             # config + Windows state
palantir personality register-all     # warm everything in config
palantir personality sync             # reconcile both directions
palantir personality sync --dry-run   # preview without changes
palantir personality prune --yes      # remove stale Windows entries only
palantir personality unregister-all   # nuke all (with confirmation)
palantir personality unregister --name opencode --keep-history
palantir personality delete --name opencode   # remove from config only
```

### Cache & paths

```bash
palantir cache path                   # show resolved directories
palantir cache clear --icons --yes    # clear icon cache
```

Override locations in `palantir.json`:

```json
{
  "paths": {
    "cache": "D:\\palantir-cache",
    "icons": "D:\\palantir-cache\\icons"
  }
}
```

Or via env vars: `PALANTIR_CACHE_PATH`, `PALANTIR_ICONS_PATH`,
`PALANTIR_IMAGES_PATH`, `PALANTIR_REGISTRY_PATH`.

---

## Real-World Scenarios

### CI/CD Build Notification

```powershell
dotnet build 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    palantir -t "Build Succeeded" -m "All targets built" --preset build-ok -q
} else {
    palantir -t "Build Failed" -m "Check the output" --preset build-fail -q
}
```

### Long-Running Task with Progress

```powershell
$files = Get-ChildItem .\data -File
$total = $files.Count

palantir -t "Processing" `
  --progress-title "Data files" `
  --progress-value 0.0 `
  --progress-status "Starting..." `
  --tag "proc" -q

for ($i = 0; $i -lt $total; $i++) {
    # ... process $files[$i] ...
    Start-Sleep -Milliseconds 200
    $pct = ($i + 1) / $total
    palantir update --tag "proc" `
      --progress-value $pct `
      --progress-value-string "$($i+1)/$total files" `
      --progress-status "Processing..." -q
}

palantir -t "Done" -m "All $total files processed" --tag "proc" --replace -q
```

### Interactive Confirmation Dialog

```powershell
$result = palantir -t "Deploy to Production" `
  -m "This will update the live environment" `
  -b "Last deploy: 2 hours ago" `
  --button "Deploy;submit" `
  --button "Cancel" `
  --wait --timeout 60 | ConvertFrom-Json

switch ($result.action) {
    "activated" {
        Write-Host "Deploying..."
        # ./deploy.ps1
    }
    "dismissed" { Write-Host "Cancelled by user" }
    "timedOut"  { Write-Host "No response, aborting" }
}
```

### Quick Reply Form

```powershell
$result = palantir -t "Message from Alice" -m "Hey, are you free for lunch?" `
  --input "reply;Type your reply..." `
  --selection "when;Now,In 30 min,In 1 hour,Can't today" `
  --button "Send;submit" --button "Ignore" `
  --wait --timeout 120 | ConvertFrom-Json

if ($result.action -eq "activated" -and $result.userInputs) {
    $reply = $result.userInputs.reply
    $when = $result.userInputs.when
    Write-Host "Sending reply: '$reply' (available: $when)"
}
```

### Scheduled Reminders (Using PowerShell)

```powershell
# Remind in 5 minutes
Start-Job {
    Start-Sleep -Seconds 300
    palantir -t "Reminder" -m "Time to stretch!" --preset reminder
}

# Remind at a specific time
$target = [DateTime]"17:00"
$delay = ($target - (Get-Date)).TotalSeconds
if ($delay -gt 0) {
    Start-Job -ArgumentList $delay {
        param($s)
        Start-Sleep -Seconds $s
        palantir -t "End of Day" -m "Time to wrap up" --preset reminder
    }
}
```

### Multi-Step Notification Workflow

```powershell
# Step 1: Show starting notification
palantir -t "Deployment" -m "Starting..." `
  --progress-value indeterminate `
  --progress-status "Initializing" `
  --tag "deploy" -q

Start-Sleep 2

# Step 2: Update progress
palantir update --tag "deploy" --progress-value 0.3 --progress-status "Building..." -q
Start-Sleep 2

palantir update --tag "deploy" --progress-value 0.6 --progress-status "Testing..." -q
Start-Sleep 2

palantir update --tag "deploy" --progress-value 0.9 --progress-status "Deploying..." -q
Start-Sleep 1

# Step 3: Replace with final result
palantir -t "Deployment Complete" `
  -m "v2.1.0 deployed to production" `
  --button "View;https://app.example.com" `
  --button "Dismiss" `
  -a mail `
  --tag "deploy" --replace -q
```

### Monitoring Dashboard Alerts

```powershell
# Send categorized alerts using headers
function Send-Alert {
    param($Title, $Message, $Category, $Severity)

    $preset = switch ($Severity) {
        "critical" { "urgent" }
        "warning"  { "reminder" }
        default    { "ping" }
    }

    palantir -t $Title -m $Message `
      --header-id $Category --header-title $Category `
      --preset $preset -q
}

Send-Alert "CPU 95%" "web-server-01" "Infrastructure" "critical"
Send-Alert "Disk 80%" "db-server-02" "Infrastructure" "warning"
Send-Alert "New signup" "user@example.com" "Activity" "info"
```
