# 📜 更新履歴

更新履歴はこのファイルに記録します。

## 2026-11-24

- Windows 版（.NET 8 / WinForms）を `windows/` フォルダに追加。タイマーの仕組み（終了時刻ベースのカウントダウン、フェーズ自動切り替え）、設定（集中5〜90分／休憩1〜30分）と実績の記録は Web 版と同一仕様（保存先は `%APPDATA%\Shuchuya\data.json`）。通知音はシステムサウンドで代替。ビルドは `windows/build.bat` / `windows/build.sh`、成果物は `windows/dist/Shuchuya.exe`
