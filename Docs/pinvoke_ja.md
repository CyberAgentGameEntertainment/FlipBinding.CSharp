# P/Invoke によるC++ライブラリのバインディング

## 概要

P/Invoke (Platform Invoke) は、C#からネイティブライブラリ（C/C++）を直接呼び出すための.NET標準機能です。Rustなどの中間言語を必要とせず、C#だけでバインディングを実装できます。

## アーキテクチャ

```
C++ライブラリ
    ↓
Cスタイルラッパー (extern "C")
    ↓
ネイティブライブラリ (.dll / .so / .dylib)
    ↓
[DllImport] / [LibraryImport]
    ↓
C# アプリ / Unity
```

## 前提条件

C++ライブラリをP/Invokeで呼び出すには、**Cスタイルのエクスポート関数**が必要です。C++のクラスやテンプレートは直接呼び出せないため、`extern "C"` でラップする必要があります。

## C++側の準備

### エクスポートマクロの定義

```cpp
// flip_capi.h
#pragma once

#ifdef _WIN32
    #ifdef FLIP_EXPORTS
        #define FLIP_API __declspec(dllexport)
    #else
        #define FLIP_API __declspec(dllimport)
    #endif
#else
    #define FLIP_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

// ここにエクスポート関数を定義

#ifdef __cplusplus
}
#endif
```

## C#側の実装

### .NET Standard 2.1 / Unity (DllImport)

```csharp
using System;
using System.Runtime.InteropServices;

namespace FlipBinding.CSharp
{
    public static class FlipNative
    {
        private const string DllName = "flip_native";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void flip_parameters_init(ref FlipParameters parameters);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void flip_evaluate(
            float* reference,
            float* test,
            int width,
            int height,
            int useHdr,
            FlipParameters* parameters,
            int applyMagmaMap,
            int computeMeanError,
            float* meanError,
            float** errorMap
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void flip_free(IntPtr ptr);
    }
}
```

## 型マッピング

### 基本型

| C/C++ | C# |
|-------|-----|
| `int` | `int` |
| `unsigned int` | `uint` |
| `float` | `float` |
| `double` | `double` |
| `bool` | `int` (0 = false, 非0 = true) |
| `char*` | `string` または `IntPtr` |
| `void*` | `IntPtr` |
| `T*` | `T[]`, `T*` (unsafe), `IntPtr` |
| `T**` | `T**` (unsafe), `out IntPtr` |

### 構造体

C++側：
```cpp
typedef struct {
    float ppd;
    float start_exposure;
    float stop_exposure;
    int num_exposures;
    const char* tonemapper;
} FlipParameters;
```

C#側：
```csharp
[StructLayout(LayoutKind.Sequential)]
public struct NativeFlipParameters
{
    public float Ppd;
    public float StartExposure;
    public float StopExposure;
    public int NumExposures;
    public IntPtr Tonemapper; // const char*
}
```

### 配列の受け渡し

```csharp
// 方法1: マネージド配列
[DllImport("flip_native")]
public static extern void process(float[] data, int length);

// 方法2: unsafe ポインタ
[DllImport("flip_native")]
public static extern unsafe void process(float* data, int length);
```

## メモリ管理

### 基本原則

- **C++で確保したメモリはC++で解放**
- **C#で確保したメモリはC#で解放**

### FLIP API のメモリ管理パターン

FLIP の `flip_evaluate` 関数は、内部でエラーマップ用のメモリを確保します。
呼び出し側は `flip_free` を使用してこのメモリを解放する必要があります。

```cpp
// C++ (flip_capi.h)
extern "C" {
    // ライブラリがメモリを確保
    FLIP_API void flip_evaluate(
        const float* reference,
        const float* test,
        int width,
        int height,
        int use_hdr,
        const FlipParameters* parameters,
        int apply_magma_map,
        int compute_mean_error,
        float* mean_error,
        float** error_map  // ライブラリが確保
    );

    // ライブラリが確保したメモリを解放
    FLIP_API void flip_free(void* ptr);
}
```

```csharp
// C#
float* errorMapPtr = null;

FlipNative.Evaluate(
    pRef, pTest, width, height,
    useHdr, &nativeParams,
    0, 1, &meanError, &errorMapPtr
);

// ネイティブメモリからマネージド配列にコピー
float[] errorMap = new float[width * height];
Marshal.Copy((IntPtr)errorMapPtr, errorMap, 0, width * height);

// ネイティブメモリを解放
FlipNative.Free((IntPtr)errorMapPtr);
```

## プラットフォーム別のライブラリ名

```csharp
public static class FlipNative
{
#if UNITY_IOS && !UNITY_EDITOR
    private const string DllName = "__Internal";
#elif UNITY_ANDROID
    private const string DllName = "flip_native";
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private const string DllName = "flip_native";
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    private const string DllName = "flip_native";
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
    private const string DllName = "flip_native";
#else
    private const string DllName = "flip_native";
#endif

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void flip_evaluate(...);
}
```

## DllImport の主要オプション

| オプション | 説明 | 例 |
|-----------|------|-----|
| `CallingConvention` | 呼び出し規約 | `CallingConvention.Cdecl` |
| `EntryPoint` | 関数名が異なる場合 | `EntryPoint = "flip_evaluate_v2"` |
| `CharSet` | 文字列エンコーディング | `CharSet.Ansi` |
| `SetLastError` | エラーコード取得 | `SetLastError = true` |
| `ExactSpelling` | 名前マングリング無効化 | `ExactSpelling = true` |

## Unity での注意点

### IL2CPP 対応

```csharp
// コールバックを使用する場合は AOT.MonoPInvokeCallback が必要
public delegate void ProgressCallback(float progress);

[AOT.MonoPInvokeCallback(typeof(ProgressCallback))]
private static void OnProgress(float progress)
{
    Debug.Log($"Progress: {progress}");
}
```

### ネイティブライブラリの配置

```
Assets/
└── Plugins/
    ├── Windows/
    │   ├── x86_64/
    │   │   └── flip_native.dll
    │   └── x86/
    │       └── flip_native.dll
    ├── macOS/
    │   └── flip_native.bundle
    ├── Linux/
    │   └── libflip_native.so
    ├── Android/
    │   └── libs/
    │       ├── arm64-v8a/
    │       │   └── libflip_native.so
    │       └── armeabi-v7a/
    │           └── libflip_native.so
    └── iOS/
        └── libflip_native.a
```

## FLIP バインディングの実装例

### C++ ラッパー (flip_capi.h)

```cpp
#ifndef FLIP_CAPI_H
#define FLIP_CAPI_H

#ifdef __cplusplus
extern "C" {
#endif

#ifdef _WIN32
    #ifdef FLIP_EXPORTS
        #define FLIP_API __declspec(dllexport)
    #else
        #define FLIP_API __declspec(dllimport)
    #endif
#else
    #define FLIP_API __attribute__((visibility("default")))
#endif

/// Parameters for FLIP evaluation.
/// C-compatible version of FLIP::Parameters.
typedef struct {
    float ppd;
    float start_exposure;
    float stop_exposure;
    int num_exposures;
    const char* tonemapper;  // "aces", "reinhard", "hable"
} FlipParameters;

/// Initializes FlipParameters with default values.
FLIP_API void flip_parameters_init(FlipParameters* params);

/// Calculates PPD (pixels per degree) from viewing conditions.
FLIP_API float flip_calculate_ppd(
    float viewing_distance,
    float resolution_x,
    float monitor_width
);

/// Evaluates FLIP between a reference image and a test image.
/// Memory for error_map is allocated by this function and must be freed with flip_free().
FLIP_API void flip_evaluate(
    const float* reference,
    const float* test,
    int width,
    int height,
    int use_hdr,
    const FlipParameters* parameters,
    int apply_magma_map,
    int compute_mean_error,
    float* mean_error,
    float** error_map
);

/// Frees memory allocated by the library.
FLIP_API void flip_free(void* ptr);

#ifdef __cplusplus
}
#endif

#endif // FLIP_CAPI_H
```

### C# バインディング (FlipNative.cs)

```csharp
using System;
using System.Runtime.InteropServices;

namespace FlipBinding.CSharp
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeFlipParameters
    {
        public float Ppd;
        public float StartExposure;
        public float StopExposure;
        public int NumExposures;
        public IntPtr Tonemapper;
    }

    internal static class FlipNative
    {
        private const string DllName = "flip_native";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "flip_parameters_init")]
        public static extern void ParametersInit(ref NativeFlipParameters parameters);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "flip_calculate_ppd")]
        public static extern float CalculatePpd(
            float viewingDistance,
            float resolutionX,
            float monitorWidth
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "flip_evaluate")]
        public static extern unsafe void Evaluate(
            float* reference,
            float* test,
            int width,
            int height,
            int useHdr,
            NativeFlipParameters* parameters,
            int applyMagmaMap,
            int computeMeanError,
            float* meanError,
            float** errorMap
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "flip_free")]
        public static extern void Free(IntPtr ptr);
    }
}
```

### 高レベル API (Flip.cs)

```csharp
using System;
using System.Runtime.InteropServices;

namespace FlipBinding.CSharp
{
    public static class Flip
    {
        public const float DefaultPpd = 67.0f;

        private static readonly IntPtr AcesPtr;
        private static readonly IntPtr ReinhardPtr;
        private static readonly IntPtr HablePtr;

        static Flip()
        {
            AcesPtr = Marshal.StringToHGlobalAnsi("aces");
            ReinhardPtr = Marshal.StringToHGlobalAnsi("reinhard");
            HablePtr = Marshal.StringToHGlobalAnsi("hable");
        }

        public static unsafe FlipResult Evaluate(
            float[] reference,
            float[] test,
            int width,
            int height,
            bool useHdr = false,
            float ppd = Flip.DefaultPpd,
            Tonemapper tonemapper = Tonemapper.Aces,
            float startExposure = float.PositiveInfinity,
            float stopExposure = float.PositiveInfinity,
            int numExposures = -1)
        {
            var nativeParams = new NativeFlipParameters
            {
                Ppd = ppd,
                StartExposure = startExposure,
                StopExposure = stopExposure,
                NumExposures = numExposures,
                Tonemapper = GetTonemapperPtr(tonemapper)
            };

            float meanError = 0;
            float* errorMapPtr = null;

            fixed (float* pRef = reference)
            fixed (float* pTest = test)
            {
                FlipNative.Evaluate(
                    pRef, pTest, width, height,
                    useHdr ? 1 : 0, &nativeParams,
                    0, 1, &meanError, &errorMapPtr
                );
            }

            float[] errorMap;
            if (errorMapPtr != null)
            {
                errorMap = new float[width * height];
                Marshal.Copy((IntPtr)errorMapPtr, errorMap, 0, width * height);
                FlipNative.Free((IntPtr)errorMapPtr);
            }
            else
            {
                errorMap = Array.Empty<float>();
            }

            return new FlipResult(meanError, errorMap, width, height);
        }

        private static IntPtr GetTonemapperPtr(Tonemapper tonemapper)
        {
            return tonemapper switch
            {
                Tonemapper.Aces => AcesPtr,
                Tonemapper.Reinhard => ReinhardPtr,
                Tonemapper.Hable => HablePtr,
                _ => AcesPtr
            };
        }
    }
}
```

### 使用例

```csharp
// LDR-FLIP (デフォルト)
var result = Flip.Evaluate(reference, test, width, height);
Console.WriteLine($"Mean error: {result.MeanError}");

// カスタムPPD
var result = Flip.Evaluate(reference, test, width, height, ppd: 100f);

// HDR-FLIP
var result = Flip.Evaluate(reference, test, width, height, useHdr: true);
```

## 参考リンク

- [Platform Invoke (P/Invoke) - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)
- [Native interoperability best practices - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/best-practices)
- [Interop with Native Libraries - Mono](https://www.mono-project.com/docs/advanced/pinvoke/)
