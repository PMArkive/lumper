using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lumper.UI.ViewModels.VtfBrowser;

public class VtfBrowserItemViewModel : ViewModelBase
{
    private string _name = "";

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private Image<Rgba32>? _image;

    public Image<Rgba32>? Image
    {
        get => _image;
        set => this.RaiseAndSetIfChanged(ref _image, value);
    }
}
