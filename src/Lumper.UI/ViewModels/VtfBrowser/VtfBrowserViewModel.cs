using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Struct;
using Lumper.UI.Models;
using Lumper.UI.Models.Matchers;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lumper.UI.ViewModels.VtfBrowser;

public partial class VtfBrowserViewModel : ViewModelBase
{
    public VtfBrowserViewModel(PakFileLump pakFileLump)
    {
        var entries = pakFileLump.Entries.Where(
            x => x.Key.ToLower().EndsWith(".vtf"));
        foreach (var entry in entries)
        {
            //todo name instead of key
            AddTexture(entry, entry.Key);
        }
    }
    private double _dimensions = 128;

    public double Dimensions
    {
        get => _dimensions;
        set
        {
            this.RaiseAndSetIfChanged(ref _dimensions, value);
            this.RaisePropertyChanged(nameof(MaxNameWidth));
        }
    }

    // 8 is how much the text will be offset from the sides, so 4px left and 4px right
    public uint MaxNameWidth => (uint)_dimensions - 8;

    private bool _showCubemaps;

    public bool ShowCubemaps
    {
        get => _showCubemaps;
        set
        {
            this.RaiseAndSetIfChanged(ref _showCubemaps, value);
            this.RaisePropertyChanged(nameof(TextureBrowserItems));
            this.RaisePropertyChanged(nameof(TexturesCount));
        }
    }

    // matches cubemap names which are formatted as cX_cY_cZ.vtf or cubemapdefault.vtf, including .hdr.vtf versions
    // X Y Z are the cubemap's origin
    // https://github.com/ValveSoftware/source-sdk-2013/blob/master/mp/src/utils/vbsp/cubemap.cpp
    [GeneratedRegex(@"^((c-?\d+_-?\d+_-?\d+)|cubemapdefault)(\.hdr){0,}\.vtf$")]
    private static partial Regex _rgxCubemap();

    private ObservableCollection<VtfBrowserItemViewModel> _textureBrowserItems =
        new();

    public ObservableCollection<VtfBrowserItemViewModel> TextureBrowserItems
    {
        get
        {
            bool isGlobPattern = TextureSearch.Contains('*') || TextureSearch.Contains('?');
            var matcher = isGlobPattern
                ? new GlobMatcher(TextureSearch, false, true)
                : new GlobMatcher($"*{TextureSearch}*", false, true);

            var localMatcher = matcher;

            return new ObservableCollection<VtfBrowserItemViewModel>(
                _textureBrowserItems.Where(t => // delegate allocation but other way is ugly lol
                {
                    if (!_showCubemaps && _rgxCubemap().IsMatch(t.Name))
                    {
                        return false;
                    }

                    return string.IsNullOrWhiteSpace(TextureSearch)
                           || localMatcher.Match(t.Name).Result;
                }));
        }
    }

    private string _textureSearch = "";

    public string TextureSearch
    {
        get => _textureSearch;
        set
        {
            this.RaiseAndSetIfChanged(ref _textureSearch, value);
            this.RaisePropertyChanged(nameof(TextureBrowserItems));
            this.RaisePropertyChanged(nameof(TexturesCount));
        }
    }

    public string TexturesCount => $"{TextureBrowserItems.Count} Items";

    private void AddTexture(PakFileEntry entry, string name)
    {
        _textureBrowserItems.Add(new VtfBrowserItemViewModel(
            name, new VtfFileData(entry)));
    }
}
