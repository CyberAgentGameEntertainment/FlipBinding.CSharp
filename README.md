# FlipBinding.CSharp

A C# binding library for [FLIP](https://github.com/NVlabs/flip) v1.7. Compatible with Unity.

## Installation

### NuGet

```
dotnet add package FlipBinding.CSharp
```

### NuGetForUnity

1. For macOS, create or edit the following file to configure native runtime settings:

   `ProjectSettings/Packages/com.github-glitchenzo.nugetforunity/NativeRuntimeSettings.json`

   ```json
   {
     "configurations": [
       {
         "cpuArchitecture": "AnyCPU",
         "editorCpuArchitecture": "",
         "editorOperatingSystem": "",
         "runtime": "osx",
         "supportedPlatformTargets": [
           "StandaloneOSX"
         ]
       }
     ]
   }
   ```

2. Open the NuGetForUnity window via **NuGet > Manage NuGet Packages**, search for "FlipBinding.CSharp", and click *
   *Install**.

## Usage

### LDR Image Comparison

```csharp
using FlipBinding.CSharp;

// Image data is in linear RGB interleaved format [r,g,b,r,g,b,...]
float[] reference = /* reference image */;
float[] test = /* test image */;

var result = Flip.Evaluate(reference, test, width, height);

// Mean error value (0-1, higher means greater difference)
Console.WriteLine($"Mean FLIP error: {result.MeanError}");

// Get per-pixel error value
float pixelError = result.GetPixel(x, y);
```

### HDR Image Comparison

For HDR image comparison, specify `useHdr: true`:

```csharp
var result = Flip.Evaluate(reference, test, width, height,
        useHdr: true,
        tonemapper,
        startExposure,
        stopExposure,
        numExposures);
```

### Magma Color Map

You can get the error map as an RGB color map for visualization:

```csharp
var result = Flip.Evaluate(reference, test, width, height, applyMagmaMap: true);

// Get RGB values
var (r, g, b) = result.GetPixelRgb(x, y);
```

## API Reference

### Flip.Evaluate

```csharp
public static FlipResult Evaluate(
    float[] reference,           // Reference image (linear RGB)
    float[] test,                // Test image (linear RGB)
    int width,                   // Image width
    int height,                  // Image height
    bool useHdr = false,         // Use HDR-FLIP
    float ppd = 67.02f,          // Pixels per degree. Default value based on: 0.7m viewing distance, 3840px resolution, 0.7m monitor width.
    Tonemapper tonemapper = Tonemapper.Aces,
    float startExposure = float.PositiveInfinity,
    float stopExposure = float.PositiveInfinity,
    int numExposures = -1,
    bool applyMagmaMap = false   // Apply Magma color map
)
```

### PPD (Pixels Per Degree) Calculation

You can calculate PPD from viewing conditions:

```csharp
float ppd = Flip.CalculatePpd(
    viewingDistance: 0.5f,   // Viewing distance (meters)
    resolutionX: 2560,       // Horizontal resolution (pixels)
    monitorWidth: 0.6f       // Monitor width (meters)
);

var result = Flip.Evaluate(reference, test, width, height, ppd: ppd);
```

## Notes

- Image data must be in linear RGB. If using sRGB, convert beforehand
- Array size must be `width * height * 3` (RGB interleaved format)

## License

This library is licensed under the [MIT License](LICENSE.txt).

This library includes binary distributions of [FLIP](https://github.com/NVlabs/flip) v1.7,
which is licensed under the BSD 3-Clause License by NVIDIA CORPORATION & AFFILIATES.
See [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt) for details.
