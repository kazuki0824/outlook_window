# 検証記録

日付: 2026-10-21

## 実施済み

- C#の全ソース7ファイルを`dotnet format whitespace . --folder --verify-no-changes`で検査し成功。書式違反を検出して非ゼロ終了することも確認し、指摘を修正済み。
- 全PowerShellスクリプトをPSScriptAnalyzer 1.23.0で検査し、エラー・警告なし。
- Microsoft.CodeAnalysis.NetAnalyzers 8.0.0を有効にして送信判定の本番ソースと回帰テストをコンパイルし、18/18成功。意図的な不正な再throwを入れた一時ソースではCA2200エラーでコンパイルに失敗することも確認。
- `RecipientInfo.cs`、`SendGate.cs`、回帰テスト`Program.cs`を、Roslyn C#コンパイラーの言語バージョン7.3でコンパイル。
- Linux / .NET 8.0.15で回帰テストを実行し、18/18成功。
- `.csproj`のXML構文、Compileエントリーが指すソースファイルの存在を確認。
- Roslynで全C#ソース7ファイルをC# 7.3として構文解析し、構文エラーなし。Outlook/VSTO参照を含む全体の意味解析・リンクを検証したものではありません。
- GitHub ActionsワークフローのYAML構文を確認。

## 未実施

| 項目 | 理由・次の確認 |
|---|---|
| Windows MSBuildによるVSTOプロジェクト全体のビルド | 実行環境がLinuxで、OfficeToolsビルドタスクと.NET Frameworkがない。Windowsでscripts/build.ps1を実行 |
| .NET Framework 4.8でのテスト実行 | Windowsでscripts/test.ps1を実行 |
| .vsto生成・署名・導入・削除 | Windowsと署名証明書、VSTO Runtimeが必要 |
| Outlook COM接続とSMTP解決 | 従来版Outlookと実メールプロファイルが必要 |
| WinFormsの表示、Esc/×、DPI、モーダル動作 | Windowsデスクトップセッションが必要 |
| GitHub Actions | 指定リポジトリをクローンし、空であることを確認。設定はローカルに実装済みだが、Push直前までの指示が継続しているため、リモート上で実行していない |

Linuxで通過したテストは送信判定の純粋C#部分に限ります。COM APIの参照解決、VSTOホストの初期化、ClickOnceの生成物、実際のメール送信を検証したものではありません。

## 回帰テストの対象

承認済みで同一宛先の場合の許可、キャンセル、空/null宛先、宛先追加・削除、To/Cc/Bccの変更、SMTPアドレス変更、表示名変更、グループ区分変更、再取得null、並べ替え、重複数、最初/2回目の取得例外、確認処理例外、空アドレス、不明な宛先種別。
