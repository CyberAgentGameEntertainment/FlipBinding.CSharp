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

### .NET Standard 2.0 / Unity (DllImport)

```csharp
using System;
using System.Runtime.InteropServices;

namespace FlipBinding.CSharp
{
    internal static class FlipNative
    {
        private const string DllName = "flip_native";

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
            float* peakError,
            float** errorMap
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "flip_free")]
        public static extern void Free(IntPtr ptr);
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
        float* peak_error,
        float** error_map  // ライブラリが確保
    );

    // ライブラリが確保したメモリを解放
    FLIP_API void flip_free(void* ptr);
}
```

```csharp
// C#
float meanError = 0;
float peakError = 0;
float* errorMapPtr = null;

FlipNative.Evaluate(
    pRef, pTest, width, height,
    useHdr ? 1 : 0, &nativeParams,
    0, 1, &meanError, &peakError, &errorMapPtr
);

// ネイティブメモリからマネージド配列にコピー
float[] errorMap = new float[width * height];
Marshal.Copy((IntPtr)errorMapPtr, errorMap, 0, width * height);

// ネイティブメモリを解放
FlipNative.Free((IntPtr)errorMapPtr);
```

## プラットフォーム別のライブラリ名

iOS（IL2CPP）ではネイティブライブラリが静的リンクされるため、`__Internal` を指定します。

```csharp
internal static class FlipNative
{
#if UNITY_IOS && !UNITY_EDITOR
    private const string DllName = "__Internal";
#else
    private const string DllName = "flip_native";
#endif
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

NuGet パッケージの `runtimes/` に同梱したネイティブライブラリを使います。
導入手順は [README.md の Installation](../README.md#installation) を参照してください。

## FLIP バインディングの実装

実装は以下のファイルを参照してください。

- C ラッパー: [flip-native/flip_capi.h](../flip-native/flip_capi.h)、[flip-native/flip_capi.cpp](../flip-native/flip_capi.cpp)
- P/Invoke 宣言: [FlipBinding.CSharp/FlipNative.cs](../FlipBinding.CSharp/FlipNative.cs)
- 高レベル API: [FlipBinding.CSharp/Flip.cs](../FlipBinding.CSharp/Flip.cs)
- 使用例: [README.md の Usage](../README.md#usage)

## 参考リンク

- [Platform Invoke (P/Invoke) - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)
- [Native interoperability best practices - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/best-practices)
- [Interop with Native Libraries - Mono](https://www.mono-project.com/docs/advanced/pinvoke/)
