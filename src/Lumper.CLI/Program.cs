namespace Lumper.CLI;

using CommandLine;
using CommandLine.Text;
using Lib.BSP;
using NLog;
using NLog.Targets;

internal sealed class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly string
        Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "Unknown Version";

    /// <summary>
    /// The main application logic of the program, called once the command line options have been parsed.
    ///
    /// It can throw at any point, and the exception will be caught and program will exit with non-zero
    /// status code.
    ///
    /// If method completes successfully, the program will exit with status code 0.
    /// </summary>
    private static void Run(CommandLineOptions options)
    {
        var bspFile = BspFile.FromPath(options.Path, null);

        if (bspFile is null)
            throw new InvalidDataException("Failed to load BSP file");

        if (options.JsonDump || options.JsonPath is not null)
        {
            bspFile.JsonDump(
                options.JsonPath ?? null,
                null,
                sortLumps: options.JsonOptions.HasFlag(JsonOptions.SortLumps),
                sortProperties: options.JsonOptions.HasFlag(JsonOptions.SortProperties),
                ignoreOffset: options.JsonOptions.HasFlag(JsonOptions.IgnoreOffset));
        }


    }

    /// <summary>
    /// Parses CLI options and wraps the main application logic with exception handling.
    /// </summary>
    public static int Main(string[] args)
    {
        // Dig out the console target to remove date and other crap from layout.
        LogManager.Configuration.LoggingRules
            .Select(rule => rule.Targets.First())
            .OfType<ConsoleTarget>()
            .First().Layout = "${message:withException=true}";

        ParserResult<CommandLineOptions> parserResult = new Parser(with =>
        {
            with.CaseInsensitiveEnumValues = true;
            with.HelpWriter = null;
            with.AutoHelp = true;
            with.AutoVersion = true;
        }).ParseArguments<CommandLineOptions>(args);

        return parserResult.MapResult(
            options =>
            {
                try
                {
                    Run(options);
                    return 0;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    return 1;
                }
            },
            errors =>
            {
                // ReSharper disable PossibleMultipleEnumeration
                if (errors.IsVersion())
                {
                    Logger.Info($"Lumper CLI v{Version}");
                    return 0;
                }

                Logger.Info(HelpText.AutoBuild(parserResult,
                    h =>
                    {
                        h.Heading = $"Lumper CLI v{Version}\nhttps://github.com/momentum-mod/lumper";
                        h.AddNewLineBetweenHelpSections = false;
                        h.AdditionalNewLineAfterOption = false;
                        h.MaximumDisplayWidth = 120;
                        h.Copyright = "";
                        return HelpText.DefaultParsingErrorsHandler(parserResult, h);
                    },
                    e => e));

                return errors.IsHelp() ? 0 : 1;
            });
    }
}
