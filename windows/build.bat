@echo off
rem しゅうちゅうや for Windows - ビルド（Windows 用。.NET 8 SDK が必要）
rem publish して成果物パスを表示する。
setlocal
cd /d %~dp0

dotnet publish Shuchuya.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o dist
if errorlevel 1 (
  echo ビルドに失敗しました。
  exit /b 1
)

echo ===== 完了 =====
echo 成果物: %CD%\dist\Shuchuya.exe
dir dist\Shuchuya.exe
endlocal
