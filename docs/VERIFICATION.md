# 検証記録

日付: 2026-10-21

## 2026-10-30: CLI簡素化とdotnet互換性

[PR #1](https://github.com/kazuki0824/outlook_window/pull/1)で、独自の環境探索、自動インストール、ビルドラッパー、テストラッパーの4ファイル・144行を削除しました。単体テストはSDK形式として`dotnet run`でビルド・実行し、VSTO本体はMSBuildのPublishターゲットを直接一度だけ呼びます。テストケースとアドイン本体のソースは変更していません。

[Windows / .NET SDK 10.0.403での実測](https://github.com/kazuki0824/outlook_window/actions/runs/40982783820):

| 実行条件 | 結果 |
|---|---|
| 通常の`dotnet build` | SDKにOfficeToolsターゲットがなくMSB4019 |
| インストール済みVSTOターゲットとPIAを指定 | VSTO参照DLLを解決できずCS0234 |
| 実在するVSTO参照DLLのHintPathも指定 | DLLコンパイル成功（警告0）。その後`VerifyClickOnceSigningSettings`がMSB4018で失敗。`Microsoft.VisualStudio.Tools.Applications.Hosting, Version=10.0.0.0`をロードできない |

結論は、現構成を`dotnet`へそのまま置き換えてVSTO配布物生成まで完了することはできなかった、という範囲です。追加の依存解決やタスクの移植まで不可能だとする検証ではありません。簡素化後の通常CIは[PRのChecks](https://github.com/kazuki0824/outlook_window/pull/1/checks)を参照してください。互換性調査ジョブは失敗ログを保存するため成功終了する仕組みであり、製品ビルドの合格を意味しません。調査用ジョブと一時targetsは最終構成には残していません。

## 2026-10-30: VSTOタスクの依存解決後の追加検証

[PR #2](https://github.com/kazuki0824/outlook_window/pull/2)で、Windows 2022 / .NET SDK 10系の`dotnet publish`を追加検証しました。

[依存DLL補完後の実行ログ](https://github.com/kazuki0824/outlook_window/actions/runs/41045075845/job/124027061276):

1. Visual Studioにインストール済みのOfficeToolsターゲットとPIA、VSTO参照DLLのHintPathを指定。
2. 使い捨ての診断用runnerで、GACにある`Microsoft.VisualStudio.Tools.*`と`Microsoft.Office.Tools.*`のDLLをVSTO BuildTasks DLLの隣へコピー。製品配布物には追加していません。
3. DLLコンパイルと`VerifyClickOnceSigningSettings`を通過し、`FindRibbons`でMSB4018。
4. 内部例外は`System.PlatformNotSupportedException: ReflectionOnly loading is not supported on this platform.`。呼び出し元は`FindRibbons.FindRibbonTypes`、未対応APIは`System.Reflection.Assembly.ReflectionOnlyLoadFrom`。

このAPIは[Microsoftの仕様](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assembly.reflectiononlyloadfrom?view=net-10.0)でも.NET Core / .NET 5以降では常に`PlatformNotSupportedException`となります。依存DLLの探索設定だけで解消する問題ではありません。`net48`をコンパイルできないという意味ではなく、既存VSTOタスクの実行ランタイムの非互換です。

[同じ条件でCLR4タスクホストも指定した実行](https://github.com/kazuki0824/outlook_window/actions/runs/41045451763/job/124028383660)では、同じ`FindRibbons`のAPI非互換を再現しました。続けて`UsingTask`を`Runtime="CLR4"`、`TaskFactory="TaskHostFactory"`に上書きすると、MSB4216で失敗しました。SDKディレクトリの`MSBuild.exe`をホストとして要求しますが、その実行ファイルは.NET SDKにはありません。Microsoftの[UsingTask仕様](https://learn.microsoft.com/en-us/visualstudio/msbuild/usingtask-element-msbuild)でもdotnet版MSBuildでCLR2/CLR4タスクを実行できないと明記されています。外部の.NET Framework版ホストを別途使う場合は「dotnetだけ」という構成にはなりません。

既存VSTOタスクを変更せずにPublishする経路は.NET Framework版MSBuildを維持します。タスクの移植・置換や独自プロセスによる代行まで不可能とする結論ではありません。診断用のDLLコピー、参照上書きtargets、ジョブは最終構成には残しません。

## 2026-10-21: 実施済み

- [Windows CI初回実行](https://github.com/kazuki0824/outlook_window/actions/runs/39593714310)で、VSTO本体とテストプロジェクトのビルド・静的解析が警告0、エラー0。リントと.NET Framework 4.8での18件のテストも成功。Publish自体は成功したが、出力先にPublishUrlを渡していたため想定先への生成物チェックで失敗し、PublishDirへ修正。
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
| インストール・削除 | 実Windows端末とVSTO Runtimeが必要。CIでの生成・署名と区別して扱う |
| Outlook COM接続とSMTP解決 | 従来版Outlookと実メールプロファイルが必要 |
| WinFormsの表示、Esc/×、DPI、モーダル動作 | Windowsデスクトップセッションが必要 |

Linuxで通過したテストは送信判定の純粋C#部分に限ります。COM APIの参照解決、VSTOホストの初期化、ClickOnceの生成物、実際のメール送信を検証したものではありません。

## 単体テストの対象

承認済みで同一宛先の場合の許可、キャンセル、空/null宛先、宛先追加・削除、To/Cc/Bccの変更、SMTPアドレス変更、表示名変更、グループ区分変更、再取得null、並べ替え、重複数、最初/2回目の取得例外、確認処理例外、空アドレス、不明な宛先種別。
