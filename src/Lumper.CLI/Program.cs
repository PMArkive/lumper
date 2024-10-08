namespace Lumper.CLI;

using CommandLine;
using Lib.BSP;

internal sealed class Program
{
    public static void Main(string[] args)
    {
        var parser = new Parser(with =>
        {
            with.HelpWriter = Console.Out;
            with.CaseInsensitiveEnumValues = true;
        });
        ParserResult<CommandLineOptions> parserResult = parser.ParseArguments<CommandLineOptions>(args);

        string? path = null;
        parserResult
            .WithParsed(x => path = x.Path)
            .WithNotParsed(CommandLineOptions.ErrorHandler);
        if (path == null)
            return;

        if (parserResult.Value.Json != null && parserResult.Value.Json.Any())
        {
            var bspFile = BspFile.FromPath(path, null);

            var sortLumps = parserResult.Value.Json.Any(
                x => x == JsonOptions.SortLumps);
            var sortProperties = parserResult.Value.Json.Any(
                x => x == JsonOptions.SortProperties);
            var ignoreOffset = parserResult.Value.Json.Any(
                x => x == JsonOptions.IgnoreOffset);
            bspFile?.JsonDump((string?)null, null, sortLumps, sortProperties, ignoreOffset);
        }
    }
}
