using System;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VTFLib;

namespace Lumper.UI.Models;

public class VtfFileData
{
    private bool _opened;
    private readonly uint _imageIndex;

    public readonly uint _frameCount;
    public readonly uint _faceCount;
    public readonly uint _mipmapCount;
    public readonly uint _depth;
    public readonly VTFImageFlag _flags;

    public VtfFileData(byte[] vtfBuffer)
    {
        VTFAPI.Initialize();

        if (!_opened)
        {
            VTFFile.CreateImage(ref _imageIndex);
            _opened = true;
        }

        VTFFile.BindImage(_imageIndex);
        VTFFile.ImageLoadLump(vtfBuffer, (uint)vtfBuffer.Length, false);

        _depth = VTFFile.ImageGetDepth();
        _frameCount = VTFFile.ImageGetFrameCount();
        _faceCount = VTFFile.ImageGetFaceCount();
        _mipmapCount = VTFFile.ImageGetMipmapCount();
        _flags = (VTFImageFlag)VTFFile.ImageGetFlags();
    }


    public bool Bind()
    {
        return VTFFile.BindImage(_imageIndex);
    }

    public Image<Rgba32>? GetImage(uint frame, uint face, uint slice, uint mipmapLevel)
    {
        if (VTFFile.ImageGetHasImage() == 0)
        {
            return null;
        }

        uint w = VTFFile.ImageGetWidth();
        uint h = VTFFile.ImageGetHeight();
        VTFImageFormat f = VTFFile.ImageGetFormat();
        IntPtr imageData = VTFFile.ImageGetData(frame, face, slice, mipmapLevel);
        int size = (int)VTFFile.ImageComputeImageSize(w, h, 1, 1, f);

        byte[] data = new byte[size];
        Marshal.Copy(imageData, data, 0, size);

        return GetImage(data, w, h, f);
    }

    private Image<Rgba32> GetImage(byte[] source, uint width, uint height, VTFImageFormat format)
    {
        int size = (int)width * (int)height * 4;
        if (size <= 0)
            throw new ArgumentException("image data array size is 0");
        byte[] dest = new byte[size];
        VTFFile.ImageConvertToRGBA8888(source, dest, width, height, format);
        return GetImageFromRgba8888(dest, (int)width, (int)height);
    }

    private static Image<Rgba32> GetImageFromRgba8888(byte[] img, int width, int height)
    {
        var rgba = new Rgba32[width * height];
        int j = 0;
        for (int i = 0; i < img.Length; i += 4)
        {
            rgba[j++] = new Rgba32(
                img[i],
                img[i + 1],
                img[i + 2],
                img[i + 3]);
        }

        return Image.LoadPixelData<Rgba32>(rgba.AsSpan(), width, height);
    }
}
