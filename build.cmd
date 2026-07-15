@echo off
setlocal enabledelayedexpansion

set ROOT=%~dp0
set STAGEDIR=%TEMP%\Zaya.OCR\staging

echo === Building Zaya.OCR.Impl.OneOcr ===

dotnet build "%ROOT%src\Zaya.OCR.Impl.OneOcr\Zaya.OCR.Impl.OneOcr.csproj" -c Release
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

set TFM_DIR=%ROOT%src\Zaya.OCR.Impl.OneOcr\bin\Release\net8.0-windows10.0.22621.0

echo === Preparing plugin structure ===

rmdir /s /q "%STAGEDIR%" 2>nul
mkdir "%STAGEDIR%"

copy /y "%TFM_DIR%\Zaya.OCR.Impl.OneOcr.dll" "%STAGEDIR%"
if %ERRORLEVEL% neq 0 (
    echo ERROR: Could not find DLL
    exit /b 1
)

echo === Detecting versions ===

for /f "tokens=*" %%a in ('findstr /i "<Version>" "%ROOT%src\Zaya.OCR\Zaya.OCR.csproj"') do set INF_LINE=%%a
set INF_LINE=!INF_LINE:^<Version^>=!
set INF_LINE=!INF_LINE:^</Version^>=!
set INF_MAJOR=!INF_LINE:~0,1!
if "!INF_MAJOR!"=="" set INF_MAJOR=1

for /f "tokens=*" %%a in ('findstr /i "<Version>" "%ROOT%src\Zaya.OCR.Impl.OneOcr\Zaya.OCR.Impl.OneOcr.csproj"') do set IMPL_LINE=%%a
set IMPL_LINE=!IMPL_LINE:^<Version^>=!
set IMPL_LINE=!IMPL_LINE:^</Version^>=!
if "!IMPL_LINE!"=="" set IMPL_LINE=1.0.0

echo === Generating plugin.json ===

set PLUGIN_JSON=%STAGEDIR%\plugin.json

echo {>"%PLUGIN_JSON%"
echo   "id": "OneOcr",>>"%PLUGIN_JSON%"
echo   "type": "ocr",>>"%PLUGIN_JSON%"
echo   "interface": "Zaya.OCR",>>"%PLUGIN_JSON%"
echo   "interfaceVersion": "!INF_MAJOR!.0.0",>>"%PLUGIN_JSON%"
echo   "pluginVersion": "!IMPL_LINE!">>"%PLUGIN_JSON%"
echo }>>"%PLUGIN_JSON%"

set PLUGIN_ZIP=Zaya.OCR.Impl.OneOcr-!IMPL_LINE!.zip
echo === Creating plugin.zip ===

rmdir /s /q "%ROOT%out" 2>nul
mkdir "%ROOT%out" 2>nul
powershell -Command "Compress-Archive -Path '%STAGEDIR%\*' -DestinationPath '%ROOT%out\%PLUGIN_ZIP%' -Force"
echo   out\%PLUGIN_ZIP%

echo === Packing NuGet packages ===

dotnet pack "%ROOT%src\Zaya.OCR\Zaya.OCR.csproj" -c Release -o "%ROOT%out" --no-build
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Cleaning up ===

rmdir /s /q "%STAGEDIR%" 2>nul

echo === Done: version !IMPL_LINE! ===
