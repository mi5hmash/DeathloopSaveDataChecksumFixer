using deathloop_savedata_checksum_fixer_cli;
using deathloop_savedata_checksum_fixer_cli.Helpers;
using Mi5hmasH.AppInfo;
using Mi5hmasH.ConsoleHelper;
using Mi5hmasH.Logger;
using Mi5hmasH.Logger.Providers;

#region SETUP

// CONSTANTS
const string breakLine = "---";

// Initialize APP_INFO
var appInfo = new MyAppInfo("deathloop_savedata_checksum_fixer_cli");

// Initialize LOGGER
var logger = new SimpleLogger
{
    LoggedAppName = appInfo.Name
};
// Configure ConsoleLogProvider
var consoleLogProvider = new ConsoleLogProvider();
logger.AddProvider(consoleLogProvider);
// Flush log providers on process exit
AppDomain.CurrentDomain.ProcessExit += (_, _) => logger.Flush();

// Initialize ProgressReporter
var progressReporter = new ProgressReporter(new Progress<string>(Console.WriteLine), null);

// Initialize CORE
var core = new Core(logger, progressReporter);

// Print HEADER
ConsoleHelper.PrintHeader(appInfo, breakLine);

// Say HELLO
ConsoleHelper.SayHello(breakLine);

// Get ARGUMENTS from command line
#if DEBUG
// For debugging purposes, you can manually set the arguments...
if (args.Length < 1)
{
    // ...below
    const string localArgs = "-h";
    args = ConsoleHelper.GetArgs(localArgs);
}
#endif
var arguments = ConsoleHelper.ReadArguments(args);
#if DEBUG
// Write the arguments to the console for debugging purposes
ConsoleHelper.WriteArguments(arguments);
Console.WriteLine(breakLine);
#endif

#endregion

#region MAIN

// Optional argument: doNotWait
var doNotWait = arguments.ContainsKey("-q");

// Show HELP if no arguments are provided or if -h is provided
if (arguments.Count == 0 || arguments.ContainsKey("-h"))
{
    PrintHelp();
    goto EXIT;
}

// Run
Run();

// EXIT the application
EXIT:
Console.WriteLine(breakLine); // print a break line
ConsoleHelper.SayGoodbye(breakLine);
if (!doNotWait) ConsoleHelper.PressAnyKeyToExit();
return;

#endregion

#region HELPERS

static void PrintHelp()
{
    const string userId = "76561197960265729";
    var inputPath = Path.Combine(".", "InputDirectory");
    var exeName = Path.Combine(".", Path.GetFileName(Environment.ProcessPath) ?? "ThisExecutableFileName.exe");
    var helpMessage = $"""
                       Usage: {exeName} -p <input_folder_path> [options]

                       Options:
                         -p <input_folder_path>  Path to folder containing SaveData files
                         -u <user_id>            New User ID (optional)
                         -s                      Switch platform (convert between console and PC SaveData formats)
                         -q                      Don't wait for user input to exit after operation completes (auto-close)
                         -h                      Show this help message

                       Examples:
                         Fix Checksum:  {exeName} -p "{inputPath}"
                         Convert:  {exeName} -p "{inputPath}" -s
                         Update User ID:  {exeName} -p "{inputPath}" -u {userId}
                         Convert, Update User ID, and Fix Checksum:  {exeName} -p "{inputPath}" -s -u {userId}
                       """;
    Console.WriteLine(helpMessage);
}

string GetValidatedInputRootPath()
{
    arguments.TryGetValue("-p", out var inputRootPath);
    if (File.Exists(inputRootPath)) inputRootPath = Path.GetDirectoryName(inputRootPath);
    return !Directory.Exists(inputRootPath)
        ? throw new DirectoryNotFoundException($"The provided path '{inputRootPath}' is not a valid directory or does not exist.")
        : inputRootPath;
}

#endregion

void Run()
{
    var cts = new CancellationTokenSource();
    // Optional argument: switchPlatform
    var switchPlatform = arguments.ContainsKey("-s");
    // Optional argument: newUserId
    arguments.TryGetValue("-u", out var newUserId);
    // Get all SaveData files from the input folder
    var inputFolderPath = GetValidatedInputRootPath();
    // Process the SaveData files
    core.ProcessSaveDataFiles(inputFolderPath, switchPlatform, newUserId, cts);
    cts.Dispose();
}