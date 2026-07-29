@echo off
setlocal enabledelayedexpansion

set ROOT=%~dp0
set STAGEDIR=%TEMP%\Zaya.OCR\staging

if "%CI%"=="true" (
    set BUILD_CONFIG=Release
) else (
    set BUILD_CONFIG=Debug
)

echo === Building Zaya.OCR.Impl.OneOcr (%BUILD_CONFIG%) ===

dotnet build "%ROOT%src\Zaya.OCR.Impl.OneOcr\Zaya.OCR.Impl.OneOcr.csproj" -c %BUILD_CONFIG%
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Building Zaya.OCR.Impl.WindowsMediaOcr (%BUILD_CONFIG%) ===

dotnet build "%ROOT%src\Zaya.OCR.Impl.WindowsMediaOcr\Zaya.OCR.Impl.WindowsMediaOcr.csproj" -c %BUILD_CONFIG%
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Building Zaya.OCR.Impl.ProximityTextLayout (%BUILD_CONFIG%) ===

dotnet build "%ROOT%src\Zaya.OCR.Impl.ProximityTextLayout\Zaya.OCR.Impl.ProximityTextLayout.csproj" -c %BUILD_CONFIG%
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Detecting version ===

for /f "usebackq delims=" %%a in (`dotnet msbuild "%ROOT%src\Zaya.OCR\Zaya.OCR.csproj" -getProperty:Version -nologo -v:q`) do set VER=%%a
set VER=!VER: =!
if "!VER!"=="" set VER=0.4.0

for /f "tokens=1,2,3 delims=." %%a in ("!VER!") do (
    set VER_MAJOR=%%a
    set VER_MINOR=%%b
    set VER_PATCH=%%c
)
set CHANNEL=!VER_MAJOR!.!VER_MINOR!
echo   Version=!VER!  Channel=!CHANNEL!

echo === Preparing output directory ===

rmdir /s /q "%ROOT%out" 2>nul
mkdir "%ROOT%out" 2>nul

echo !VER!>"%ROOT%out\version.txt"
echo !CHANNEL!>"%ROOT%out\channel.txt"

echo === Creating Zaya.OCR.Impl.OneOcr plugin.zip ===

rmdir /s /q "%STAGEDIR%" 2>nul
mkdir "%STAGEDIR%"

set OOCR_TFM=%ROOT%src\Zaya.OCR.Impl.OneOcr\bin\%BUILD_CONFIG%\net8.0-windows10.0.22621.0

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
echo   "interfaceVersion": "!VER!",>>"%PLUGIN_JSON%"
echo   "pluginVersion": "!VER!",>>"%PLUGIN_JSON%"
echo   "primitivesChannel": "!CHANNEL!">>"%PLUGIN_JSON%"
echo }>>"%PLUGIN_JSON%"

REM Stable asset name (no version in filename) for host updater.
powershell -Command "Compress-Archive -Path '%STAGEDIR%\*' -DestinationPath '%ROOT%out\Zaya.OCR.Impl.OneOcr.zip' -Force"
echo   out\Zaya.OCR.Impl.OneOcr.zip

echo === Creating Zaya.OCR.Impl.WindowsMediaOcr plugin.zip ===

rmdir /s /q "%STAGEDIR%" 2>nul
mkdir "%STAGEDIR%"

set WMO_TFM=%ROOT%src\Zaya.OCR.Impl.WindowsMediaOcr\bin\%BUILD_CONFIG%\net8.0-windows10.0.19041.0

copy /y "%WMO_TFM%\Zaya.OCR.Impl.WindowsMediaOcr.dll" "%STAGEDIR%"
if %ERRORLEVEL% neq 0 (
    echo ERROR: Could not find WindowsMediaOcr DLL
    exit /b 1
)

call :CopySatellites "%WMO_TFM%" "%STAGEDIR%"

set PLUGIN_JSON=%STAGEDIR%\plugin.json

echo {>"%PLUGIN_JSON%"
echo   "id": "WindowsMediaOcr",>>"%PLUGIN_JSON%"
echo   "type": "ocr",>>"%PLUGIN_JSON%"
echo   "interface": "Zaya.OCR",>>"%PLUGIN_JSON%"
echo   "interfaceVersion": "!VER!",>>"%PLUGIN_JSON%"
echo   "pluginVersion": "!VER!",>>"%PLUGIN_JSON%"
echo   "primitivesChannel": "!CHANNEL!">>"%PLUGIN_JSON%"
echo }>>"%PLUGIN_JSON%"

powershell -Command "Compress-Archive -Path '%STAGEDIR%\*' -DestinationPath '%ROOT%out\Zaya.OCR.Impl.WindowsMediaOcr.zip' -Force"
echo   out\Zaya.OCR.Impl.WindowsMediaOcr.zip

echo === Creating Zaya.OCR.Impl.ProximityTextLayout plugin.zip ===

rmdir /s /q "%STAGEDIR%" 2>nul
mkdir "%STAGEDIR%"

set LAYOUT_TFM=%ROOT%src\Zaya.OCR.Impl.ProximityTextLayout\bin\%BUILD_CONFIG%\net8.0

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
echo   "interfaceVersion": "!VER!",>>"%PLUGIN_JSON%"
echo   "pluginVersion": "!VER!",>>"%PLUGIN_JSON%"
echo   "primitivesChannel": "!CHANNEL!">>"%PLUGIN_JSON%"
echo }>>"%PLUGIN_JSON%"

powershell -Command "Compress-Archive -Path '%STAGEDIR%\*' -DestinationPath '%ROOT%out\Zaya.OCR.Impl.ProximityTextLayout.zip' -Force"
echo   out\Zaya.OCR.Impl.ProximityTextLayout.zip

echo === Packing NuGet packages ===

dotnet pack "%ROOT%src\Zaya.OCR.Impl.OneOcr\Zaya.OCR.Impl.OneOcr.csproj" -c %BUILD_CONFIG% -o "%ROOT%out" --no-build
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

dotnet pack "%ROOT%src\Zaya.OCR.Impl.WindowsMediaOcr\Zaya.OCR.Impl.WindowsMediaOcr.csproj" -c %BUILD_CONFIG% -o "%ROOT%out" --no-build
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

dotnet pack "%ROOT%src\Zaya.OCR.Impl.ProximityTextLayout\Zaya.OCR.Impl.ProximityTextLayout.csproj" -c %BUILD_CONFIG% -o "%ROOT%out" --no-build
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

dotnet pack "%ROOT%src\Zaya.OCR\Zaya.OCR.csproj" -c %BUILD_CONFIG% -o "%ROOT%out" --no-build
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Cleaning up ===

rmdir /s /q "%STAGEDIR%" 2>nul

echo === Done: version !VER! channel !CHANNEL! ===
goto :eof

:CopySatellites
    for /d %%d in ("%~1\*") do (
        if exist "%%d\*.resources.dll" (
            mkdir "%~2\%%~nxd" 2>nul
            copy /y "%%d\*" "%~2\%%~nxd\"
        )
    )
    exit /b
