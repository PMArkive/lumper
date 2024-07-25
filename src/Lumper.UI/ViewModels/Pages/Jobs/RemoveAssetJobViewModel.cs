namespace Lumper.UI.ViewModels.Pages.Jobs;

using System;
using System.Collections.Generic;
using System.Linq;
using Lib.Jobs;
using Lib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Services;
using Shared.Pakfile;
using Views.Pages.Jobs;

public sealed class RemoveAssetJobViewModel : JobViewModel
{
    public class GameSelection : ReactiveObject
    {
        public required string Origin { get; init; }

        [Reactive]
        public bool Selected { get; set; }
    }

    public List<GameSelection> Selection { get; } =
        AssetManifest.Origins
            .Select(origin => new GameSelection { Origin = origin, Selected = true })
            .ToList();

    public override RemoveAssetJob Job { get; }

    public RemoveAssetJobViewModel(RemoveAssetJob job) : base(job)
    {
        RegisterView<RemoveAssetJobViewModel, RemoveAssetJobView>();

        Job = job;

        foreach (GameSelection selection in Selection)
            selection.WhenAnyValue(x => x.Selected)
                .Subscribe(_ =>
                {
                    var selected = Selection.Where(x => x.Selected).ToList();
                    // If all are selected, set filter to null.
                    Job.OriginFilter = selected.Count == AssetManifest.Origins.Count
                        ? null
                        : selected.Select(x => x.Origin).ToList();
                });
    }

    protected override void OnSuccess()
        => BspService.Instance.ResetLumpViewModel(typeof(PakfileLumpViewModel));
}
