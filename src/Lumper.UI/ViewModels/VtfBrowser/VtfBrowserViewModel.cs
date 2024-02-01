using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lumper.Lib.BSP.Struct;
using Lumper.UI.Models;
using Lumper.UI.Models.Matchers;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lumper.UI.ViewModels.VtfBrowser;

public partial class VtfBrowserViewModel : ViewModelBase
{
    public class Vtf : ReactiveObject
    {
        private Image<Rgba32>? _image;
        private string _name = "";

        public Image<Rgba32>? Image
        {
            get => _image;
            set => this.RaiseAndSetIfChanged(ref _image, value);
        }

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
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

    [GeneratedRegex(@"^((c-?\d+_-?\d+_-?\d+)|cubemapdefault)[\.hdr]{0,}\.vtf$")]
    private static partial Regex _rgxCubemap();

    private static ObservableCollection<Vtf> _textureBrowserItems =
        new ObservableCollection<Vtf>();

    public ObservableCollection<Vtf> TextureBrowserItems
    {
        get
        {
            var isGlobPattern = TextureSearch.Contains('*') || TextureSearch.Contains('?');
            var matcher = isGlobPattern
                ? new GlobMatcher(TextureSearch, false, true)
                : new GlobMatcher($"*{TextureSearch}*", false, true);

            var localMatcher = matcher;

            return new ObservableCollection<Vtf>(
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

    public static void AddTexture(PakFileEntry entry, string name)
    {
        //duplicate code from PakFileEntryVtfViewModel::Open
        using var mem = new MemoryStream();
        entry.DataStream.CopyTo(mem);
        var vtfFileData = new VtfFileData(mem.ToArray());

        _textureBrowserItems.Add(new Vtf
        {
            Name = name,
            Image = vtfFileData.GetImage(0, 0, 0, 0)
        });
    }

    public static void ClearTextures()
    {
        _textureBrowserItems.Clear();
    }
}
