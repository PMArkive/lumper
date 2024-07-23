namespace Lumper.CLI;

using CommandLine;

[Flags]
public enum JsonOptions
{
    SortLumps = 0x1,
    SortProperties = 0x2,
    IgnoreOffset = 0x4
}

public class CommandLineOptions
{
    [Value(index: 0, Required = true, MetaName = "BSP File", HelpText = "Path to input BSP file")]
    public required string Path { get; set; }

    [Option('j',
        "json",
        Default = null,
        Required = false,
        HelpText = "Output JSON summary of the BSP to <directory containing BSP file>/<bsp file name>.json")]
    public bool JsonDump { get; set; }

    [Option('J',
        "jsonPath",
        Default = null,
        Required = false,
        HelpText =
            "Output JSON summary of the BSP the given path, relative to current directory. If given, --json can be omitted.")]
    public string? JsonPath { get; set; }

    [Option('k',
        "jsonOpts",
        Default = CLI.JsonOptions.SortLumps | CLI.JsonOptions.SortProperties | CLI.JsonOptions.IgnoreOffset,
        Required = false,
        HelpText =
            "Provide either flags (1 = Sort Lumps, 2 = Sort Properties, 4 = Ignore Offsets)," +
            " or comma-separator list of names, e.g. sortproperties,ignoreoffsets.")]
    public JsonOptions JsonOptions { get; set; }
}
