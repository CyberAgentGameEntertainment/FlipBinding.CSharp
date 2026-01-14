# FLIP C# バインディング サンプルコード

このドキュメントでは、`FlipBinding.CSharp` ライブラリの使用方法と、Python の FLIP コマンドとのデフォルト値の違いについて説明します。

## Python FLIP vs C# バインディングのデフォルト比較

| パラメータ | Python デフォルト | C# デフォルト | 備考 |
|-----------|-----------------|--------------|------|
| PPD | 67.02 | 67.02 | 同じ |
| Tonemapper | ACES | ACES | 同じ |
| applyMagma | **True** | **False** | Python に合わせるなら `true` を指定 |
| inputsRGB | **True (sRGB入力)** | **linear RGB前提** | sRGB→linear 変換が必要 |
| startExposure | infinity | PositiveInfinity | 同じ（自動計算） |
| stopExposure | infinity | PositiveInfinity | 同じ（自動計算） |
| numExposures | -1 | -1 | 同じ（自動計算） |

### 重要な違い

1. **applyMagmaMap**: Python のデフォルトは `True`（Magma カラーマップ適用）ですが、C# のデフォルトは `false`（グレースケール）です。Python と同じ出力を得るには `applyMagmaMap: true` を指定してください。

2. **入力色空間**: Python の FLIP は sRGB 入力を自動で linear RGB に変換しますが、C# バインディングは linear RGB を前提としています。PNG 画像を読み込む場合は、sRGB から linear RGB への変換が必要です。

## サンプルコード

### 基本的な使用例（Python デフォルト互換）

```csharp
using FlipBinding.CSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

// 画像読み込み (sRGB PNG から linear RGB へ変換)
var (referenceData, width, height) = LoadPngAsLinearRgb("reference.png");
var (testData, _, _) = LoadPngAsLinearRgb("test.png");

// Python デフォルトと同じ設定で FLIP 評価
var result = Flip.Evaluate(
    referenceData,
    testData,
    width,
    height,
    useHdr: false,               // LDR-FLIP
    ppd: 67.02f,                 // デフォルト（省略可）
    tonemapper: Tonemapper.Aces, // デフォルト（省略可）
    applyMagmaMap: true          // Python デフォルトに合わせる
);

Console.WriteLine($"Mean FLIP error: {result.MeanError:F6}");

// Magma カラーマップ付きエラーマップを保存
SaveRgbFloatAsPng(result.ErrorMap, result.Width, result.Height, "flip_error.png");
```

### 最小限の使用例（C# デフォルト）

```csharp
using FlipBinding.CSharp;

// 画像データは linear RGB float 配列 [r,g,b,r,g,b,...] 形式
float[] referenceData = /* ... */;
float[] testData = /* ... */;
int width = 1920;
int height = 1080;

// C# デフォルト設定で評価（グレースケール出力）
var result = Flip.Evaluate(referenceData, testData, width, height);

Console.WriteLine($"Mean FLIP error: {result.MeanError:F6}");

// グレースケールエラーマップへのアクセス
float errorAtPixel = result.GetPixel(x, y);
```

### HDR-FLIP の使用例

```csharp
using FlipBinding.CSharp;

// HDR 画像データ（linear RGB、値は 1.0 を超える場合あり）
float[] referenceHdr = /* EXR などから読み込み */;
float[] testHdr = /* EXR などから読み込み */;

var result = Flip.Evaluate(
    referenceHdr,
    testHdr,
    width,
    height,
    useHdr: true,                         // HDR-FLIP を使用
    tonemapper: Tonemapper.Aces,          // トーンマッパー選択
    startExposure: float.PositiveInfinity, // 自動計算
    stopExposure: float.PositiveInfinity,  // 自動計算
    numExposures: -1,                      // 自動計算
    applyMagmaMap: true
);

Console.WriteLine($"HDR-FLIP Mean error: {result.MeanError:F6}");
```

### PPD のカスタム計算

```csharp
// 視聴条件からPPDを計算
float ppd = Flip.CalculatePpd(
    viewingDistance: 0.5f,  // モニターからの距離（メートル）
    resolutionX: 2560,      // 水平解像度（ピクセル）
    monitorWidth: 0.6f      // モニターの物理幅（メートル）
);

var result = Flip.Evaluate(referenceData, testData, width, height, ppd: ppd);
```

## ヘルパー関数

### sRGB PNG から linear RGB への変換

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// sRGB PNG 画像を linear RGB float 配列として読み込みます。
/// </summary>
static (float[] Data, int Width, int Height) LoadPngAsLinearRgb(string path)
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
            data[index] = SRgbToLinear(pixel.R / 255f);
            data[index + 1] = SRgbToLinear(pixel.G / 255f);
            data[index + 2] = SRgbToLinear(pixel.B / 255f);
        }
    }
    return (data, width, height);
}

/// <summary>
/// sRGB から linear RGB への変換（標準 sRGB 伝達関数）。
/// </summary>
static float SRgbToLinear(float sC)
{
    return sC <= 0.04045f
        ? sC / 12.92f
        : MathF.Pow((sC + 0.055f) / 1.055f, 2.4f);
}
```

### RGB float 配列から PNG への保存

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// RGB float 配列を PNG 画像として保存します。
/// Magma カラーマップ付きエラーマップの保存に使用します。
/// </summary>
static void SaveRgbFloatAsPng(float[] data, int width, int height, string path)
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
```

### グレースケールエラーマップから PNG への保存

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// グレースケール float 配列を PNG 画像として保存します。
/// applyMagmaMap: false の場合のエラーマップ保存に使用します。
/// </summary>
static void SaveGrayscaleFloatAsPng(float[] data, int width, int height, string path)
{
    using var image = new Image<L8>(width, height);
    for (var y = 0; y < height; y++)
    {
        for (var x = 0; x < width; x++)
        {
            var index = y * width + x;
            var gray = (byte)Math.Clamp(data[index] * 255f + 0.5f, 0, 255);
            image[x, y] = new L8(gray);
        }
    }
    image.SaveAsPng(path);
}
```

## 必要なパッケージ

```xml
<PackageReference Include="FlipBinding.CSharp" Version="x.x.x" />
<PackageReference Include="SixLabors.ImageSharp" Version="3.1.6" />
```

## FlipResult の使い方

```csharp
var result = Flip.Evaluate(referenceData, testData, width, height, applyMagmaMap: true);

// 基本情報
Console.WriteLine($"Mean Error: {result.MeanError}");
Console.WriteLine($"Width: {result.Width}");
Console.WriteLine($"Height: {result.Height}");
Console.WriteLine($"Is Magma Map: {result.IsMagmaMap}");

// ピクセルアクセス
if (result.IsMagmaMap)
{
    // Magma カラーマップの場合は RGB で取得
    var (r, g, b) = result.GetPixelRgb(x, y);
}
else
{
    // グレースケールの場合は単一値で取得
    float error = result.GetPixel(x, y);
}
```

## 参考リンク

- [FLIP (GitHub)](https://github.com/NVlabs/flip) - オリジナルの FLIP 実装
- [FLIP 論文](https://research.nvidia.com/publication/2020-07_FLIP) - NVIDIA Research