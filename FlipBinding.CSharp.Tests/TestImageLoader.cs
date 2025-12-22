// SPDX-FileCopyrightText: 2025 CyberAgent, Inc.
// SPDX-License-Identifier: MIT

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TinyEXR;

namespace FlipBinding.CSharp.Tests;

/// <summary>
/// Utility class for loading test images from the flip submodule.
/// </summary>
internal static class TestImageLoader
{
    /// <summary>
    /// Base path to the flip submodule from the test project.
    /// </summary>
    private static readonly string s_flipBasePath =
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "flip"));

    /// <summary>
    /// Path to test images in the flip submodule.
    /// </summary>
    private static string ImagesPath => Path.Combine(s_flipBasePath, "images");

    /// <summary>
    /// Path to test reference images (correct FLIP outputs).
    /// </summary>
    private static string TestsPath => Path.Combine(s_flipBasePath, "src", "tests");

    /// <summary>
    /// Gets the full path to a test image.
    /// </summary>
    public static string GetImagePath(string filename) => Path.Combine(ImagesPath, filename);

    /// <summary>
    /// Gets the full path to a test reference image.
    /// </summary>
    public static string GetTestReferencePath(string filename) => Path.Combine(TestsPath, filename);

    /// <summary>
    /// Loads a PNG image and converts it to a float array in RGB interleaved format.
    /// Values are converted from sRGB to linear RGB.
    /// </summary>
    /// <param name="path">Path to the PNG file.</param>
    /// <returns>Tuple of (RGB float array, width, height).</returns>
    public static (float[] Data, int Width, int Height) LoadPngAsRgbFloat(string path)
    {
        using var image = Image.Load<Rgb24>(path);
        var width = image.Width;
        var height = image.Height;
        var data = new float[width * height * 3];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = image[x, y];
                var index = (y * width + x) * 3;
                data[index] = SRgbToLinearRgb(pixel.R / 255f);
                data[index + 1] = SRgbToLinearRgb(pixel.G / 255f);
                data[index + 2] = SRgbToLinearRgb(pixel.B / 255f);
            }
        }

        return (data, width, height);
    }

    /// <summary>
    /// Converts a single sRGB channel value to linear RGB.
    /// Uses the standard sRGB transfer function.
    /// </summary>
    /// <param name="sC">sRGB channel value in [0,1] range.</param>
    /// <returns>Linear RGB channel value.</returns>
    private static float SRgbToLinearRgb(float sC)
    {
        if (sC <= 0.04045f)
            return sC / 12.92f;
        else
            return MathF.Pow((sC + 0.055f) / 1.055f, 2.4f);
    }

    /// <summary>
    /// Loads a PNG image and converts it to a float array in RGB interleaved format.
    /// Values are normalized to [0,1] range (sRGB values, NOT converted to linear).
    /// Use this for loading Magma colormap images or other sRGB output images.
    /// </summary>
    /// <param name="path">Path to the PNG file.</param>
    /// <returns>Tuple of (RGB float array, width, height).</returns>
    public static (float[] Data, int Width, int Height) LoadPngAsRgbFloatSrgb(string path)
    {
        using var image = Image.Load<Rgb24>(path);
        var width = image.Width;
        var height = image.Height;
        var data = new float[width * height * 3];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = image[x, y];
                var index = (y * width + x) * 3;
                data[index] = pixel.R / 255f;
                data[index + 1] = pixel.G / 255f;
                data[index + 2] = pixel.B / 255f;
            }
        }

        return (data, width, height);
    }

    /// <summary>
    /// Loads a PNG image and converts it to a single-channel float array.
    /// Used for loading FLIP error map images.
    /// </summary>
    /// <param name="path">Path to the PNG file.</param>
    /// <returns>Tuple of (grayscale float array, width, height).</returns>
    public static (float[] Data, int Width, int Height) LoadPngAsGrayscaleFloat(string path)
    {
        using var image = Image.Load<Rgb24>(path);
        var width = image.Width;
        var height = image.Height;
        var data = new float[width * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = image[x, y];
                var index = y * width + x;
                // FLIP error maps are stored as magma colormap, use R channel as approximation
                data[index] = pixel.R / 255f;
            }
        }

        return (data, width, height);
    }

    /// <summary>
    /// Saves a float array in RGB interleaved format as a PNG image.
    /// </summary>
    /// <param name="data">RGB float array with values in [0,1] range.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="path">Path to save the PNG file.</param>
    public static void SaveRgbFloatAsPng(float[] data, int width, int height, string path)
    {
        using var image = new Image<Rgb24>(width, height);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = (y * width + x) * 3;
                var r = (byte)Math.Clamp(data[index] * 255f + 0.5f, 0, 255);
                var g = (byte)Math.Clamp(data[index + 1] * 255f + 0.5f, 0, 255);
                var b = (byte)Math.Clamp(data[index + 2] * 255f + 0.5f, 0, 255);
                image[x, y] = new Rgb24(r, g, b);
            }
        }

        image.SaveAsPng(path);
    }

    /// <summary>
    /// Loads an EXR image and converts it to a float array in RGB interleaved format.
    /// Values are in linear HDR range (can exceed 1.0).
    /// </summary>
    /// <param name="path">Path to the EXR file.</param>
    /// <returns>Tuple of (RGB float array, width, height).</returns>
    public static (float[] Data, int Width, int Height) LoadExrAsRgbFloat(string path)
    {
        var reader = new SinglePartExrReader();
        reader.Read(path);
        var width = reader.Width;
        var height = reader.Height;

        // Get channel data as bytes and convert to float
        var rBytes = reader.GetImageData("R");
        var gBytes = reader.GetImageData("G");
        var bBytes = reader.GetImageData("B");

        // Convert bytes to float arrays (assuming half or float format)
        var rData = ConvertChannelToFloat(rBytes, reader.Channels.First(c => c.Name == "R").Type);
        var gData = ConvertChannelToFloat(gBytes, reader.Channels.First(c => c.Name == "G").Type);
        var bData = ConvertChannelToFloat(bBytes, reader.Channels.First(c => c.Name == "B").Type);

        // Interleave RGB data
        var rgbData = new float[width * height * 3];
        for (var i = 0; i < width * height; i++)
        {
            rgbData[i * 3] = rData[i];
            rgbData[i * 3 + 1] = gData[i];
            rgbData[i * 3 + 2] = bData[i];
        }

        return (rgbData, width, height);
    }

    private static float[] ConvertChannelToFloat(ReadOnlySpan<byte> bytes, ExrPixelType pixelType)
    {
        // ExrPixelType: Uint = 0, Half = 1, Float = 2
        int pixelCount;
        float[] result;

        switch (pixelType)
        {
            case ExrPixelType.Half: // Half (16-bit float)
                pixelCount = bytes.Length / 2;
                result = new float[pixelCount];
                for (var i = 0; i < pixelCount; i++)
                {
                    var halfBits = BitConverter.ToUInt16(bytes.Slice(i * 2, 2));
                    result[i] = (float)BitConverter.UInt16BitsToHalf(halfBits);
                }

                break;

            case ExrPixelType.Float: // Float (32-bit float)
                pixelCount = bytes.Length / 4;
                result = new float[pixelCount];
                for (var i = 0; i < pixelCount; i++)
                {
                    result[i] = BitConverter.ToSingle(bytes.Slice(i * 4, 4));
                }

                break;

            default: // Uint (32-bit)
                pixelCount = bytes.Length / 4;
                result = new float[pixelCount];
                for (var i = 0; i < pixelCount; i++)
                {
                    result[i] = BitConverter.ToUInt32(bytes.Slice(i * 4, 4)) / (float)uint.MaxValue;
                }

                break;
        }

        return result;
    }
}
