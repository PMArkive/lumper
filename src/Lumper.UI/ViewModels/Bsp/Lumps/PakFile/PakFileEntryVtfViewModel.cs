using System;
using System.IO;
using Lumper.Lib.BSP.Struct;
using Lumper.UI.Models;
using Lumper.UI.ViewModels.VtfBrowser;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VTFLib;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;

public class PakFileEntryVtfViewModel : PakFileEntryLeafViewModel
{
    public PakFileEntryVtfViewModel(PakFileEntryBranchViewModel parent,
        PakFileEntry entry, string name)
        : base(parent, entry, name)
    {
        VtfBrowserViewModel.AddTexture(entry, name);
    }

    public override BspNodeBase? ViewNode => this;

    public override string NodeName =>
        $"PakFileEntry{(string.IsNullOrWhiteSpace(Name) ? "" : $" ({Name})")}";

    private string _info = "";

    public string Info
    {
        get => _info;
        set => this.RaiseAndSetIfChanged(ref _info, value);
    }

    private bool _isModified = false;

    public override bool IsModified
    {
        get => _isModified;
    }

    public Image<Rgba32>? _image = null;

    public Image<Rgba32>? Image
    {
        get => _image;
        private set => this.RaiseAndSetIfChanged(ref _image, value);
    }

    private uint _frame;

    public uint Frame
    {
        get => _frame;
        private set
        {
            this.RaiseAndSetIfChanged(ref _frame, value);
            UpdateImage();
        }
    }

    private uint _face;

    public uint Face
    {
        get => _face;
        private set
        {
            this.RaiseAndSetIfChanged(ref _face, value);
            UpdateImage();
        }
    }

    private uint _slice;

    public uint Slice
    {
        get => _slice;
        private set
        {
            this.RaiseAndSetIfChanged(ref _slice, value);
            UpdateImage();
        }
    }

    private uint _mipmapLevel;

    public uint MipmapLevel
    {
        get => _mipmapLevel;
        private set
        {
            this.RaiseAndSetIfChanged(ref _mipmapLevel, value);
            UpdateImage();
        }
    }

    public uint FrameMax => _vtfData?._frameCount - 1 ?? 0;
    public uint FaceMax => _vtfData?._faceCount - 1 ?? 0;
    public uint MipmapMax => _vtfData?._mipmapCount - 1 ?? 0;

    private VtfFileData? _vtfData;

    public override void Open()
    {
        //can't get length for byte array from LzmaStream
        //so we need to read to a different stream first
        using var mem = new MemoryStream();
        _entry.DataStream.CopyTo(mem);
        _vtfData = new VtfFileData(mem.ToArray());

        this.RaisePropertyChanged(nameof(FrameMax));
        this.RaisePropertyChanged(nameof(FaceMax));
        this.RaisePropertyChanged(nameof(MipmapMax));

        Info = $"MajorVersion: {VTFFile.ImageGetMajorVersion()}\n" +
               $"MinorVersion: {VTFFile.ImageGetMinorVersion()}\n" +
               $"Size: {VTFFile.ImageGetSize()}\n" +
               $"Width: {VTFFile.ImageGetWidth()}\n" +
               $"Height: {VTFFile.ImageGetHeight()}\n" +
               $"Format: {Enum.GetName(VTFFile.ImageGetFormat())}\n" +
               $"Depth: {_vtfData._depth}\n" +
               $"FrameCount: {_vtfData._frameCount}\n" +
               $"FaceCount: {_vtfData._faceCount}\n" +
               $"MipmapCount: {_vtfData._mipmapCount}\n" +
               $"Flags: {_vtfData._flags.ToString().Replace(",", "\n")}\n";

        UpdateImage();
    }

    private void UpdateImage()
    {
        Image = _vtfData?.GetImage(Frame, Face, Slice, MipmapLevel);
    }

    public static Image<Rgba32> ImageFromFileStream(Stream fileSteam)
    {
        return SixLabors.ImageSharp.Image.Load<Rgba32>(fileSteam);
    }

    public void SetImageData(Image<Rgba32> image)
    {
        if (!_vtfData!.Bind())
        {
            return;
        }

        _isModified = true;
        byte[] buffer = GetRgba888FromImage(image, out _);

        var f = VTFFile.ImageGetFormat();
        int size = (int)VTFFile.ImageComputeImageSize(
            (uint)image.Width, (uint)image.Height, 1, 1, f);
        byte[] buffer2 = new byte[size];
        VTFFile.ImageConvertFromRGBA8888(
            buffer,
            buffer2,
            (uint)image.Width,
            (uint)image.Height,
            f
        );
        VTFFile.ImageSetData(Frame, Face, Slice, MipmapLevel, buffer2);
        SaveVtf();
    }

    public void SetNewImage(Image<Rgba32> image)
    {
        if (!_vtfData!.Bind())
        {
            return;
        }

        _isModified = true;
        byte[] buffer = GetRgba888FromImage(image, out bool hasAlpha);
        var createOptions = new SVTFCreateOptions();
        VTFFile.ImageCreateDefaultCreateStructure(ref createOptions);
        createOptions.imageFormat =
            hasAlpha ? VTFImageFormat.IMAGE_FORMAT_DXT5 : VTFImageFormat.IMAGE_FORMAT_DXT1;
        if (!VTFFile.ImageCreateSingle(
                (uint)image.Width,
                (uint)image.Height,
                buffer,
                ref createOptions))
        {
            string err = VTFAPI.GetLastError();
            Console.WriteLine(err);
        }

        SaveVtf();
    }

    public void SaveVtf()
    {
        var vtfBuffer = new byte[VTFFile.ImageGetSize()];
        _entry.DataStream = new MemoryStream(vtfBuffer);

        uint uiSize = 0;
        if (!VTFFile.ImageSaveLump(vtfBuffer, (uint)vtfBuffer.Length, ref uiSize))
        {
            string err = VTFAPI.GetLastError();
            Console.WriteLine(err);
        }

        _entry.DataStream.Seek(0, SeekOrigin.Begin);
    }

    private static byte[] GetRgba888FromImage(Image<Rgba32> image, out bool hasAlpha)
    {
        int size = image.Width * image.Height * 4;
        using var mem = new MemoryStream();
        var buffer = new byte[size];
        int i = 0;
        hasAlpha = false;
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Rgba32 pixel = image[x, y];
                buffer[i++] = pixel.R;
                buffer[i++] = pixel.G;
                buffer[i++] = pixel.B;
                buffer[i++] = pixel.A;
                if (!hasAlpha && pixel.A != 255)
                    hasAlpha = true;
            }
        }

        return buffer;
    }

    public override void Update()
    {
        _isModified = false;
        base.Update();
    }
}
