# 📜 更新履歴

更新履歴はこのファイルに記録します。

## 2026-11-24

- Windows 版を追加（`windows/`、.NET 8 / WinForms）。タイマーの仕組み（終了時刻ベースのカウントダウン、フェーズ自動切り替え）、設定（集中5〜90分／休憩1〜30分）と実績の記録は Web 版と同一仕様（保存先は `%APPDATA%\Shuchuya\data.json`）。通知音はシステムサウンドで代替。 ビルドは `windows/build.bat` / `windows/build.sh`、成果物は `windows/dist/Shuchuya.exe`（git には含めない）。
- GitHub Release「Windows版 v1.0.0」を公開（タグ `v1.0.0`、exe を添付）。
- README・CHANGELOG の書式を統一フォーマットに整理。
