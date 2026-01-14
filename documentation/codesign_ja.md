# macOS ネイティブライブラリの署名について

macOS では、署名されていないダイナミックライブラリ（`.dylib`）を実行しようとすると、Gatekeeper によってブロックされることがあります。このドキュメントでは、開発時のアドホック署名と、配布用の Apple Developer Program 証明書による署名方法を説明します。

## 1. 開発時のアドホック署名

開発中や CI で正式な証明書が利用できない場合は、アドホック署名を使用できます。

### 手順

1. NuGet パッケージ（`.nupkg`）をダウンロード
2. 解凍（`.nupkg` は ZIP 形式）
3. 以下のコマンドでアドホック署名を実行

```bash
codesign --sign - runtimes/osx/native/libflip_native.dylib
```

`--sign -` の `-` はアドホック署名を意味します。これにより、ローカル環境での実行が可能になります。

### 署名の確認

```bash
codesign --verify --verbose runtimes/osx/native/libflip_native.dylib
```

## 2. Apple Developer Program 証明書による署名

配布用のビルドでは、Apple Developer Program の「Developer ID Application」証明書で署名することで、Gatekeeper の警告なしに実行できるようになります。

### 前提条件

GitHub Actions で署名を行うには、以下のシークレットをリポジトリまたは Organization に設定してください。

| シークレット名 | 説明 |
|---|---|
| `BUILD_CERTIFICATE_BASE64` | Developer ID Application 証明書を `.p12` 形式でエクスポートし、Base64 エンコードしたもの |
| `P12_PASSWORD` | `.p12` ファイルのエクスポート時に設定したパスワード |
| `KEYCHAIN_PASSWORD` | CI 上で作成する一時キーチェーンのパスワード（任意のランダムな文字列で可） |

> **Note:** App Store への配布ではないため、プロビジョニングプロファイルは不要です。

### 証明書の準備手順

1. **Keychain Access で証明書をエクスポート**
   - Keychain Access を開く
   - 「Developer ID Application: (チーム名)」証明書を選択
   - 右クリック → 「書き出す...」で `.p12` 形式で保存
   - パスワードを設定（これが `P12_PASSWORD` になる）

2. **Base64 エンコード**
   ```bash
   base64 -i certificate.p12 -o certificate_base64.txt
   ```

3. **GitHub Secrets に登録**
   - `BUILD_CERTIFICATE_BASE64`: `certificate_base64.txt` の内容
   - `P12_PASSWORD`: エクスポート時のパスワード
   - `KEYCHAIN_PASSWORD`: 任意のランダムな文字列

## 3. GitHub Actions ビルドワークフロー

`.github/workflows/build.yml` では、macOS ネイティブライブラリのビルドと署名を自動化しています。

### ワークフローの流れ

`build-native-osx` ジョブで以下の処理を行います：

1. **ネイティブライブラリのビルド**
   - arm64 と x86_64 それぞれのアーキテクチャでビルド
   - `lipo` コマンドで Universal Binary を作成

2. **証明書のインストール**（シークレットが設定されている場合のみ）
   - Base64 エンコードされた証明書をデコード
   - 一時キーチェーンを作成し、証明書をインポート

3. **コード署名**（シークレットが設定されている場合のみ）
   - `codesign` コマンドでライブラリに署名

### codesign コマンドの証明書指定について

ワークフローでは以下のように署名しています：

```bash
codesign --sign "Developer ID Application" --force --timestamp flip-native/libflip_native.dylib
```

`"Developer ID Application"` はプレースホルダーではありません。`codesign` コマンドは証明書名の**部分一致**で検索するため、この指定で正しく動作します。

実際の証明書の正式名は以下のような形式です：
```
Developer ID Application: チーム名 (チームID)
```

キーチェーンにインポートされた証明書の中から「Developer ID Application」を含むものが自動的に選択されます。

> **Note:** キーチェーンに複数の「Developer ID Application」証明書がある場合は、より具体的な名前（例：`"Developer ID Application: CyberAgent, Inc. (XXXXXXXXXX)"`）を指定する必要があります。CI 環境では通常1つしかインストールしないため、この指定で問題ありません。

### シークレットが未設定の場合

シークレットが設定されていない場合、署名ステップはスキップされます。これにより、フォークしたリポジトリや証明書を持たない環境でもビルドは成功します。ただし、生成されたライブラリは署名されていないため、使用時にはアドホック署名が必要です。

## 参考リンク

- [Xcode 開発用の macOS ランナーに Apple 証明書をインストールする - GitHub Docs](https://docs.github.com/ja/actions/how-tos/deploy/deploy-to-third-party-platforms/sign-xcode-applications)
- [[macOS] ライブラリをコード署名する (codesign)](https://qiita.com/Arime/items/e1df2a8c3d4c2ce75069)
