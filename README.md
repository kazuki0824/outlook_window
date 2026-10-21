# Outlook Recipient Confirm

Windowsの**従来版Outlook**で、通常の送信操作の直後・実際の送信前に、To・Cc・Bccの全宛先エントリーを表示するVSTOアドインです。C# / .NET Framework 4.8 / WinFormsで実装し、Visual Studio IDEやフォームデザイナーを使わずCLIでビルドする構成です。

## 現在の検証状態

**Windows CIでVSTOビルド、C#静的解析、リントと回帰テスト18件の成功を確認しています。** 配布物のPublishとアップロードを含む最新CIの結果はActionsを参照してください。インストールとOutlook実機動作は未検証です。

送信判定の本番ソースをC# 7.3としてコンパイルし、Linux / .NET 8上で回帰テスト18件が成功しています。これはアドイン全体のビルド成功や.NET Framework 4.8での動作を意味しません。詳細は[検証記録](docs/VERIFICATION.md)と[実機テスト](docs/MANUAL_TESTS.md)を参照してください。

## 動作

- `Application.ItemSend`でメール送信を捕捉します。会議依頼など`MailItem`以外は対象外です。
- To・Cc・Bcc、表示名、SMTPアドレス、配布リストの区別を、スクロール可能な表に省略せず登録します。長い値は列幅変更・水平スクロールと選択行の全文欄で確認できます。
- 「送信する」を選択した場合のみ、元の送信処理を続行します。`MailItem.Send()`で再送しません。
- キャンセル・Esc・×は送信中止。初期フォーカスと既定ボタンはキャンセルです。
- SMTPアドレス取得失敗や未解決宛先がある場合は送信を中止します。
- 承認直後に宛先を再取得し、追加・削除・To/Cc/Bcc変更・表示名変更を検知したら中止します。
- 宛先・本文を独自のサーバーへ送信せず、ファイルやログにも記録しません。Outlookのアドレス帳参照自体はExchangeとの通信を伴う場合があります。

### 「全宛先」の範囲

全件とは**作成中メールのRecipientsコレクションにある全エントリー**です。配布リストの内部メンバー、動的配布グループの展開結果、受信後の自動転送先までは列挙しません。Exchange配布リストはグループのSMTPアドレスと注意書きを表示します。SMTPアドレスを持たない個人用連絡先グループは送信を止めるため、Outlookで先に展開してください。

これは利用者の確認を補助するアドインです。アドインの無効化・クラッシュ、後続の別アドインによる宛先変更、サーバー側ルールなど、アドインの実行範囲外まで送信を強制制御するものではありません。

## Windowsでの準備

- Windows、Windows PowerShell 5.1以上。
- Visual Studio Build Tools 2022以降のMSBuild、VSTO build tools、ClickOnce build tools、.NET Framework 4.8 SDK/Targeting Pack、Office 15 PIAs。
- 実機確認時は従来版Outlook、.NET Framework 4.8、Visual Studio Tools for Office Runtimeが必要です。新しいOutlookには対応しません。

Build Toolsは、Microsoft公式ブートストラッパーをダウンロードする次のスクリプトで導入できます。管理者権限のPowerShellで実行してください。既に必要なツールがある場合は導入を省略します。

```powershell
.\scripts\install-build-tools.ps1
```

スクリプトを実行する際は、組織のPowerShell実行ポリシーに従ってください。スクリプトはポリシーを変更しません。

## CLIビルドとテスト

まず、通常ユーザーのPowerShellで開発用署名証明書を作ります。秘密鍵はそのユーザーの証明書ストア内に生成され、PFXファイルには書き出されません。Trusted Root / Trusted Publishersへの登録は行いません。

```powershell
$thumbprint = .\scripts\new-dev-certificate.ps1
.\scripts\test.ps1
.\scripts\build.ps1 -CertificateThumbprint $thumbprint -Publish
```

既存のコード署名証明書を使う場合は、CurrentUser\Myにある秘密鍵付き証明書の拇印を渡してください。`RECIPIENT_CONFIRM_CERT_THUMBPRINT`環境変数でも指定できます。証明書の作成は毎回行う必要はありません。

`build.ps1`は`vswhere`でVSTO対応の`MSBuild.exe`を検出し、通常形式の`.csproj`をビルドします。`dotnet build`単独には対応していません。必要なら`-OfficePiaPath 'C:\...\Office15'`でPIAの場所を指定できます。

出力:

| 場所 | 内容 |
|---|---|
| `src/RecipientConfirm/bin/Release/` | DLL、アプリケーションマニフェスト、`.vsto`など |
| `artifacts/publish/` | `-Publish`指定時のローカルClickOnce配布ディレクトリ |

`-Publish`はローカルへの配布用ファイル生成だけです。ネットワークへの公開やGit Pushは行いません。ビルド中のOfficeアドイン登録・解除ターゲットは無効にし、導入は次の手順に分離しています。

## 導入・削除

**Outlookを閉じてから**、署名済みのローカル配布ディレクトリを指定します。

```powershell
.\scripts\install.ps1
# 削除
.\scripts\install.ps1 -Uninstall
```

既定では`artifacts/publish/RecipientConfirm.vsto`を使用します。別の配布先を使う場合は`-Manifest`で指定してください。`.vsto`だけでなく、配下のApplication Filesも含めて配布します。

VSTOInstallerの信頼確認は表示されます。自己署名証明書の受け入れ可否は環境のポリシーによります。信頼できない発行元として拒否された場合、署名証明書・組織の配布ポリシーを確認してください。Trust Centerを無効化したり、ルート証明書を自動登録したりする処理は含めていません。組織配布では組織が承認したコード署名証明書を使用してください。

## CI

`.github/workflows/build.yml`はWindowsでリント、回帰テスト、VSTOビルド、ローカルPublishを実行する単一ジョブです。CI内だけで開発用証明書を作成して秘密鍵を削除し、開発用配布ファイルをartifactに保存します。本番署名済みリリースではありません。

| チェック | ツール・失敗条件 |
|---|---|
| C#書式 | .NET 8 SDKの`dotnet format whitespace . --folder --verify-no-changes`。`.editorconfig`と異なる書式があれば失敗 |
| C#静的解析 | `Microsoft.CodeAnalysis.NetAnalyzers` 8.0.0を両プロジェクトのビルド時に実行。警告をエラーとして扱う |
| PowerShell | `PSScriptAnalyzer` 1.23.0。全scripts配下のエラー・警告があれば失敗 |
| 回帰テスト | 送信判定テスト18件。失敗時は非ゼロ終了 |
| ビルド・配布物 | MSBuildと開発用証明書でビルドし、DLL・マニフェスト・.vstoの存在も検査 |

ローカルでのリントには.NET 8 SDKとPSScriptAnalyzerが必要です。

```powershell
Install-Module PSScriptAnalyzer -RequiredVersion 1.23.0 -Scope CurrentUser
.\scripts\lint.ps1
```

書式の修正には`dotnet format whitespace . --folder`を使います。VSTOプロジェクトを.NET SDKのMSBuildで読み込まないよう、書式チェックはフォルダー単位で実施します。意味解析を伴うC#静的解析はWindows版MSBuildが担当します。

mainへのPushとPRのイベントに対応し、同一refの古い実行を取り消します。作業ブランチへのPushとPR更新で重複実行しない設定です。Outlook実機テストはCIの対象外です。

## ソース構成

| ファイル | 役割 |
|---|---|
| `src/RecipientConfirm/ThisAddIn.cs` | VSTOエントリーポイント、ItemSendの購読、キャンセル制御 |
| `src/RecipientConfirm/RecipientReader.cs` | Outlookからの宛先取得、SMTP解決、COM参照の後始末 |
| `src/RecipientConfirm/RecipientConfirmationForm.cs` | 確認ウィンドウ |
| `src/RecipientConfirm/RecipientInfo.cs` | 宛先情報と比較 |
| `src/RecipientConfirm/SendGate.cs` | 承認前後の再確認 |
| `tests/RecipientConfirm.Tests/` | 外部テストフレームワーク不要の回帰テスト |

## Git

ソースの管理先は[kazuki0824/outlook_window](https://github.com/kazuki0824/outlook_window)です。鍵、ビルド出力、個人設定は`.gitignore`で除外しています。Windowsビルドと実機テストの未検証項目は検証記録を参照してください。

## Microsoft公式資料

- [Application.ItemSend](https://learn.microsoft.com/en-us/office/vba/api/outlook.application.itemsend)
- [受信者のSMTPアドレスの取得](https://learn.microsoft.com/en-us/office/client-developer/outlook/pia/how-to-get-the-e-mail-address-of-a-recipient)
- [Office PIAsの配置場所](https://learn.microsoft.com/en-us/visualstudio/vsto/office-primary-interop-assemblies)
- [Build Toolsのコンポーネント一覧](https://learn.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-build-tools)
- [OutlookAddInBase](https://learn.microsoft.com/en-us/dotnet/api/microsoft.office.tools.outlook.outlookaddinbase)
- [ClickOnceによるOfficeソリューションの配布](https://learn.microsoft.com/en-us/visualstudio/vsto/deploying-an-office-solution-by-using-clickonce)
