# 📜 更新履歴

更新履歴はこのファイルに記録します。

## 2026-11-25（ドキュメント）

- README・CHANGELOG を新ドキュメント設計（2026-11-25）に合わせて再構成（メタ表の追加、節構成と見出し形式の統一）。内容の事実は変更なし。

## 2026-11-24

### 追加

- Windows 版を追加（`windows/`、.NET 8 / WinForms）。タイマーの仕組み（終了時刻ベースのカウントダウン、フェーズ自動切り替え）、設定（集中5〜90分／休憩1〜30分）と実績の記録は Web 版と同一仕様（保存先は `%APPDATA%\Shuchuya\data.json`）。通知音はシステムサウンドで代替。 ビルドは `windows/build.bat` / `windows/build.sh`、成果物は `windows/dist/Shuchuya.exe`（git には含めない）。
- GitHub Release「Windows版 v1.0.0」を公開（タグ `v1.0.0`、exe を添付）。

### 変更

- README・CHANGELOG の書式を統一フォーマットに整理。
