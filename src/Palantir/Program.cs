using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.CommandLine;
using Palantir;
using Windows.UI.Notifications;

// ── Version ────────────────────────────────────────────────────────

var version = typeof(ToastService).Assembly
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
    ?.InformationalVersion ?? "0.0.0";

// ── Text Content ────────────────────────────────────────────────────

var titleOption = new Option<string?>("--title", "-t")
{ Description = "Toast title text (first line, bold)" };

var messageOption = new Option<string?>("--message", "-m")
{ Description = "Toast body message text (second line). Use \"-\" to read from stdin" };

var bodyOption = new Option<string?>("--body", "-b")
{ Description = "Additional body text (third line). Use \"-\" to read from stdin" };

var attributionOption = new Option<string?>("--attribution")
{ Description = "Attribution text shown at the bottom (e.g. \"Via Palantir\")" };

// ── Images ──────────────────────────────────────────────────────────

var imageOption = new Option<string?>("--image", "-i")
{ Description = "App logo override image (file path or http/https URL)" };

var cropCircleOption = new Option<bool>("--crop-circle")
{ Description = "Crop the app logo image as a circle" };

var heroImageOption = new Option<string?>("--hero-image")
{ Description = "Hero image displayed at the top of the toast (file path or URL)" };

var inlineImageOption = new Option<string?>("--inline-image")
{ Description = "Inline image shown within the toast body (file path or URL)" };

// ── Buttons & Inputs ────────────────────────────────────────────────

var buttonOption = new Option<string[]>("--button")
{ Description = "Add a button. Use \"Label\" for dismiss or \"Label;uri\" for protocol activation" };

var inputOption = new Option<string[]>("--input")
{ Description = "Add a text input box. Use \"id\" or \"id;placeholder\"" };

var selectionOption = new Option<string[]>("--selection")
{ Description = "Add a selection box. Format: \"id;Option A,Option B,Option C\"" };

// ── Audio ───────────────────────────────────────────────────────────

var audioOption = new Option<string?>("--audio", "-a")
{ Description = "Audio sound name (default, im, mail, reminder, sms, alarm, call, etc.) or file path" };

var silentOption = new Option<bool>("--silent", "-s")
{ Description = "Suppress all audio (silent notification)" };

var loopOption = new Option<bool>("--loop")
{ Description = "Loop the audio sound (use with --duration long)" };

// ── Behavior ────────────────────────────────────────────────────────

var durationOption = new Option<string?>("--duration")
{ Description = "Toast duration: \"short\" (~5s) or \"long\" (~25s)" };

var scenarioOption = new Option<string?>("--scenario")
{ Description = "Toast scenario: default, alarm, reminder, or incomingCall" };

var expirationOption = new Option<int?>("--expiration")
{ Description = "Expiration time in minutes from now" };

var timestampOption = new Option<string?>("--timestamp")
{ Description = "Custom timestamp in ISO 8601 format" };

// ── Progress Bar ────────────────────────────────────────────────────

var progressTitleOption = new Option<string?>("--progress-title")
{ Description = "Progress bar title" };

var progressValueOption = new Option<string?>("--progress-value")
{ Description = "Progress bar value (0.0 to 1.0) or \"indeterminate\"" };

var progressValueStringOption = new Option<string?>("--progress-value-string")
{ Description = "Progress bar value string override (e.g. \"3/10 songs\")" };

var progressStatusOption = new Option<string?>("--progress-status")
{ Description = "Progress bar status text (e.g. \"Downloading...\")" };

// ── Identity ────────────────────────────────────────────────────────

var tagOption = new Option<string?>("--tag")
{ Description = "Toast tag for identifying and updating toasts" };

var groupOption = new Option<string?>("--group")
{ Description = "Toast group for organizing and updating toasts" };

// ── Header ──────────────────────────────────────────────────────────

var headerIdOption = new Option<string?>("--header-id")
{ Description = "Header ID for grouping related toasts in Action Center" };

var headerTitleOption = new Option<string?>("--header-title")
{ Description = "Header display title (defaults to header ID)" };

var headerArgumentsOption = new Option<string?>("--header-arguments")
{ Description = "Header activation arguments" };

// ── Launch ──────────────────────────────────────────────────────────

var launchUriOption = new Option<string?>("--launch")
{ Description = "URI to open when the toast body is clicked" };

var onClickOption = new Option<string?>("--on-click")
{ Description = "Shell command to execute when the toast is activated (implies --wait)" };

// ── Advanced ────────────────────────────────────────────────────────

var presetOption = new Option<string?>("--preset")
{ Description = "Apply a preset (use 'palantir preset list' to see available presets)" };

var waitOption = new Option<bool>("--wait")
{ Description = "Block until the toast is dismissed or activated, output result as JSON" };

var timeoutOption = new Option<int?>("--timeout")
{ Description = "Timeout in seconds for --wait (default: wait indefinitely)" };

var formatOption = new Option<string?>("--format")
{ Description = "Output format for --wait: json (default), text, or none" };

var replaceOption = new Option<bool>("--replace")
{ Description = "Replace an existing toast with the same --tag (re-shows the popup)" };

var dryRunOption = new Option<bool>("--dry-run")
{ Description = "Output the toast XML without displaying it" };

var jsonOption = new Option<string?>("--json")
{ Description = "Load toast options from a JSON file (use \"-\" for stdin)" };

var versionOption = new Option<bool>("--version")
{ Description = "Show version information" };

// ── Styling (per-line) ──────────────────────────────────────────────

var titleStyleOption = new Option<string?>("--title-style")
{ Description = "Title style: header, large, normal, small, dim (or raw schema value)" };

var titleAlignOption = new Option<string?>("--title-align")
{ Description = "Title alignment: left, center, right" };

var messageStyleOption = new Option<string?>("--message-style")
{ Description = "Message style: header, large, normal, small, dim (or raw schema value)" };

var messageAlignOption = new Option<string?>("--message-align")
{ Description = "Message alignment: left, center, right" };

var bodyStyleOption = new Option<string?>("--body-style")
{ Description = "Body style: header, large, normal, small, dim (or raw schema value)" };

var bodyAlignOption = new Option<string?>("--body-align")
{ Description = "Body alignment: left, center, right" };

// ── Extra Text Lines ────────────────────────────────────────────────

var extraTextOption = new Option<string[]>("--extra-text")
{ Description = "Append an extra text line (repeatable)" };

var extraTextStyleOption = new Option<string[]>("--extra-text-style")
{ Description = "Style for the most recent --extra-text" };

var extraTextAlignOption = new Option<string[]>("--extra-text-align")
{ Description = "Alignment for the most recent --extra-text" };

// ── Columns / Groups (rich layout) ──────────────────────────────────

var columnOption = new Option<string[]>("--column")
{ Description = "Add a column. Spec: \"text=Value;style=dim;align=right\". Repeatable." };

var columnRowOption = new Option<bool>("--column-row")
{ Description = "Start a new row of columns (separator between --column groups)" };

// ── Escape Hatches (full XML control) ───────────────────────────────

var textRawOption = new Option<string[]>("--text-raw")
{ Description = "Append a verbatim <text> XML element. Repeatable. Advanced." };

var xmlFragmentOption = new Option<string[]>("--xml-fragment")
{ Description = "Inject raw XML at the current --xml-anchor. Use \"@path\" to load from file. Advanced." };

var xmlAnchorOption = new Option<string[]>("--xml-anchor")
{ Description = "Anchor for following --xml-fragment(s): binding (default), actions, toast" };

var validateXmlOption = new Option<bool>("--validate-xml")
{ Description = "Validate raw XML before sending (CLI off by default; on by default in library)" };

var expandShortcodesOption = new Option<bool>("--expand-shortcodes")
{ Description = "Expand emoji shortcodes (e.g. :check: → ✅) in all text fields" };

// ── Personality (toast app identity) ────────────────────────────────

var asOption = new Option<string?>("--as")
{ Description = "Use a configured personality (corner icon + app name)" };

var displayNameOption = new Option<string?>("--display-name")
{ Description = "One-off override for the corner app name (implies ad-hoc personality)" };

var appIconOption = new Option<string?>("--app-icon")
{ Description = "One-off override for the corner app icon (file path or URL)" };

// ── Output ─────────────────────────────────────────────────────────

var quietOption = new Option<bool>("--quiet", "-q")
{ Description = "Suppress informational output (e.g. \"Toast notification sent.\")", Recursive = true };

// ── Root Command ────────────────────────────────────────────────────

var rootCommand = new RootCommand("Palantir - Windows Toast Notification CLI tool")
{
    titleOption, messageOption, bodyOption, attributionOption,
    imageOption, cropCircleOption, heroImageOption, inlineImageOption,
    buttonOption, inputOption, selectionOption,
    audioOption, silentOption, loopOption,
    durationOption, scenarioOption, expirationOption, timestampOption,
    progressTitleOption, progressValueOption, progressValueStringOption, progressStatusOption,
    tagOption, groupOption,
    headerIdOption, headerTitleOption, headerArgumentsOption,
    launchUriOption, onClickOption,
    presetOption, waitOption, timeoutOption, formatOption, replaceOption,
    dryRunOption, jsonOption, versionOption,
    titleStyleOption, titleAlignOption,
    messageStyleOption, messageAlignOption,
    bodyStyleOption, bodyAlignOption,
    extraTextOption, extraTextStyleOption, extraTextAlignOption,
    columnOption, columnRowOption,
    textRawOption, xmlFragmentOption, xmlAnchorOption,
    validateXmlOption, expandShortcodesOption,
    asOption, displayNameOption, appIconOption,
    quietOption,
};

// ── Clear subcommand ────────────────────────────────────────────────

var clearCommand = new Command("clear") { Description = "Clear all toast notification history" };
clearCommand.SetAction(parseResult =>
{
    try
    {
        ToastService.ClearHistory();
        if (!parseResult.GetValue(quietOption))
            Console.WriteLine("Toast notification history cleared.");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error clearing toast history: {ex.Message}");
        Environment.ExitCode = 1;
    }
});
rootCommand.Subcommands.Add(clearCommand);

// ── Remove subcommand ───────────────────────────────────────────────

var removeCommand = new Command("remove") { Description = "Remove specific toasts from notification history" };
var removeTagOption = new Option<string?>("--tag")
{ Description = "Tag of the toast to remove" };
var removeGroupOption = new Option<string?>("--group")
{ Description = "Group to remove all toasts from" };
removeCommand.Options.Add(removeTagOption);
removeCommand.Options.Add(removeGroupOption);

removeCommand.SetAction(parseResult =>
{
    var tag = parseResult.GetValue(removeTagOption);
    var group = parseResult.GetValue(removeGroupOption);
    var quiet = parseResult.GetValue(quietOption);

    if (string.IsNullOrWhiteSpace(tag) && string.IsNullOrWhiteSpace(group))
    {
        Console.Error.WriteLine("Error: At least --tag or --group must be provided.");
        Console.Error.WriteLine("Use --help for usage information.");
        Environment.ExitCode = 1;
        return;
    }

    try
    {
        if (!string.IsNullOrWhiteSpace(tag))
        {
            ToastService.Remove(tag, group);
            if (!quiet) Console.WriteLine($"Removed toast with tag \"{tag}\".");
        }
        else if (!string.IsNullOrWhiteSpace(group))
        {
            ToastService.RemoveGroup(group);
            if (!quiet) Console.WriteLine($"Removed all toasts in group \"{group}\".");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error removing toast: {ex.Message}");
        Environment.ExitCode = 1;
    }
});
rootCommand.Subcommands.Add(removeCommand);

// ── Update subcommand ───────────────────────────────────────────────

var updateCommand = new Command("update") { Description = "Update an existing toast's progress data" };
var updateTagOption = new Option<string?>("--tag")
{ Description = "Tag of the toast to update (required)" };
var updateGroupOption = new Option<string?>("--group")
{ Description = "Group of the toast to update" };
var updateProgressValueOption = new Option<string?>("--progress-value")
{ Description = "New progress value (0.0-1.0 or \"indeterminate\")" };
var updateProgressValueStringOption = new Option<string?>("--progress-value-string")
{ Description = "New progress value display string" };
var updateProgressStatusOption = new Option<string?>("--progress-status")
{ Description = "New progress status text" };
var updateProgressTitleOption = new Option<string?>("--progress-title")
{ Description = "New progress title" };
var updateSequenceOption = new Option<uint?>("--sequence")
{ Description = "Sequence number (must be >= previous update)" };

updateCommand.Options.Add(updateTagOption);
updateCommand.Options.Add(updateGroupOption);
updateCommand.Options.Add(updateProgressValueOption);
updateCommand.Options.Add(updateProgressValueStringOption);
updateCommand.Options.Add(updateProgressStatusOption);
updateCommand.Options.Add(updateProgressTitleOption);
updateCommand.Options.Add(updateSequenceOption);

updateCommand.SetAction(parseResult =>
{
    var tag = parseResult.GetValue(updateTagOption);
    var quiet = parseResult.GetValue(quietOption);

    if (string.IsNullOrWhiteSpace(tag))
    {
        Console.Error.WriteLine("Error: --tag is required for the update command.");
        Console.Error.WriteLine("Use --help for usage information.");
        Environment.ExitCode = 1;
        return;
    }

    try
    {
        var result = ToastService.Update(
            tag,
            parseResult.GetValue(updateGroupOption),
            parseResult.GetValue(updateProgressValueOption),
            parseResult.GetValue(updateProgressValueStringOption),
            parseResult.GetValue(updateProgressStatusOption),
            parseResult.GetValue(updateProgressTitleOption),
            parseResult.GetValue(updateSequenceOption));

        if (result == NotificationUpdateResult.Succeeded)
        {
            if (!quiet) Console.WriteLine($"Toast \"{tag}\" updated successfully.");
        }
        else
        {
            Console.Error.WriteLine($"Failed to update toast \"{tag}\": {result}");
            Environment.ExitCode = 1;
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error updating toast: {ex.Message}");
        Environment.ExitCode = 1;
    }
});
rootCommand.Subcommands.Add(updateCommand);

// ── Completions subcommand ──────────────────────────────────────────

var completionsCommand = new Command("completions") { Description = "Generate shell completion script" };
var shellArgument = new Argument<string>("shell")
{ Description = "Shell type: powershell" };
completionsCommand.Arguments.Add(shellArgument);

completionsCommand.SetAction(parseResult =>
{
    var shell = parseResult.GetValue(shellArgument)?.ToLowerInvariant();

    if (shell == "powershell" || shell == "pwsh")
    {
        Console.WriteLine("""
            # Palantir tab completion for PowerShell
            # Add this to your $PROFILE
            Register-ArgumentCompleter -CommandName palantir -Native -ScriptBlock {
                param($wordToComplete, $commandAst, $cursorPosition)

                $options = @(
                    '-t', '--title', '-m', '--message', '-b', '--body', '--attribution',
                    '-i', '--image', '--crop-circle', '--hero-image', '--inline-image',
                    '--button', '--input', '--selection',
                    '-a', '--audio', '-s', '--silent', '--loop',
                    '--duration', '--scenario', '--expiration', '--timestamp',
                    '--progress-title', '--progress-value', '--progress-value-string', '--progress-status',
                    '--tag', '--group',
                    '--header-id', '--header-title', '--header-arguments',
                    '--launch', '--on-click',
                    '--preset', '--wait', '--timeout', '--format', '--replace',
                    '--dry-run', '--json', '--version',
                    '--title-style', '--title-align',
                    '--message-style', '--message-align',
                    '--body-style', '--body-align',
                    '--extra-text', '--extra-text-style', '--extra-text-align',
                    '--column', '--column-row',
                    '--text-raw', '--xml-fragment', '--xml-anchor',
                    '--validate-xml', '--expand-shortcodes',
                    '--as', '--display-name', '--app-icon',
                    '-q', '--quiet'
                )
                $subcommands = @('clear', 'remove', 'update', 'completions', 'preset', 'history', 'test', 'personality', 'cache')
                $all = $options + $subcommands

                $all | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                    [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
                }
            }
            """);
    }
    else
    {
        Console.Error.WriteLine($"Unsupported shell: \"{shell}\". Supported: powershell");
        Environment.ExitCode = 1;
    }
});
rootCommand.Subcommands.Add(completionsCommand);

// ── Preset subcommand ───────────────────────────────────────────────

var presetCommand = new Command("preset") { Description = "Manage notification presets" };

// preset save <name> [json-or-file]
var presetSaveCommand = new Command("save") { Description = "Save a new preset from JSON" };
var presetSaveNameArg = new Argument<string>("name") { Description = "Preset name" };
var presetSaveJsonArg = new Argument<string?>("json-or-file")
{ Description = "Inline JSON string, file path, or \"-\" for stdin" };
presetSaveJsonArg.Arity = ArgumentArity.ZeroOrOne;
presetSaveCommand.Arguments.Add(presetSaveNameArg);
presetSaveCommand.Arguments.Add(presetSaveJsonArg);

presetSaveCommand.SetAction(parseResult =>
{
    var name = parseResult.GetValue(presetSaveNameArg)!;
    var jsonOrFile = parseResult.GetValue(presetSaveJsonArg);
    var quiet = parseResult.GetValue(quietOption);

    try
    {
        string jsonContent;

        if (string.IsNullOrWhiteSpace(jsonOrFile))
        {
            // Try stdin
            if (Console.IsInputRedirected)
            {
                jsonContent = Console.In.ReadToEnd();
            }
            else
            {
                Console.Error.WriteLine(
                    "Error: No preset data provided. Use one of:\n" +
                    $"  palantir preset save {name} '{{\"audio\":\"mail\",\"duration\":\"long\"}}'\n" +
                    $"  palantir preset save {name} preset.json\n" +
                    $"  echo '{{...}}' | palantir preset save {name}");
                Environment.ExitCode = 1;
                return;
            }
        }
        else if (jsonOrFile == "-")
        {
            if (!Console.IsInputRedirected)
            {
                Console.Error.WriteLine("Error: Cannot read from stdin — no input piped.");
                Environment.ExitCode = 1;
                return;
            }
            jsonContent = Console.In.ReadToEnd();
        }
        else if (jsonOrFile.TrimStart().StartsWith('{'))
        {
            // Inline JSON
            jsonContent = jsonOrFile;
        }
        else
        {
            // File path
            if (!File.Exists(jsonOrFile))
            {
                Console.Error.WriteLine($"Error: File not found: \"{jsonOrFile}\"");
                Environment.ExitCode = 1;
                return;
            }
            jsonContent = File.ReadAllText(jsonOrFile);
        }

        var preset = PresetStore.DeserializePreset(jsonContent);
        PresetStore.SavePreset(name, preset);

        if (!quiet)
        {
            var label = PresetStore.IsBuiltIn(name) ? " (overrides built-in)" : "";
            Console.WriteLine($"Preset \"{name}\" saved{label}.");
        }
    }
    catch (System.Text.Json.JsonException ex)
    {
        Console.Error.WriteLine($"Error parsing JSON: {ex.Message}");
        Environment.ExitCode = 1;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error saving preset: {ex.Message}");
        Environment.ExitCode = 1;
    }
});
presetCommand.Subcommands.Add(presetSaveCommand);

// preset list
var presetListCommand = new Command("list") { Description = "List all available presets" };
presetListCommand.SetAction(parseResult =>
{
    var builtIn = PresetStore.GetBuiltInPresets();
    var user = PresetStore.GetUserPresets();

    // Built-in presets
    Console.WriteLine("Built-in:");
    foreach (var (name, opts) in builtIn)
    {
        var overridden = user.Keys.Any(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
        var marker = overridden ? " (overridden by user)" : "";
        Console.WriteLine($"  {name,-14} {PresetStore.FormatSummary(opts)}{marker}");
    }

    // User presets
    if (user.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("User:");
        foreach (var (name, opts) in user)
            Console.WriteLine($"  {name,-14} {PresetStore.FormatSummary(opts)}");
    }

    Console.WriteLine();
    Console.WriteLine($"Config: {PresetStore.GetConfigFilePath()}");
});
presetCommand.Subcommands.Add(presetListCommand);

// preset show <name>
var presetShowCommand = new Command("show") { Description = "Show a preset's full configuration" };
var presetShowNameArg = new Argument<string>("name") { Description = "Preset name" };
presetShowCommand.Arguments.Add(presetShowNameArg);

presetShowCommand.SetAction(parseResult =>
{
    var name = parseResult.GetValue(presetShowNameArg)!;
    var preset = PresetStore.GetPreset(name);

    if (preset is null)
    {
        Console.Error.WriteLine($"Error: Preset \"{name}\" not found.");
        Console.Error.WriteLine("Use 'palantir preset list' to see available presets.");
        Environment.ExitCode = 1;
        return;
    }

    var source = PresetStore.IsBuiltIn(name) ? " (built-in)" : " (user)";
    // Check if a user preset shadows a built-in
    var user = PresetStore.GetUserPresets();
    if (user.Keys.Any(k => k.Equals(name, StringComparison.OrdinalIgnoreCase))
        && PresetStore.IsBuiltIn(name))
    {
        source = " (user, overrides built-in)";
    }

    Console.WriteLine($"# {name}{source}");
    Console.WriteLine(PresetStore.SerializePresetJson(preset));
});
presetCommand.Subcommands.Add(presetShowCommand);

// preset delete <name>
var presetDeleteCommand = new Command("delete") { Description = "Delete a user preset" };
var presetDeleteNameArg = new Argument<string>("name") { Description = "Preset name" };
presetDeleteCommand.Arguments.Add(presetDeleteNameArg);

presetDeleteCommand.SetAction(parseResult =>
{
    var name = parseResult.GetValue(presetDeleteNameArg)!;
    var quiet = parseResult.GetValue(quietOption);

    if (!PresetStore.DeletePreset(name))
    {
        if (PresetStore.IsBuiltIn(name))
        {
            Console.Error.WriteLine(
                $"Error: \"{name}\" is a built-in preset and cannot be deleted.");
        }
        else
        {
            Console.Error.WriteLine($"Error: User preset \"{name}\" not found.");
        }
        Environment.ExitCode = 1;
        return;
    }

    if (!quiet)
    {
        var restored = PresetStore.IsBuiltIn(name) ? " Built-in default restored." : "";
        Console.WriteLine($"Preset \"{name}\" deleted.{restored}");
    }
});
presetCommand.Subcommands.Add(presetDeleteCommand);

rootCommand.Subcommands.Add(presetCommand);

// ── History subcommand ──────────────────────────────────────────────

var historyCommand = new Command("history") { Description = "List active toast notifications" };
historyCommand.SetAction(parseResult =>
{
    try
    {
        var entries = ToastService.GetHistory();

        if (entries.Count == 0)
        {
            Console.WriteLine("No active notifications.");
            return;
        }

        foreach (var entry in entries)
        {
            var tag = entry.Tag ?? "-";
            var group = entry.Group ?? "-";
            var content = entry.Texts.Count > 0
                ? string.Join(" | ", entry.Texts)
                : "(no text)";
            var expires = entry.ExpirationTime.HasValue
                ? $" expires={entry.ExpirationTime.Value:HH:mm:ss}"
                : "";

            Console.WriteLine($"  tag={tag,-16} group={group,-16} {content}{expires}");
        }

        Console.WriteLine($"\nTotal: {entries.Count}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error getting history: {ex.Message}");
        Environment.ExitCode = 1;
    }
});
rootCommand.Subcommands.Add(historyCommand);

// ── Test subcommand ─────────────────────────────────────────────────

var testCommand = new Command("test") { Description = "Send a test notification to verify Palantir is working" };
testCommand.SetAction(parseResult =>
{
    try
    {
        ToastService.Show(new ToastOptions
        {
            Title = "Palantir",
            Message = $"Notifications are working! (v{version})",
            Attribution = "Test notification",
        });
        if (!parseResult.GetValue(quietOption))
            Console.WriteLine("Test notification sent successfully.");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.ExitCode = 1;
    }
});
rootCommand.Subcommands.Add(testCommand);

// ── Personality subcommand ──────────────────────────────────────────

var personalityCommand = new Command("personality")
{ Description = "Manage personalities (toast app identity: corner icon + name)" };

// personality register --name X --display-name Y --icon Z
var pRegisterCmd = new Command("register")
{ Description = "Register a personality with Windows (also writes to config)" };
var pRegNameOpt = new Option<string>("--name") { Description = "Personality name", Required = true };
var pRegDisplayOpt = new Option<string>("--display-name") { Description = "App name shown on the toast", Required = true };
var pRegIconOpt = new Option<string>("--icon") { Description = "Icon (file path or URL)", Required = true };
pRegisterCmd.Options.Add(pRegNameOpt);
pRegisterCmd.Options.Add(pRegDisplayOpt);
pRegisterCmd.Options.Add(pRegIconOpt);
pRegisterCmd.SetAction(parseResult =>
{
    var name = parseResult.GetValue(pRegNameOpt)!;
    var personality = new Personality
    {
        DisplayName = parseResult.GetValue(pRegDisplayOpt),
        Icon = parseResult.GetValue(pRegIconOpt),
    };
    var quiet = parseResult.GetValue(quietOption);

    try
    {
        PersonalityStore.SavePersonality(name, personality);
        var entry = PersonalityStore.Register(name, personality);
        if (!quiet)
            Console.WriteLine(
                $"Registered \"{name}\" → AUMID={entry.Aumid}, shortcut={entry.ShortcutPath}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.ExitCode = 1;
    }
});
personalityCommand.Subcommands.Add(pRegisterCmd);

// personality unregister --name X [--keep-history] [--keep-shortcut]
var pUnregisterCmd = new Command("unregister")
{ Description = "Remove a personality's Windows registration (does not edit config)" };
var pUnregNameOpt = new Option<string>("--name") { Description = "Personality name", Required = true };
var pUnregKeepHistOpt = new Option<bool>("--keep-history")
{ Description = "Preserve Action Center history for this personality" };
var pUnregKeepShortcutOpt = new Option<bool>("--keep-shortcut")
{ Description = "Do not delete the Start Menu shortcut" };
pUnregisterCmd.Options.Add(pUnregNameOpt);
pUnregisterCmd.Options.Add(pUnregKeepHistOpt);
pUnregisterCmd.Options.Add(pUnregKeepShortcutOpt);
pUnregisterCmd.SetAction(parseResult =>
{
    var name = parseResult.GetValue(pUnregNameOpt)!;
    var quiet = parseResult.GetValue(quietOption);
    try
    {
        var ok = PersonalityStore.Unregister(
            name,
            keepHistory: parseResult.GetValue(pUnregKeepHistOpt),
            keepShortcut: parseResult.GetValue(pUnregKeepShortcutOpt));
        if (!quiet) Console.WriteLine(ok
            ? $"Unregistered \"{name}\"."
            : $"No registration found for \"{name}\".");
        if (!ok) Environment.ExitCode = 1;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.ExitCode = 1;
    }
});
personalityCommand.Subcommands.Add(pUnregisterCmd);

// personality list
var pListCmd = new Command("list") { Description = "List personalities (config + Windows state)" };
var pListVerboseOpt = new Option<bool>("--verbose", "-v")
{ Description = "Show concrete shortcut paths and resolved icon paths" };
pListCmd.Options.Add(pListVerboseOpt);
pListCmd.SetAction(parseResult =>
{
    var verbose = parseResult.GetValue(pListVerboseOpt);
    var infos = PersonalityStore.List();
    if (infos.Count == 0)
    {
        Console.WriteLine("No personalities defined.");
        Console.WriteLine($"Config:   {PresetStore.GetConfigFilePath()}");
        Console.WriteLine($"Registry: {PathsResolver.GetRegistryFilePath()}");
        return;
    }

    foreach (var i in infos)
    {
        var states = new List<string>();
        var isBuiltInDefault = i.Name.Equals(
            PersonalityStore.BuiltInDefaultName, StringComparison.OrdinalIgnoreCase);
        if (i.InConfig) states.Add("config");
        if (isBuiltInDefault && !i.InConfig) states.Add("built-in");
        if (i.RegisteredInWindows) states.Add("windows");
        if (!i.InConfig && !isBuiltInDefault && i.RegisteredInWindows) states.Add("STALE");
        if (i.InConfig && !i.RegisteredInWindows) states.Add("not-registered");
        var stateStr = string.Join(",", states);
        Console.WriteLine(
            $"  {i.Name,-16} {i.DisplayName ?? "-",-20} aumid={i.Aumid,-30} [{stateStr}]");
        if (verbose)
        {
            if (!string.IsNullOrEmpty(i.ShortcutPath))
                Console.WriteLine($"      shortcut: {i.ShortcutPath}");
            if (!string.IsNullOrEmpty(i.Icon))
                Console.WriteLine($"      icon:     {i.Icon}");
        }
    }
    Console.WriteLine();
    Console.WriteLine($"Config:   {PresetStore.GetConfigFilePath()}");
    Console.WriteLine($"Registry: {PathsResolver.GetRegistryFilePath()}");
});
personalityCommand.Subcommands.Add(pListCmd);

// personality register-all
var pRegAllCmd = new Command("register-all")
{ Description = "Register all personalities defined in palantir.json" };
pRegAllCmd.SetAction(parseResult =>
{
    var quiet = parseResult.GetValue(quietOption);
    var entries = PersonalityStore.RegisterAll(
        onWarning: msg => Console.Error.WriteLine(msg));
    if (!quiet) Console.WriteLine($"Registered {entries.Count} personality(ies).");
});
personalityCommand.Subcommands.Add(pRegAllCmd);

// personality unregister-all [--yes] [--keep-history]
var pUnregAllCmd = new Command("unregister-all")
{ Description = "Unregister all Palantir-managed personalities from Windows" };
var pUnregAllYesOpt = new Option<bool>("--yes")
{ Description = "Skip confirmation prompt" };
var pUnregAllKeepHistOpt = new Option<bool>("--keep-history")
{ Description = "Preserve Action Center history" };
pUnregAllCmd.Options.Add(pUnregAllYesOpt);
pUnregAllCmd.Options.Add(pUnregAllKeepHistOpt);
pUnregAllCmd.SetAction(parseResult =>
{
    var quiet = parseResult.GetValue(quietOption);
    if (!parseResult.GetValue(pUnregAllYesOpt))
    {
        Console.Write("Unregister all Palantir personalities? [y/N]: ");
        var ans = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(ans)
            || !ans.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Cancelled.");
            return;
        }
    }
    var n = PersonalityStore.UnregisterAll(
        keepHistory: parseResult.GetValue(pUnregAllKeepHistOpt),
        onWarning: msg => Console.Error.WriteLine(msg));
    if (!quiet) Console.WriteLine($"Unregistered {n} personality(ies).");
});
personalityCommand.Subcommands.Add(pUnregAllCmd);

// personality sync [--yes] [--keep-history] [--dry-run]
var pSyncCmd = new Command("sync")
{ Description = "Reconcile config ↔ Windows: register missing, unregister stale" };
var pSyncYesOpt = new Option<bool>("--yes")
{ Description = "Skip confirmation prompt for stale removals" };
var pSyncKeepHistOpt = new Option<bool>("--keep-history")
{ Description = "Preserve Action Center history when removing stale entries" };
var pSyncDryRunOpt = new Option<bool>("--dry-run")
{ Description = "Show what would change without modifying anything" };
pSyncCmd.Options.Add(pSyncYesOpt);
pSyncCmd.Options.Add(pSyncKeepHistOpt);
pSyncCmd.Options.Add(pSyncDryRunOpt);
pSyncCmd.SetAction(parseResult =>
{
    var quiet = parseResult.GetValue(quietOption);

    if (parseResult.GetValue(pSyncDryRunOpt))
    {
        var infos = PersonalityStore.List();
        var toReg = infos.Where(i => i.InConfig && !i.RegisteredInWindows).ToList();
        var toUnreg = infos.Where(i => !i.InConfig && i.RegisteredInWindows).ToList();
        Console.WriteLine($"Would register {toReg.Count}: {string.Join(", ", toReg.Select(i => i.Name))}");
        Console.WriteLine($"Would unregister {toUnreg.Count}: {string.Join(", ", toUnreg.Select(i => i.Name))}");
        return;
    }

    var stale = PersonalityStore.List().Where(i => !i.InConfig && i.RegisteredInWindows).ToList();
    if (stale.Count > 0 && !parseResult.GetValue(pSyncYesOpt))
    {
        Console.Write($"Sync will unregister {stale.Count} stale entry(ies). Continue? [y/N]: ");
        var ans = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(ans)
            || !ans.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Cancelled.");
            return;
        }
    }

    var (reg, unreg) = PersonalityStore.Sync(
        keepHistory: parseResult.GetValue(pSyncKeepHistOpt),
        onWarning: msg => Console.Error.WriteLine(msg));
    if (!quiet) Console.WriteLine($"Synced: registered {reg}, unregistered {unreg}.");
});
personalityCommand.Subcommands.Add(pSyncCmd);

// personality prune [--yes] [--keep-history]
var pPruneCmd = new Command("prune")
{ Description = "Remove Windows entries no longer in config" };
var pPruneYesOpt = new Option<bool>("--yes") { Description = "Skip confirmation" };
var pPruneKeepHistOpt = new Option<bool>("--keep-history") { Description = "Preserve Action Center history" };
pPruneCmd.Options.Add(pPruneYesOpt);
pPruneCmd.Options.Add(pPruneKeepHistOpt);
pPruneCmd.SetAction(parseResult =>
{
    var quiet = parseResult.GetValue(quietOption);
    if (!parseResult.GetValue(pPruneYesOpt))
    {
        Console.Write("Prune all stale Palantir personalities? [y/N]: ");
        var ans = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(ans)
            || !ans.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Cancelled.");
            return;
        }
    }
    var n = PersonalityStore.Prune(
        keepHistory: parseResult.GetValue(pPruneKeepHistOpt),
        onWarning: msg => Console.Error.WriteLine(msg));
    if (!quiet) Console.WriteLine($"Pruned {n} personality(ies).");
});
personalityCommand.Subcommands.Add(pPruneCmd);

// personality use --name X
var pUseCmd = new Command("use")
{ Description = "Set the default personality (used when --as is not specified)" };
var pUseNameOpt = new Option<string?>("--name")
{ Description = "Personality name, or omit to clear default" };
pUseCmd.Options.Add(pUseNameOpt);
pUseCmd.SetAction(parseResult =>
{
    var quiet = parseResult.GetValue(quietOption);
    var config = PresetStore.LoadConfig();
    var name = parseResult.GetValue(pUseNameOpt);
    config.DefaultPersonality = string.IsNullOrWhiteSpace(name) ? null : name;
    PresetStore.SaveConfig(config);
    if (!quiet) Console.WriteLine(name is null
        ? "Default personality cleared."
        : $"Default personality set to \"{name}\".");
});
personalityCommand.Subcommands.Add(pUseCmd);

// personality delete --name X
var pDeleteCmd = new Command("delete")
{ Description = "Delete a personality from config (does not unregister from Windows; run sync to clean up)" };
var pDelNameOpt = new Option<string>("--name") { Description = "Personality name", Required = true };
pDeleteCmd.Options.Add(pDelNameOpt);
pDeleteCmd.SetAction(parseResult =>
{
    var quiet = parseResult.GetValue(quietOption);
    var name = parseResult.GetValue(pDelNameOpt)!;
    if (!PersonalityStore.DeletePersonality(name))
    {
        Console.Error.WriteLine($"Error: personality \"{name}\" not found in config.");
        Environment.ExitCode = 1;
        return;
    }
    if (!quiet) Console.WriteLine(
        $"Deleted \"{name}\" from config. Run 'palantir personality sync' or " +
        $"'palantir personality unregister --name {name}' to remove from Windows.");
});
personalityCommand.Subcommands.Add(pDeleteCmd);

rootCommand.Subcommands.Add(personalityCommand);

// ── Cache subcommand ────────────────────────────────────────────────

var cacheCommand = new Command("cache") { Description = "Manage Palantir caches (icons, images)" };

var cachePathCmd = new Command("path") { Description = "Print resolved cache directories" };
cachePathCmd.SetAction(_ =>
{
    Console.WriteLine($"cache:    {PathsResolver.GetCacheDirectory()}");
    Console.WriteLine($"icons:    {PathsResolver.GetIconsDirectory()}");
    Console.WriteLine($"images:   {PathsResolver.GetImagesDirectory()}");
    Console.WriteLine($"registry: {PathsResolver.GetRegistryFilePath()}");
});
cacheCommand.Subcommands.Add(cachePathCmd);

var cacheClearCmd = new Command("clear") { Description = "Clear cache (default: everything)" };
var cacheClearIconsOpt = new Option<bool>("--icons") { Description = "Clear only the icons cache" };
var cacheClearImagesOpt = new Option<bool>("--images") { Description = "Clear only the images cache" };
var cacheClearYesOpt = new Option<bool>("--yes") { Description = "Skip confirmation prompt" };
cacheClearCmd.Options.Add(cacheClearIconsOpt);
cacheClearCmd.Options.Add(cacheClearImagesOpt);
cacheClearCmd.Options.Add(cacheClearYesOpt);
cacheClearCmd.SetAction(parseResult =>
{
    var quiet = parseResult.GetValue(quietOption);
    var onlyIcons = parseResult.GetValue(cacheClearIconsOpt);
    var onlyImages = parseResult.GetValue(cacheClearImagesOpt);
    var both = !onlyIcons && !onlyImages;

    var label = (onlyIcons, onlyImages, both) switch
    {
        (true, false, _) => "icons cache",
        (false, true, _) => "images cache",
        _ => "all caches",
    };

    if (!parseResult.GetValue(cacheClearYesOpt))
    {
        Console.Write($"Clear {label}? [y/N]: ");
        var ans = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(ans)
            || !ans.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Cancelled.");
            return;
        }
    }

    var total = 0;
    if (onlyIcons || both) total += IconCache.Clear();
    if (onlyImages || both)
    {
        var dir = PathsResolver.GetImagesDirectory();
        if (Directory.Exists(dir))
        {
            total += Directory.GetFiles(dir).Length;
            Directory.Delete(dir, recursive: true);
        }
    }
    if (!quiet) Console.WriteLine($"Cleared {label} ({total} file(s)).");
});
cacheCommand.Subcommands.Add(cacheClearCmd);

rootCommand.Subcommands.Add(cacheCommand);

// ── Root handler ────────────────────────────────────────────────────

rootCommand.SetAction(parseResult =>
{
    // ── Version check ───────────────────────────────────────────
    if (parseResult.GetValue(versionOption))
    {
        Console.WriteLine($"palantir {version}");
        return;
    }

    var quiet = parseResult.GetValue(quietOption);
    Action<string>? onWarning = quiet ? null : msg => Console.Error.WriteLine(msg);

    // ── JSON input ──────────────────────────────────────────────
    var jsonFile = parseResult.GetValue(jsonOption);
    ToastOptions options;

    if (!string.IsNullOrWhiteSpace(jsonFile))
    {
        try
        {
            string jsonContent;
            if (jsonFile == "-")
            {
                if (!Console.IsInputRedirected)
                {
                    Console.Error.WriteLine(
                        "Error: Cannot read JSON from stdin — no input piped. " +
                        "Use: echo '{\"title\":\"Hello\"}' | palantir --json -");
                    Environment.ExitCode = 1;
                    return;
                }
                jsonContent = Console.In.ReadToEnd();
            }
            else
            {
                jsonContent = File.ReadAllText(jsonFile);
            }

            var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options = JsonSerializer.Deserialize<ToastOptions>(jsonContent, jsonOpts)
                      ?? new ToastOptions();
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Error parsing JSON: {ex.Message}");
            Environment.ExitCode = 1;
            return;
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine($"Error: JSON file not found: \"{jsonFile}\"");
            Environment.ExitCode = 1;
            return;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Error reading JSON file: {ex.Message}");
            Environment.ExitCode = 1;
            return;
        }

        // Override JSON values with any explicitly provided CLI options
        OverrideFromCli(options, parseResult);
    }
    else
    {
        options = BuildOptionsFromCli(parseResult);
    }

    // ── Stdin pipe support ──────────────────────────────────────
    string? stdinContent = null;
    string? ResolveStdin(string? value)
    {
        if (value != "-") return value;
        if (!Console.IsInputRedirected)
        {
            Console.Error.WriteLine(
                "Error: Cannot read from stdin — no input piped. " +
                "Use: echo \"text\" | palantir -m -");
            Environment.ExitCode = 1;
            return null;
        }
        stdinContent ??= Console.In.ReadToEnd().TrimEnd('\r', '\n');
        return stdinContent;
    }

    options.Title = ResolveStdin(options.Title);
    options.Message = ResolveStdin(options.Message);
    options.Body = ResolveStdin(options.Body);

    if (Environment.ExitCode != 0) return;

    // ── Apply preset ────────────────────────────────────────────
    if (!string.IsNullOrWhiteSpace(options.Preset))
    {
        var preset = PresetStore.GetPreset(options.Preset);
        if (preset is null)
        {
            onWarning?.Invoke(
                $"Warning: Unknown preset \"{options.Preset}\". " +
                "Use 'palantir preset list' to see available presets.");
        }
        else
        {
            var explicitOptions = GetExplicitOptions(parseResult);
            PresetStore.MergePreset(options, preset, explicitOptions);
        }
    }

    // ── --on-click implies --wait ───────────────────────────────
    if (!string.IsNullOrWhiteSpace(options.OnClickCommand))
        options.Wait = true;

    // ── --timeout implies --wait ────────────────────────────────
    var timeout = parseResult.GetValue(timeoutOption) ?? options.Timeout;
    if (timeout.HasValue)
        options.Wait = true;

    // ── --replace requires --tag ────────────────────────────────
    if (parseResult.GetValue(replaceOption))
    {
        if (string.IsNullOrWhiteSpace(options.Tag))
        {
            Console.Error.WriteLine("Error: --replace requires --tag to identify the toast to replace.");
            Environment.ExitCode = 1;
            return;
        }
    }

    // ── Validate required options ───────────────────────────────
    if (string.IsNullOrWhiteSpace(options.Title) && string.IsNullOrWhiteSpace(options.Message)
        && !options.DryRun)
    {
        Console.Error.WriteLine("Error: At least --title or --message must be provided.");
        Console.Error.WriteLine("Use --help for usage information.");
        Environment.ExitCode = 1;
        return;
    }

    // ── Validate expiration ─────────────────────────────────────
    if (options.Expiration.HasValue && options.Expiration.Value <= 0)
    {
        onWarning?.Invoke(
            $"Warning: Expiration must be a positive number of minutes. " +
            $"Got {options.Expiration.Value}. Ignoring.");
        options.Expiration = null;
    }

    // ── Execute ─────────────────────────────────────────────────
    try
    {
        if (options.DryRun)
        {
            var xml = ToastService.GetXml(options, onWarning);
            Console.WriteLine(xml);
            return;
        }

        if (options.Wait)
        {
            var result = ToastService.ShowAndWait(options, timeout, onWarning);

            // Output result in requested format
            var format = parseResult.GetValue(formatOption)?.ToLowerInvariant() ?? "json";
            switch (format)
            {
                case "json":
                    Console.WriteLine(result.ToJson());
                    break;
                case "text":
                    Console.WriteLine(result.ToText());
                    break;
                case "none":
                    break;
                default:
                    onWarning?.Invoke(
                        $"Warning: Unknown format \"{format}\". Valid: json, text, none. Using json.");
                    Console.WriteLine(result.ToJson());
                    break;
            }

            // Execute on-click command if toast was activated
            if (result.Action == "activated" &&
                !string.IsNullOrWhiteSpace(options.OnClickCommand))
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe",
                        Arguments = $"/c {options.OnClickCommand}",
                        UseShellExecute = false,
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error executing on-click command: {ex.Message}");
                    Environment.ExitCode = 1;
                }
            }

            // Set exit code based on result
            Environment.ExitCode = result.Action switch
            {
                "activated" => 0,
                "dismissed" => 1,
                "failed" => 2,
                "cancelled" => 3,
                "timedOut" => 4,
                _ => 5,
            };
            return;
        }

        ToastService.Show(options, onWarning);
        if (!quiet)
            Console.WriteLine("Toast notification sent.");
    }
    catch (InvalidOperationException ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.ExitCode = 1;
    }
    catch (System.Runtime.InteropServices.COMException ex)
    {
        Console.Error.WriteLine($"Error sending toast notification (COM): {ex.Message}");
        Environment.ExitCode = 1;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error sending toast notification: {ex.Message}");
        Environment.ExitCode = 1;
    }
});

// ── Run ─────────────────────────────────────────────────────────────

var config = new CommandLineConfiguration(rootCommand);
return config.Invoke(args);

// ── Helper methods ──────────────────────────────────────────────────

ToastOptions BuildOptionsFromCli(ParseResult parseResult)
{
    var options = new ToastOptions
    {
        Title = parseResult.GetValue(titleOption),
        Message = parseResult.GetValue(messageOption),
        Body = parseResult.GetValue(bodyOption),
        Attribution = parseResult.GetValue(attributionOption),
        Image = parseResult.GetValue(imageOption),
        CropCircle = parseResult.GetValue(cropCircleOption),
        HeroImage = parseResult.GetValue(heroImageOption),
        InlineImage = parseResult.GetValue(inlineImageOption),
        Buttons = parseResult.GetValue(buttonOption) ?? [],
        Inputs = parseResult.GetValue(inputOption) ?? [],
        Selections = parseResult.GetValue(selectionOption) ?? [],
        Audio = parseResult.GetValue(audioOption),
        Silent = parseResult.GetValue(silentOption),
        Loop = parseResult.GetValue(loopOption),
        Duration = parseResult.GetValue(durationOption),
        Scenario = parseResult.GetValue(scenarioOption),
        Expiration = parseResult.GetValue(expirationOption),
        Timestamp = parseResult.GetValue(timestampOption),
        ProgressTitle = parseResult.GetValue(progressTitleOption),
        ProgressValue = parseResult.GetValue(progressValueOption),
        ProgressValueString = parseResult.GetValue(progressValueStringOption),
        ProgressStatus = parseResult.GetValue(progressStatusOption),
        Tag = parseResult.GetValue(tagOption),
        Group = parseResult.GetValue(groupOption),
        HeaderId = parseResult.GetValue(headerIdOption),
        HeaderTitle = parseResult.GetValue(headerTitleOption),
        HeaderArguments = parseResult.GetValue(headerArgumentsOption),
        LaunchUri = parseResult.GetValue(launchUriOption),
        OnClickCommand = parseResult.GetValue(onClickOption),
        Preset = parseResult.GetValue(presetOption),
        Wait = parseResult.GetValue(waitOption),
        Timeout = parseResult.GetValue(timeoutOption),
        DryRun = parseResult.GetValue(dryRunOption),
        TitleStyle = parseResult.GetValue(titleStyleOption),
        TitleAlign = parseResult.GetValue(titleAlignOption),
        MessageStyle = parseResult.GetValue(messageStyleOption),
        MessageAlign = parseResult.GetValue(messageAlignOption),
        BodyStyle = parseResult.GetValue(bodyStyleOption),
        BodyAlign = parseResult.GetValue(bodyAlignOption),
        // CLI default: validation OFF for performance; --validate-xml flips it on.
        ValidateXml = parseResult.GetValue(validateXmlOption),
        ExpandShortcodes = parseResult.GetValue(expandShortcodesOption),
        Personality = parseResult.GetValue(asOption),
        DisplayName = parseResult.GetValue(displayNameOption),
        AppIcon = parseResult.GetValue(appIconOption),
    };

    ApplyOrderedRichOptions(options, parseResult);
    return options;
}

void OverrideFromCli(ToastOptions options, ParseResult parseResult)
{
    // Override JSON values with explicitly provided CLI options.
    // For nullable types: CLI value wins if non-null.
    // For bool types: CLI value wins if true (flags are additive).
    // For arrays: CLI value wins if non-empty.
    var cliTitle = parseResult.GetValue(titleOption);
    if (cliTitle is not null) options.Title = cliTitle;

    var cliMessage = parseResult.GetValue(messageOption);
    if (cliMessage is not null) options.Message = cliMessage;

    var cliBody = parseResult.GetValue(bodyOption);
    if (cliBody is not null) options.Body = cliBody;

    var cliAttribution = parseResult.GetValue(attributionOption);
    if (cliAttribution is not null) options.Attribution = cliAttribution;

    var cliImage = parseResult.GetValue(imageOption);
    if (cliImage is not null) options.Image = cliImage;

    if (parseResult.GetValue(cropCircleOption)) options.CropCircle = true;

    var cliHeroImage = parseResult.GetValue(heroImageOption);
    if (cliHeroImage is not null) options.HeroImage = cliHeroImage;

    var cliInlineImage = parseResult.GetValue(inlineImageOption);
    if (cliInlineImage is not null) options.InlineImage = cliInlineImage;

    var cliButtons = parseResult.GetValue(buttonOption);
    if (cliButtons is { Length: > 0 }) options.Buttons = cliButtons;

    var cliInputs = parseResult.GetValue(inputOption);
    if (cliInputs is { Length: > 0 }) options.Inputs = cliInputs;

    var cliSelections = parseResult.GetValue(selectionOption);
    if (cliSelections is { Length: > 0 }) options.Selections = cliSelections;

    var cliAudio = parseResult.GetValue(audioOption);
    if (cliAudio is not null) options.Audio = cliAudio;

    if (parseResult.GetValue(silentOption)) options.Silent = true;
    if (parseResult.GetValue(loopOption)) options.Loop = true;

    var cliDuration = parseResult.GetValue(durationOption);
    if (cliDuration is not null) options.Duration = cliDuration;

    var cliScenario = parseResult.GetValue(scenarioOption);
    if (cliScenario is not null) options.Scenario = cliScenario;

    var cliExpiration = parseResult.GetValue(expirationOption);
    if (cliExpiration.HasValue) options.Expiration = cliExpiration;

    var cliTimestamp = parseResult.GetValue(timestampOption);
    if (cliTimestamp is not null) options.Timestamp = cliTimestamp;

    var cliProgressTitle = parseResult.GetValue(progressTitleOption);
    if (cliProgressTitle is not null) options.ProgressTitle = cliProgressTitle;

    var cliProgressValue = parseResult.GetValue(progressValueOption);
    if (cliProgressValue is not null) options.ProgressValue = cliProgressValue;

    var cliProgressValueString = parseResult.GetValue(progressValueStringOption);
    if (cliProgressValueString is not null) options.ProgressValueString = cliProgressValueString;

    var cliProgressStatus = parseResult.GetValue(progressStatusOption);
    if (cliProgressStatus is not null) options.ProgressStatus = cliProgressStatus;

    var cliTag = parseResult.GetValue(tagOption);
    if (cliTag is not null) options.Tag = cliTag;

    var cliGroup = parseResult.GetValue(groupOption);
    if (cliGroup is not null) options.Group = cliGroup;

    var cliHeaderId = parseResult.GetValue(headerIdOption);
    if (cliHeaderId is not null) options.HeaderId = cliHeaderId;

    var cliHeaderTitle = parseResult.GetValue(headerTitleOption);
    if (cliHeaderTitle is not null) options.HeaderTitle = cliHeaderTitle;

    var cliHeaderArguments = parseResult.GetValue(headerArgumentsOption);
    if (cliHeaderArguments is not null) options.HeaderArguments = cliHeaderArguments;

    var cliLaunchUri = parseResult.GetValue(launchUriOption);
    if (cliLaunchUri is not null) options.LaunchUri = cliLaunchUri;

    var cliOnClick = parseResult.GetValue(onClickOption);
    if (cliOnClick is not null) options.OnClickCommand = cliOnClick;

    var cliPreset = parseResult.GetValue(presetOption);
    if (cliPreset is not null) options.Preset = cliPreset;

    if (parseResult.GetValue(waitOption)) options.Wait = true;
    var cliTimeout = parseResult.GetValue(timeoutOption);
    if (cliTimeout.HasValue) options.Timeout = cliTimeout;
    if (parseResult.GetValue(dryRunOption)) options.DryRun = true;

    // ── Styling overrides ──────────────────────────────────────
    var cliTitleStyle = parseResult.GetValue(titleStyleOption);
    if (cliTitleStyle is not null) options.TitleStyle = cliTitleStyle;
    var cliTitleAlign = parseResult.GetValue(titleAlignOption);
    if (cliTitleAlign is not null) options.TitleAlign = cliTitleAlign;
    var cliMsgStyle = parseResult.GetValue(messageStyleOption);
    if (cliMsgStyle is not null) options.MessageStyle = cliMsgStyle;
    var cliMsgAlign = parseResult.GetValue(messageAlignOption);
    if (cliMsgAlign is not null) options.MessageAlign = cliMsgAlign;
    var cliBodyStyle = parseResult.GetValue(bodyStyleOption);
    if (cliBodyStyle is not null) options.BodyStyle = cliBodyStyle;
    var cliBodyAlign = parseResult.GetValue(bodyAlignOption);
    if (cliBodyAlign is not null) options.BodyAlign = cliBodyAlign;

    // --validate-xml: CLI flag forces it on (overrides JSON-loaded false).
    if (parseResult.GetValue(validateXmlOption)) options.ValidateXml = true;
    if (parseResult.GetValue(expandShortcodesOption)) options.ExpandShortcodes = true;

    var cliAs = parseResult.GetValue(asOption);
    if (cliAs is not null) options.Personality = cliAs;
    var cliDisplayName = parseResult.GetValue(displayNameOption);
    if (cliDisplayName is not null) options.DisplayName = cliDisplayName;
    var cliAppIcon = parseResult.GetValue(appIconOption);
    if (cliAppIcon is not null) options.AppIcon = cliAppIcon;

    // For list-style rich options: if user provided any on the CLI, REPLACE
    // the JSON-loaded equivalents (mirrors how Buttons/Inputs/Selections behave).
    if (parseResult.GetValue(extraTextOption) is { Length: > 0 })
        options.ExtraTexts.Clear();
    if (parseResult.GetValue(columnOption) is { Length: > 0 })
        options.Groups.Clear();
    if (parseResult.GetValue(textRawOption) is { Length: > 0 })
        options.RawTextElements.Clear();
    if (parseResult.GetValue(xmlFragmentOption) is { Length: > 0 })
        options.XmlFragments.Clear();

    // Append ordered rich options (extra-text, columns, raw, fragments).
    ApplyOrderedRichOptions(options, parseResult);
}

void ApplyOrderedRichOptions(ToastOptions options, ParseResult parseResult)
{
    // Walk parsed tokens in order to honor positional semantics:
    //   --extra-text X --extra-text-style dim     → style attaches to X
    //   --column a --column b --column-row --column c   → row 1 = [a,b], row 2 = [c]
    //   --xml-anchor actions --xml-fragment ...    → fragment anchored to actions
    var tokens = parseResult.Tokens;
    TextLine? currentExtra = null;
    List<TextLine>? currentRow = null;
    var anchor = XmlAnchor.Binding;

    string? Next(int i) => i + 1 < tokens.Count ? tokens[i + 1].Value : null;

    for (var i = 0; i < tokens.Count; i++)
    {
        switch (tokens[i].Value)
        {
            case "--extra-text":
                currentExtra = new TextLine { Text = Next(i) ?? "" };
                options.ExtraTexts.Add(currentExtra);
                break;
            case "--extra-text-style":
                if (currentExtra is not null) currentExtra.Style = Next(i);
                break;
            case "--extra-text-align":
                if (currentExtra is not null) currentExtra.Align = Next(i);
                break;
            case "--column":
                currentRow ??= new List<TextLine>();
                currentRow.Add(ParseColumnSpec(Next(i) ?? ""));
                break;
            case "--column-row":
                if (currentRow is { Count: > 0 })
                {
                    options.Groups.Add(currentRow);
                    currentRow = null;
                }
                break;
            case "--text-raw":
                options.RawTextElements.Add(Next(i) ?? "");
                break;
            case "--xml-anchor":
                var anchorVal = (Next(i) ?? "binding").Trim().ToLowerInvariant();
                anchor = anchorVal switch
                {
                    "actions" => XmlAnchor.Actions,
                    "toast"   => XmlAnchor.Toast,
                    _         => XmlAnchor.Binding,
                };
                break;
            case "--xml-fragment":
                var arg = Next(i) ?? "";
                if (arg.StartsWith('@'))
                {
                    var path = arg[1..];
                    if (!File.Exists(path))
                        throw new FileNotFoundException(
                            $"--xml-fragment file not found: \"{path}\"");
                    arg = File.ReadAllText(path);
                }
                options.XmlFragments.Add(new XmlFragment { Fragment = arg, Anchor = anchor });
                break;
        }
    }

    if (currentRow is { Count: > 0 })
        options.Groups.Add(currentRow);
}

TextLine ParseColumnSpec(string spec)
{
    var line = new TextLine();
    foreach (var pair in spec.Split(';'))
    {
        var eq = pair.IndexOf('=');
        if (eq <= 0) continue;
        var key = pair[..eq].Trim();
        var val = pair[(eq + 1)..].Trim();
        switch (key.ToLowerInvariant())
        {
            case "text":  line.Text = val; break;
            case "style": line.Style = val; break;
            case "align": line.Align = val; break;
        }
    }
    return line;
}

HashSet<string> GetExplicitOptions(ParseResult parseResult)
{
    var explicit_ = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // String? options — explicitly provided if non-null
    if (parseResult.GetValue(titleOption) is not null) explicit_.Add("title");
    if (parseResult.GetValue(messageOption) is not null) explicit_.Add("message");
    if (parseResult.GetValue(bodyOption) is not null) explicit_.Add("body");
    if (parseResult.GetValue(attributionOption) is not null) explicit_.Add("attribution");
    if (parseResult.GetValue(imageOption) is not null) explicit_.Add("image");
    if (parseResult.GetValue(heroImageOption) is not null) explicit_.Add("heroImage");
    if (parseResult.GetValue(inlineImageOption) is not null) explicit_.Add("inlineImage");
    if (parseResult.GetValue(audioOption) is not null) explicit_.Add("audio");
    if (parseResult.GetValue(durationOption) is not null) explicit_.Add("duration");
    if (parseResult.GetValue(scenarioOption) is not null) explicit_.Add("scenario");
    if (parseResult.GetValue(timestampOption) is not null) explicit_.Add("timestamp");
    if (parseResult.GetValue(progressTitleOption) is not null) explicit_.Add("progressTitle");
    if (parseResult.GetValue(progressValueOption) is not null) explicit_.Add("progressValue");
    if (parseResult.GetValue(progressValueStringOption) is not null) explicit_.Add("progressValueString");
    if (parseResult.GetValue(progressStatusOption) is not null) explicit_.Add("progressStatus");
    if (parseResult.GetValue(tagOption) is not null) explicit_.Add("tag");
    if (parseResult.GetValue(groupOption) is not null) explicit_.Add("group");
    if (parseResult.GetValue(headerIdOption) is not null) explicit_.Add("headerId");
    if (parseResult.GetValue(headerTitleOption) is not null) explicit_.Add("headerTitle");
    if (parseResult.GetValue(headerArgumentsOption) is not null) explicit_.Add("headerArguments");
    if (parseResult.GetValue(launchUriOption) is not null) explicit_.Add("launch");
    if (parseResult.GetValue(onClickOption) is not null) explicit_.Add("onClick");

    // Bool options — explicitly provided if true (flags)
    if (parseResult.GetValue(cropCircleOption)) explicit_.Add("cropCircle");
    if (parseResult.GetValue(silentOption)) explicit_.Add("silent");
    if (parseResult.GetValue(loopOption)) explicit_.Add("loop");
    if (parseResult.GetValue(waitOption)) explicit_.Add("wait");

    // Int? options
    if (parseResult.GetValue(expirationOption).HasValue) explicit_.Add("expiration");
    if (parseResult.GetValue(timeoutOption).HasValue) explicit_.Add("timeout");

    // Array options — explicitly provided if non-empty
    if (parseResult.GetValue(buttonOption) is { Length: > 0 }) explicit_.Add("buttons");
    if (parseResult.GetValue(inputOption) is { Length: > 0 }) explicit_.Add("inputs");
    if (parseResult.GetValue(selectionOption) is { Length: > 0 }) explicit_.Add("selections");

    // New rich/styling options
    if (parseResult.GetValue(titleStyleOption) is not null) explicit_.Add("titleStyle");
    if (parseResult.GetValue(titleAlignOption) is not null) explicit_.Add("titleAlign");
    if (parseResult.GetValue(messageStyleOption) is not null) explicit_.Add("messageStyle");
    if (parseResult.GetValue(messageAlignOption) is not null) explicit_.Add("messageAlign");
    if (parseResult.GetValue(bodyStyleOption) is not null) explicit_.Add("bodyStyle");
    if (parseResult.GetValue(bodyAlignOption) is not null) explicit_.Add("bodyAlign");
    if (parseResult.GetValue(validateXmlOption)) explicit_.Add("validateXml");
    if (parseResult.GetValue(expandShortcodesOption)) explicit_.Add("expandShortcodes");
    if (parseResult.GetValue(extraTextOption) is { Length: > 0 }) explicit_.Add("extraTexts");
    if (parseResult.GetValue(columnOption) is { Length: > 0 }) explicit_.Add("groups");
    if (parseResult.GetValue(textRawOption) is { Length: > 0 }) explicit_.Add("rawTextElements");
    if (parseResult.GetValue(xmlFragmentOption) is { Length: > 0 }) explicit_.Add("xmlFragments");

    if (parseResult.GetValue(asOption) is not null) explicit_.Add("personality");
    if (parseResult.GetValue(displayNameOption) is not null) explicit_.Add("displayName");
    if (parseResult.GetValue(appIconOption) is not null) explicit_.Add("appIcon");

    return explicit_;
}
