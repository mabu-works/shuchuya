# しゅうちゅうや｜集中タイマー

ポモドーロ・テクニック式の集中タイマーです。ビルド不要の静的サイトで、PWA としてインストールもできます。

🌐 公開先: [shuchuya.vercel.app](https://shuchuya.vercel.app)
📜 更新履歴: [CHANGELOG.md](CHANGELOG.md)

## ✨ 機能

- 集中（初期25分）と休憩（初期5分）を**自動で切り替え**（集中終了→休憩→次の集中…）
- 円形プログレスリング＋大きな残り時間表示
- 開始 / 一時停止（再開）/ リセット
- フェーズ終了時に Web Audio の短い通知音（集中終了と休憩終了で音程が変わります）
- ブラウザのタブタイトルにも残り時間とフェーズを表示
- 今日の完了セッション数と集中合計時間を表示
- 直近7日の集中時間を棒グラフで表示（今日は色を変えて強調）
- 設定（集中 5〜90分 / 休憩 1〜30分）は localStorage に保存

## 📝 使い方

1. `index.html` をブラウザで開く（または静的ホスティングに配置する）だけです。
2. 「開始」を押すと集中タイマーが始まります。終わると通知音が鳴り、自動で休憩に切り替わります。
3. 集中セッションが完了するたびに「今日の実績」に記録されます（日別に localStorage へ保存）。
4. 時間を変えたい場合は「設定」で分数を変更してください。待機中は即座に反映され、実行中の場合は次のフェーズから反映されます。

## 🪟 Windows 版

`windows/` フォルダに、同じ仕様の Windows アプリ（.NET 8 / WinForms）があります。集中／休憩の自動切り替え、進捗リング、今日の実績と直近7日のグラフまで Web 版と同じで、設定と実績も保存されます（保存先は Web 版の localStorage の代わりに `%APPDATA%\Shuchuya\data.json`）。オフラインで使えます。

- ダウンロード: [GitHub Releases（Windows版 v1.0.0）](https://github.com/mabu-works/shuchuya/releases/tag/v1.0.0)
- ビルド: `windows/build.bat`（Windows）または `windows/build.sh` で `windows/dist/Shuchuya.exe` を生成
- フェーズ終了の通知音は、Web 版のチャイムの代わりに Windows のシステムサウンドを使います

## 📋 技術メモ

- 外部リソース不使用（フォントはシステムの日本語フォントスタック、ライブラリ不使用）
- タイマーは終了時刻（Date.now ベース）から残り時間を算出するため、タブがバックグラウンドでも誤差が蓄積しません
- 円形プログレスは SVG の stroke-dashoffset で描画
- 通知音は Web Audio API のサイン波2音チャイム（音源ファイルなし）
- 統計データ形式：`{ "YYYY-MM-DD": { "sessions": 回数, "minutes": 集中分数 } }`
- PWA：`manifest.webmanifest` と `sw.js`（同一オリジン GET をキャッシュファーストで配信、キャッシュ名 `shuchuya-v1`）
- アイコンは Python PIL で生成した角丸＋日本語1文字の PNG（192px / 512px）

## 📁 ファイル構成

```
index.html            画面
style.css             スタイル（ダークモード対応）
app.js                タイマーと統計のロジック
sw.js                 Service Worker
manifest.webmanifest  PWA マニフェスト
icons/                アイコン（192 / 512）
```
