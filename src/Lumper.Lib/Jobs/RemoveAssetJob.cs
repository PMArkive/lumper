namespace Lumper.Lib.Jobs;

using System.Collections.Generic;
using System.Linq;
using BSP;
using BSP.Lumps.BspLumps;
using BSP.Struct;
using NLog;
using Util;

public class RemoveAssetJob : Job, IJob
{
    public static string JobName => "Remove Game Assets";
    public override string JobNameInternal => JobName;

    /// <summary>
    /// List of origins to remove matching assets from. If null, all matching assets are removed.
    /// The typical use-case for this job is removing *all* assets; if we filled this list with
    /// every current origin, the workflow would become incomplete as sloo
    ///
    /// </summary>
    public List<string>? OriginFilter { get; set; } = [];

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override bool Run(BspFile bsp)
    {
        if (OriginFilter?.Count == 0)
        {
            Logger.Warn("No games selected.");
            return false;
        }

        PakfileLump pakfileLump = bsp.GetLump<PakfileLump>();

        var numMatches = 0;

        // Could probably speed this up a bit by parallelizing, but we can't read
        // multiple zip entries at a time, and that's the most expensive operation here.
        foreach (PakfileEntry entry in pakfileLump.Entries.ToList())
        {
            var hash = entry.HashSHA1;
            if (!AssetManifest.Manifest.TryGetValue(hash, out List<AssetManifest.Asset>? assets))
                continue;

            if (OriginFilter is not null && !assets.Any(asset => OriginFilter.Contains(asset.Origin)))
                continue;

            pakfileLump.Entries.Remove(entry);
            numMatches++;
            var matches = string.Join(", ", assets.Select(asset => $"{asset.Origin} asset {asset.Path}"));
            Logger.Info($"Removed {entry.Key} which matched {matches}");
        }

        if (numMatches > 0)
        {
            Logger.Info($"Removed {numMatches} game assets!");
            pakfileLump.IsModified = true;
            return true;
        }
        else
        {
            Logger.Info("Did not find any game assets to remove.");
            return false;
        }
    }
}
