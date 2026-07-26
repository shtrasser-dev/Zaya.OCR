@echo off
setlocal enabledelayedexpansion

set ROOT=%~dp0
set STAGEDIR=%TEMP%\Zaya.OCR\staging

echo === Building Zaya.OCR.Impl.OneOcr ===

dotnet build "%ROOT%src\Zaya.OCR.Impl.OneOcr\Zaya.OCR.Impl.OneOcr.csproj" -c Release
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Building Zaya.OCR.Impl.ProximityTextLayout ===

dotnet build "%ROOT%src\Zaya.OCR.Impl.ProximityTextLayout\Zaya.OCR.Impl.ProximityTextLayout.csproj" -c Release
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

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

set LAYOUT_VERSION=!IMPL_LINE!

echo === Preparing output directory ===

rmdir /s /q "%ROOT%out" 2>nul
mkdir "%ROOT%out" 2>nul

echo === Creating Zaya.OCR.Impl.OneOcr plugin.zip ===

rmdir /s /q "%STAGEDIR%" 2>nul
mkdir "%STAGEDIR%"

set OOCR_TFM=%ROOT%src\Zaya.OCR.Impl.OneOcr\bin\Release\net8.0-windows10.0.22621.0

copy /y "%OOCR_TFM%\Zaya.OCR.Impl.OneOcr.dll" "%STAGEDIR%"
if %ERRORLEVEL% neq 0 (
    echo ERROR: Could not find OneOcr DLL
    exit /b 1
)

call :CopySatellites "%OOCR_TFM%" "%STAGEDIR%"

set PLUGIN_JSON=%STAGEDIR%\plugin.json

echo {>"%PLUGIN_JSON%"
echo   "id": "OneOcr",>>"%PLUGIN_JSON%"
echo   "type": "ocr",>>"%PLUGIN_JSON%"
echo   "interface": "Zaya.OCR",>>"%PLUGIN_JSON%"
echo   "interfaceVersion": "!INF_MAJOR!.0.0",>>"%PLUGIN_JSON%"
echo   "pluginVersion": "!IMPL_LINE!">>"%PLUGIN_JSON%"
echo }>>"%PLUGIN_JSON%"

powershell -Command "Compress-Archive -Path '%STAGEDIR%\*' -DestinationPath '%ROOT%out\Zaya.OCR.Impl.OneOcr-!IMPL_LINE!.zip' -Force"
echo   out\Zaya.OCR.Impl.OneOcr-!IMPL_LINE!.zip

echo === Creating Zaya.OCR.Impl.ProximityTextLayout plugin.zip ===

rmdir /s /q "%STAGEDIR%" 2>nul
mkdir "%STAGEDIR%"

set LAYOUT_TFM=%ROOT%src\Zaya.OCR.Impl.ProximityTextLayout\bin\Release\net8.0

copy /y "%LAYOUT_TFM%\Zaya.OCR.Impl.ProximityTextLayout.dll" "%STAGEDIR%"
if %ERRORLEVEL% neq 0 (
    echo ERROR: Could not find ProximityTextLayout DLL
    exit /b 1
)

call :CopySatellites "%LAYOUT_TFM%" "%STAGEDIR%"

set PLUGIN_JSON=%STAGEDIR%\plugin.json

echo {>"%PLUGIN_JSON%"
echo   "id": "ProximityTextLayout",>>"%PLUGIN_JSON%"
echo   "type": "textlayout",>>"%PLUGIN_JSON%"
echo   "interface": "Zaya.OCR",>>"%PLUGIN_JSON%"
echo   "interfaceVersion": "!INF_MAJOR!.0.0",>>"%PLUGIN_JSON%"
echo   "pluginVersion": "!LAYOUT_VERSION!">>"%PLUGIN_JSON%"
echo }>>"%PLUGIN_JSON%"

powershell -Command "Compress-Archive -Path '%STAGEDIR%\*' -DestinationPath '%ROOT%out\Zaya.OCR.Impl.ProximityTextLayout-!LAYOUT_VERSION!.zip' -Force"
echo   out\Zaya.OCR.Impl.ProximityTextLayout-!LAYOUT_VERSION!.zip

echo === Packing NuGet packages ===

dotnet pack "%ROOT%src\Zaya.OCR.Impl.OneOcr\Zaya.OCR.Impl.OneOcr.csproj" -c Release -o "%ROOT%out" --no-build
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

dotnet pack "%ROOT%src\Zaya.OCR.Impl.ProximityTextLayout\Zaya.OCR.Impl.ProximityTextLayout.csproj" -c Release -o "%ROOT%out" --no-build
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

dotnet pack "%ROOT%src\Zaya.OCR\Zaya.OCR.csproj" -c Release -o "%ROOT%out" --no-build
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Cleaning up ===

rmdir /s /q "%STAGEDIR%" 2>nul

echo === Done: version !IMPL_LINE! ===
goto :eof

:CopySatellites
    for /d %%d in ("%~1\*") do (
        if exist "%%d\*.resources.dll" (
            mkdir "%~2\%%~nxd" 2>nul
            copy /y "%%d\*" "%~2\%%~nxd\"
        )
    )
    exit /b
