# FLIP C++ ライブラリ ドキュメント

FLIP (LDR-FLIP / HDR-FLIP) は、NVIDIA Research が開発した画像比較アルゴリズムです。人間の視覚特性を考慮した知覚的な画像差分を計算します。

## ビルド方法

### 必要条件

- CMake 3.9 以上
- C++17 対応コンパイラ
- OpenMP (オプション、マルチスレッド化)
- CUDA Toolkit (オプション、GPU 加速)

### 基本的なビルド手順

```bash
cd flip/src
mkdir build
cd build
cmake ..
cmake --build . --config Release
```

Windows では `--config Release` を指定して Release ビルドを実行します。

### CMake オプション

| オプション | デフォルト | 説明 |
|-----------|-----------|------|
| `FLIP_ENABLE_CUDA` | OFF | CUDA サポートを有効化 |
| `FLIP_LIBRARY` | OFF | ライブラリとしてビルド |

CUDA 版をビルドする場合:

```bash
cmake -DFLIP_ENABLE_CUDA=ON ..
cmake --build . --config Release
```

### ビルド成果物

- **flip** (flip-cli): CPU 版コマンドラインツール
- **flip-cuda** (flip-cuda-cli): CUDA 版コマンドラインツール

## API の使い方

### 単一ヘッダー設計

v1.3 以降、FLIP は単一ヘッダーとして提供されています。

```cpp
#define FLIP_ENABLE_CUDA    // CUDA を使用する場合のみ定義
#include "FLIP.h"
```

### 主要なクラス

#### FLIP::color3

3チャンネルカラーを表すクラスです。

```cpp
class color3 {
public:
    union {
        struct { float r, g, b; };
        struct { float x, y, z; };
        struct { float h, s, v; };
    };
    // 演算子: +, -, *, /, +=, *=, /=
};
```

#### FLIP::Parameters

計算パラメータを保持する構造体です。

```cpp
struct Parameters {
    float PPD;              // Pixels Per Degree
    float startExposure;    // 開始露出値
    float stopExposure;     // 終了露出値
    int numExposures;       // 露出数
    std::string tonemapper; // トーンマッパー名
};
```

デフォルト値:
- PPD: 67 (4K ディスプレイ、視距離 0.7m 想定)
- tonemapper: "aces"

#### FLIP::image<T>

テンプレート画像クラスです。

```cpp
template<typename T>
class image: public tensor<T> {
public:
    image(const int width, const int height);
    image(const int width, const int height, const T clearColor);

    T get(int x, int y) const;
    void set(int x, int y, T value);

    // 画像処理メソッド
    void LDR_FLIP(image<color3>& reference, image<color3>& test, float ppd);
    void expose(float level);
    void toneMap(const std::string& tm);
    void clamp(float low = 0.0f, float high = 1.0f);
};
```

### evaluate() 関数

FLIP 計算のメインエントリポイントです。4つのオーバーロードがあります。

#### 1. 完全版 (全オプション対応)

```cpp
static void evaluate(
    FLIP::image<FLIP::color3>& referenceImageInput,
    FLIP::image<FLIP::color3>& testImageInput,
    const bool useHDR,
    FLIP::Parameters& parameters,
    FLIP::image<float>& errorMapFLIPOutput,
    FLIP::image<float>& maxErrorExposureMapOutput,
    const bool returnIntermediateLDRFLIPImages,
    std::vector<FLIP::image<float>*>& intermediateLDRFLIPImages,
    const bool returnIntermediateLDRImages,
    std::vector<FLIP::image<FLIP::color3>*>& intermediateLDRImages
);
```

#### 2. 中間版 (露出マップあり)

```cpp
static void evaluate(
    FLIP::image<FLIP::color3>& referenceImageInput,
    FLIP::image<FLIP::color3>& testImageInput,
    const bool useHDR,
    FLIP::Parameters& parameters,
    FLIP::image<float>& errorMapFLIPOutput,
    FLIP::image<float>& maxErrorExposureMapOutput
);
```

#### 3. 簡易版 (エラーマップのみ)

```cpp
static void evaluate(
    FLIP::image<FLIP::color3>& referenceImageInput,
    FLIP::image<FLIP::color3>& testImageInput,
    const bool useHDR,
    FLIP::Parameters& parameters,
    FLIP::image<float>& errorMapFLIPOutput
);
```

#### 4. ポインタ版 (C 互換)

```cpp
static void evaluate(
    const float* referenceThreeChannelImage,  // インターリーブ RGB
    const float* testThreeChannelImage,       // インターリーブ RGB
    const int imageWidth,
    const int imageHeight,
    const bool useHDR,
    FLIP::Parameters& parameters,
    const bool applyMagmaMapToOutput,
    const bool computeMeanFLIPError,
    float& meanFLIPError,
    float** errorMapFLIPOutput
);
```

**パラメータ説明:**

| パラメータ | 説明 |
|-----------|------|
| `referenceImageInput` | 参照画像 (線形 RGB、[0,1]) |
| `testImageInput` | テスト画像 (線形 RGB、[0,1]) |
| `useHDR` | true: HDR-FLIP、false: LDR-FLIP |
| `parameters` | 計算パラメータ |
| `errorMapFLIPOutput` | 出力エラーマップ ([0,1] グレースケール) |
| `maxErrorExposureMapOutput` | 各ピクセルの最大エラー露出インデックス |
| `applyMagmaMapToOutput` | magma カラーマップを適用 |
| `computeMeanFLIPError` | 平均エラー値を計算 |

### 使用例

#### LDR-FLIP

```cpp
#include "FLIP.h"

void compareLDR() {
    // 画像を読み込み (線形 RGB として)
    FLIP::image<FLIP::color3> reference(width, height);
    FLIP::image<FLIP::color3> test(width, height);
    // ... 画像データを設定 ...

    // パラメータ設定
    FLIP::Parameters params;
    params.PPD = 67.0f;  // デフォルト値

    // エラーマップを計算
    FLIP::image<float> errorMap(width, height);
    FLIP::evaluate(reference, test, false, params, errorMap);

    // errorMap には [0,1] のエラー値が格納される
}
```

#### HDR-FLIP

```cpp
#include "FLIP.h"

void compareHDR() {
    FLIP::image<FLIP::color3> reference(width, height);
    FLIP::image<FLIP::color3> test(width, height);
    // ... HDR 画像データを設定 ...

    FLIP::Parameters params;
    params.PPD = 67.0f;
    params.tonemapper = "aces";
    // startExposure, stopExposure, numExposures は
    // 無限大/-1 のままにすると自動計算される

    FLIP::image<float> errorMap(width, height);
    FLIP::image<float> exposureMap(width, height);
    FLIP::evaluate(reference, test, true, params, errorMap, exposureMap);
}
```

#### C 互換ポインタ API

```cpp
#include "FLIP.h"

void compareWithPointers(
    float* refData,   // [r,g,b, r,g,b, ...] インターリーブ形式
    float* testData,
    int width,
    int height
) {
    FLIP::Parameters params;
    float meanError;
    float* errorMapData = nullptr;

    FLIP::evaluate(
        refData, testData,
        width, height,
        false,           // useHDR
        params,
        true,            // applyMagmaMapToOutput
        true,            // computeMeanFLIPError
        meanError,
        &errorMapData
    );

    // errorMapData を使用後に解放
    delete[] errorMapData;
}
```

## 処理パイプライン

### LDR-FLIP

1. 入力画像を線形 RGB [0,1] として受け取る
2. YCxCz 色空間に変換
3. 空間フィルタリング (分離可能フィルタ)
4. エラーマップを出力 ([0,1] の値)

### HDR-FLIP

1. 露出範囲を自動計算 (未指定の場合)
2. 各露出値に対してループ:
   - 露出補正を適用
   - トーンマッピング
   - [0,1] にクランプ
   - LDR-FLIP を計算
3. 各ピクセルで最大エラー値を取得
4. 露出マップも生成 (どの露出で最大エラーが発生したか)

### トーンマッパー

3種類のトーンマッパーが利用可能:

| 名前 | 説明 |
|------|------|
| `aces` | ACES フィルミックトーンマッピング (デフォルト) |
| `reinhard` | Reinhard トーンマッピング |
| `hable` | Hable/Uncharted 2 トーンマッピング |

## PPD (Pixels Per Degree) の計算

PPD は視覚的な解像度を表すパラメータです。

```cpp
float PPD = FLIP::calculatePPD(
    0.7f,    // 視距離 (メートル)
    3840.0f, // 画面幅 (ピクセル)
    0.7f     // モニター幅 (メートル)
);
// 結果: 約 67 ppd
```

計算式:
```
PPD = 視距離 × (画面解像度 / モニター幅) × (π / 180)
```

## コマンドラインツール

### 基本的な使い方

```bash
# LDR 画像の比較
flip -r reference.png -t test.png

# HDR 画像の比較
flip -r reference.exr -t test.exr

# PPD を指定
flip -r ref.png -t test.png -ppd 100

# 視聴条件から PPD を計算
flip -r ref.png -t test.png -vc 0.7 0.7 3840
```

### 主要オプション

| オプション | 説明 |
|-----------|------|
| `-r, --reference` | 参照画像 (必須) |
| `-t, --test` | テスト画像 (複数可、必須) |
| `-ppd, --pixels-per-degree` | PPD 値を直接指定 |
| `-vc, --viewing-conditions` | 視距離/モニター幅/解像度 |
| `-tm, --tone-mapper` | {aces\|hable\|reinhard} |
| `-cstart, --start-exposure` | 開始露出値 |
| `-cstop, --stop-exposure` | 終了露出値 |
| `-n, --num-exposures` | 露出数 |
| `-b, --basename` | 出力ファイル名プレフィックス |
| `--histogram` | 加重ヒストグラムを出力 |
| `--no-error-map` | エラーマップ出力をスキップ |
| `--no-exposure-map` | 露出マップ出力をスキップ (HDR) |
| `--save-ldr-images` | 中間 LDR 画像を保存 |
| `--save-ldrflip` | 中間 LDR-FLIP マップを保存 |

## 入出力フォーマット

### サポート形式

- **PNG**: LDR 画像 (8ビット)
- **EXR**: HDR 画像 (浮動小数点)

### 出力ファイル命名規則

#### LDR-FLIP

```
flip.<ref>.<test>.<ppd>ppd.ldr.png
weighted_histogram.<ref>.<test>.<ppd>ppd.ldr.py
pooled_values.<ref>.<test>.<ppd>ppd.ldr.txt
```

#### HDR-FLIP

```
flip.<ref>.<test>.<ppd>ppd.hdr.<tm>.<start>_to_<stop>.<N>.png
exposure_map.<ref>.<test>.<ppd>ppd.hdr.<tm>.<start>_to_<stop>.<N>.png
```

例: `flip.reference.test.67ppd.hdr.aces.m12.5423_to_p0.9427.14.png`
- `m12.5423`: マイナス 12.5423
- `p0.9427`: プラス 0.9427
- `14`: 14個の露出値

## 統計情報

FLIP は以下の統計値を計算できます:

- 平均 (Mean)
- 中央値 (Median)
- 加重第1四分位数 (Weighted 1st Quartile)
- 加重第3四分位数 (Weighted 3rd Quartile)
- 最小値 / 最大値

加重ヒストグラムは Python スクリプトとして出力され、numpy/matplotlib で可視化できます。

## 依存ライブラリ

FLIP は以下のライブラリをバンドルしています:

- **stb_image.h**: PNG 読み込み
- **stb_image_write.h**: PNG 書き込み
- **tinyexr.h**: EXR 読み書き

## ライセンス

BSD 3-Clause License

## 参考資料

- [FLIP GitHub リポジトリ](https://github.com/NVlabs/flip)
- [FLIP 論文](https://research.nvidia.com/publication/2020-07_FLIP)
