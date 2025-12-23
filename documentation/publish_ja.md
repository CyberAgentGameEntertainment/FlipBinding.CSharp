# NuGet パッケージ公開ワークフロー

このドキュメントでは、NuGet パッケージのバージョニングと nuget.org への公開ワークフローについて説明します。

## バージョニング

パッケージのバージョンは `FlipBinding.CSharp/FlipBinding.CSharp.csproj` の `PackageVersion` で管理します。

```xml
<PackageVersion>1.0.0-preview.8</PackageVersion>
```

バージョン形式は [Semantic Versioning 2.0.0](https://semver.org/lang/ja/) に従います。

- リリース版: `1.0.0`, `1.2.3`
- プレリリース版: `1.0.0-preview.1`, `1.0.0-alpha`, `1.0.0-beta.2`

## 公開ワークフロー

パッケージの nuget.org への公開は、`v` ではじまるタグをリポジトリにプッシュすると自動的に行われます。

### 概要

`.github/workflows/build.yml` がパッケージのビルドと公開を担当します。

- mainへのpush および PRのとき: build-native-* → test-native-* → build-nupkg
- tag pushのとき: build-native-* → test-native-* → build-nupkg → **publish-nupkg**

### publish-nupkg ジョブ

タグプッシュ時のみ実行される公開ジョブです。

#### バリデーション

タグのバージョン（`v` を除いた部分）が nupkg ファイル名から抽出したバージョンと一致することを確認します。

- タグ: `v1.0.0` → バージョン: `1.0.0`
- nupkg: `FlipBinding.CSharp.1.0.0.nupkg` → バージョン: `1.0.0`

#### 公開先

- **テスト環境（暫定）**: https://int.nugettest.org/
- **本番環境（将来）**: https://nuget.org/

#### 認証

[NuGet Trusted Publishing](https://learn.microsoft.com/ja-jp/nuget/nuget-org/trusted-publishing) を使用します。

## 前提条件

### GitHub シークレット

以下のシークレットをリポジトリまたは Organization に設定してください。

| シークレット名 | 説明 |
|--------------|------|
| `CT_NUGET_USER` | nuget.org のユーザー ID |

### nuget.org での設定

[Trusted Publishing](https://learn.microsoft.com/ja-jp/nuget/nuget-org/trusted-publishing) を使用してパッケージを公開します。

nuget.org で以下の設定が必要です:

1. **Trusted Publishers の設定**
   - nuget.org にサインイン
   - パッケージの管理画面で Trusted Publishers を設定
   - GitHub リポジトリ情報を登録:
     - Repository owner: `CyberAgentGameEntertainment`
     - Repository name: `FlipBinding.CSharp`
     - Workflow: `build.yml`

2. **パッケージ ID の予約**（推奨）
   - パッケージ ID `FlipBinding.CSharp` を予約して、他者による同名パッケージの公開を防止

## 参考リンク

- [NuGet Trusted Publishing](https://learn.microsoft.com/ja-jp/nuget/nuget-org/trusted-publishing)
- [dotnet nuget push コマンド](https://learn.microsoft.com/ja-jp/dotnet/core/tools/dotnet-nuget-push)
- [Semantic Versioning](https://semver.org/lang/ja/)
