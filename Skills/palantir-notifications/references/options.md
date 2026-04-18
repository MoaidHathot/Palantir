# Palantir — Complete Options Reference

## Global Options

| Option | Short | Type | Description |
|---|---|---|---|
| `--title` | `-t` | string | Toast title text (first line, bold) |
| `--message` | `-m` | string | Toast body message text (second line). Use `"-"` for stdin |
| `--body` | `-b` | string | Additional body text (third line). Use `"-"` for stdin |
| `--attribution` | | string | Attribution text at the bottom |
| `--image` | `-i` | string | App logo override image (file path or URL) |
| `--crop-circle` | | flag | Crop the app logo as a circle |
| `--hero-image` | | string | Hero image at the top of the toast |
| `--inline-image` | | string | Inline image in the toast body |
| `--button` | | string[] | Button spec (see Button Formats below) |
| `--input` | | string[] | Text input: `"id"` or `"id;placeholder"` |
| `--selection` | | string[] | Selection box: `"id;Option A,Option B,Option C"` |
| `--audio` | `-a` | string | Sound name or file path |
| `--silent` | `-s` | flag | Suppress audio |
| `--loop` | | flag | Loop audio (use with `--duration long`) |
| `--duration` | | string | `short` (~5s) or `long` (~25s) |
| `--scenario` | | string | `default`, `alarm`, `reminder`, or `incomingCall` |
| `--expiration` | | int | Auto-expire after N minutes |
| `--timestamp` | | string | Custom timestamp (ISO 8601) |
| `--progress-title` | | string | Progress bar title |
| `--progress-value` | | string | Progress value: `0.0`-`1.0` or `"indeterminate"` |
| `--progress-value-string` | | string | Progress value display override (e.g. `"3/10 songs"`) |
| `--progress-status` | | string | Progress status text |
| `--tag` | | string | Toast tag for identifying/updating toasts |
| `--group` | | string | Toast group for organizing toasts |
| `--header-id` | | string | Header ID for Action Center grouping |
| `--header-title` | | string | Header display title |
| `--header-arguments` | | string | Header activation arguments |
| `--launch` | | string | URI to open when toast body is clicked |
| `--on-click` | | string | Shell command to run on activation (implies `--wait`) |
| `--preset` | | string | Apply a named preset |
| `--wait` | | flag | Block until interaction, output result |
| `--timeout` | | int | Timeout in seconds for `--wait` |
| `--format` | | string | Output format for `--wait`: `json`, `text`, or `none` |
| `--replace` | | flag | Replace existing toast with same `--tag` |
| `--dry-run` | | flag | Output toast XML without displaying |
| `--json` | | string | Load options from JSON file (use `"-"` for stdin) |
| `--version` | | flag | Show version information |
| `--quiet` | `-q` | flag | Suppress informational console output |

## Button Formats

### Semicolon format (legacy + extended)

| Format | Example | Behavior |
|---|---|---|
| `"Label"` | `"OK"` | Dismiss button |
| `"Label;dismiss"` | `"Cancel;dismiss"` | Dismiss (explicit) |
| `"Label;submit"` | `"Send;submit"` | Foreground activation (captures user input) |
| `"Label;uri"` | `"Open;https://..."` | Protocol activation (opens URI) |

### Structured key-value format

Format: `"key=value,key=value,..."`

| Key | Required | Values |
|---|---|---|
| `label` | Yes | Button display text |
| `action` | No | `submit`, `dismiss`, or a URI (default: `dismiss`) |
| `arguments` | No | Custom activation arguments (default: label text) |

Example: `"label=Send,action=submit,arguments=send-reply"`

## Named Audio Sounds

| Name | Sound |
|---|---|
| `default` | Default notification |
| `im` | Instant message |
| `mail` | New mail |
| `reminder` | Reminder |
| `sms` | SMS/text message |
| `alarm` through `alarm10` | Alarm (looping) |
| `call` through `call10` | Phone call (looping) |

Also accepts: `ms-winsoundevent:` URIs or local file paths.

## Subcommands

| Command | Description |
|---|---|
| `clear` | Clear all toast notification history |
| `remove --tag X [--group Y]` | Remove a specific toast |
| `remove --group Y` | Remove all toasts in a group |
| `update --tag X [options]` | Update progress bar data on an existing toast |
| `history` | List active toast notifications |
| `test` | Send a test notification |
| `preset save <name> <json>` | Save a preset from JSON string, file, or stdin |
| `preset list` | List all presets (built-in + user) |
| `preset show <name>` | Show a preset's JSON configuration |
| `preset delete <name>` | Delete a user preset |
| `completions powershell` | Generate PowerShell completion script |

## Wait Result Schema

When using `--wait`, the JSON output follows this schema:

```json
{
  "action": "activated | dismissed | failed | cancelled | timedOut",
  "arguments": "button=ButtonLabel",
  "reason": "UserCanceled | TimedOut | ApplicationHidden",
  "error": "error message if failed",
  "userInputs": {
    "inputId": "user's typed value",
    "selectionId": "selected option"
  }
}
```

Fields are omitted when null. Only `action` is always present.

### Exit codes

| Code | Action |
|---|---|
| 0 | activated |
| 1 | dismissed |
| 2 | failed |
| 3 | cancelled |
| 4 | timedOut |
| 5 | unknown |

## Config File Location

Resolution order (first match wins):

1. `$env:PALANTIR_CONFIG_PATH` directory
2. `$env:XDG_CONFIG_HOME/Palantir/`
3. `$env:APPDATA/Palantir/`

File: `palantir.json`

```json
{
  "presets": {
    "preset-name": {
      "audio": "mail",
      "duration": "long"
    }
  }
}
```
