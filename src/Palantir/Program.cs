using System.CommandLine;
using Palantir;

// ── Text Content ────────────────────────────────────────────────────

var titleOption = new Option<string?>("--title", "-t")
{ Description = "Toast title text (first line, bold)" };

var messageOption = new Option<string?>("--message", "-m")
{ Description = "Toast body message text (second line)" };

var bodyOption = new Option<string?>("--body", "-b")
{ Description = "Additional body text (third line)" };

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

var audioOption = new Option<string?>("--audio")
{ Description = "Audio sound name (default, im, mail, reminder, sms, alarm, call, etc.) or file path" };

var silentOption = new Option<bool>("--silent")
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

var appIdOption = new Option<string?>("--app-id")
{ Description = "Application User Model ID (AUMID) for the toast source" };

var tagOption = new Option<string?>("--tag")
{ Description = "Toast tag for identifying and updating toasts" };

var groupOption = new Option<string?>("--group")
{ Description = "Toast group for organizing and updating toasts" };

// ── Launch ──────────────────────────────────────────────────────────

var launchUriOption = new Option<string?>("--launch")
{ Description = "URI to open when the toast body is clicked" };

// ── Root Command ────────────────────────────────────────────────────

var rootCommand = new RootCommand("Palantir - Windows Toast Notification CLI tool")
{
    titleOption, messageOption, bodyOption, attributionOption,
    imageOption, cropCircleOption, heroImageOption, inlineImageOption,
    buttonOption, inputOption, selectionOption,
    audioOption, silentOption, loopOption,
    durationOption, scenarioOption, expirationOption, timestampOption,
    progressTitleOption, progressValueOption, progressValueStringOption, progressStatusOption,
    appIdOption, tagOption, groupOption,
    launchUriOption,
};

// ── Clear subcommand ────────────────────────────────────────────────

var clearCommand = new Command("clear") { Description = "Clear all toast notification history" };
clearCommand.SetAction(_ =>
{
    ToastService.ClearHistory();
    Console.WriteLine("Toast notification history cleared.");
});
rootCommand.Subcommands.Add(clearCommand);

// ── Root handler ────────────────────────────────────────────────────

rootCommand.SetAction(parseResult =>
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
        AppId = parseResult.GetValue(appIdOption),
        Tag = parseResult.GetValue(tagOption),
        Group = parseResult.GetValue(groupOption),
        LaunchUri = parseResult.GetValue(launchUriOption),
    };

    if (string.IsNullOrWhiteSpace(options.Title) && string.IsNullOrWhiteSpace(options.Message))
    {
        Console.Error.WriteLine("Error: At least --title or --message must be provided.");
        Console.Error.WriteLine("Use --help for usage information.");
        Environment.ExitCode = 1;
        return;
    }

    try
    {
        ToastService.Show(options);
        Console.WriteLine("Toast notification sent.");
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
