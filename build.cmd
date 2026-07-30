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

echo === Detecting versions ===

for /f "usebackq delims=" %%a in (`dotnet msbuild "%ROOT%src\Zaya.OCR\Zaya.OCR.csproj" -getProperty:Version -nologo -v:q`) do set IFACE=%%a
set IFACE=!IFACE: =!
if "!IFACE!"=="" set IFACE=1.0.0

for /f "tokens=1,2 delims=." %%a in ("!IFACE!") do set CHANNEL=%%a.%%b
if "!CHANNEL!"=="." set CHANNEL=1.0

for /f "usebackq delims=" %%a in (`dotnet msbuild "%ROOT%src\Zaya.OCR.Impl.OneOcr\Zaya.OCR.Impl.OneOcr.csproj" -getProperty:Version -nologo -v:q`) do set VER_ONEOCR=%%a
set VER_ONEOCR=!VER_ONEOCR: =!
if "!VER_ONEOCR!"=="" set VER_ONEOCR=!IFACE!

for /f "usebackq delims=" %%a in (`dotnet msbuild "%ROOT%src\Zaya.OCR.Impl.WindowsMediaOcr\Zaya.OCR.Impl.WindowsMediaOcr.csproj" -getProperty:Version -nologo -v:q`) do set VER_WMO=%%a
set VER_WMO=!VER_WMO: =!
if "!VER_WMO!"=="" set VER_WMO=!IFACE!

for /f "usebackq delims=" %%a in (`dotnet msbuild "%ROOT%src\Zaya.OCR.Impl.ProximityTextLayout\Zaya.OCR.Impl.ProximityTextLayout.csproj" -getProperty:Version -nologo -v:q`) do set VER_LAYOUT=%%a
set VER_LAYOUT=!VER_LAYOUT: =!
if "!VER_LAYOUT!"=="" set VER_LAYOUT=!IFACE!

REM Release tag version = max pluginVersion (semver string compare works for x.y.z same width)
set MAXVER=!VER_ONEOCR!
call :MaxVer !VER_WMO!
call :MaxVer !VER_LAYOUT!

echo   Interface=!IFACE!  UpdateChannel=!CHANNEL!  MaxPlugin=!MAXVER!
echo   OneOcr=!VER_ONEOCR!  WindowsMediaOcr=!VER_WMO!  Layout=!VER_LAYOUT!

echo === Preparing output directory ===

rmdir /s /q "%ROOT%out" 2>nul
mkdir "%ROOT%out" 2>nul

echo !MAXVER!>"%ROOT%out\version.txt"
echo !CHANNEL!>"%ROOT%out\channel.txt"
del "%ROOT%out\plugins.versions.txt" 2>nul

echo === Creating Zaya.OCR.Impl.OneOcr plugin.zip ===
call :MakeZip OneOcr ocr "%ROOT%src\Zaya.OCR.Impl.OneOcr\bin\%BUILD_CONFIG%\net8.0-windows10.0.22621.0" Zaya.OCR.Impl.OneOcr.dll Zaya.OCR.Impl.OneOcr.zip !VER_ONEOCR!
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Creating Zaya.OCR.Impl.WindowsMediaOcr plugin.zip ===
call :MakeZip WindowsMediaOcr ocr "%ROOT%src\Zaya.OCR.Impl.WindowsMediaOcr\bin\%BUILD_CONFIG%\net8.0-windows10.0.19041.0" Zaya.OCR.Impl.WindowsMediaOcr.dll Zaya.OCR.Impl.WindowsMediaOcr.zip !VER_WMO!
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo === Creating Zaya.OCR.Impl.ProximityTextLayout plugin.zip ===
call :MakeZip ProximityTextLayout textlayout "%ROOT%src\Zaya.OCR.Impl.ProximityTextLayout\bin\%BUILD_CONFIG%\net8.0" Zaya.OCR.Impl.ProximityTextLayout.dll Zaya.OCR.Impl.ProximityTextLayout.zip !VER_LAYOUT!
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

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

echo === Done: interface !IFACE! updateChannel !CHANNEL! release !MAXVER! ===
goto :eof

:MaxVer
    if "%~1" gtr "!MAXVER!" set MAXVER=%~1
    exit /b

:MakeZip
    set ZIP_ID=%~1
    set ZIP_TYPE=%~2
    set ZIP_TFM=%~3
    set ZIP_DLL=%~4
    set ZIP_NAME=%~5
    set ZIP_PVER=%~6

    rmdir /s /q "%STAGEDIR%" 2>nul
    mkdir "%STAGEDIR%"

    copy /y "%ZIP_TFM%\%ZIP_DLL%" "%STAGEDIR%"
    if %ERRORLEVEL% neq 0 (
        echo ERROR: Could not find %ZIP_DLL%
        exit /b 1
    )

    call :CopySatellites "%ZIP_TFM%" "%STAGEDIR%"

    set PLUGIN_JSON=%STAGEDIR%\plugin.json
    echo {>"%PLUGIN_JSON%"
    echo   "id": "!ZIP_ID!",>>"%PLUGIN_JSON%"
    echo   "type": "!ZIP_TYPE!",>>"%PLUGIN_JSON%"
    echo   "interface": "Zaya.OCR",>>"%PLUGIN_JSON%"
    echo   "interfaceVersion": "!IFACE!",>>"%PLUGIN_JSON%"
    echo   "pluginVersion": "!ZIP_PVER!">>"%PLUGIN_JSON%"
    echo }>>"%PLUGIN_JSON%"

    powershell -Command "Compress-Archive -Path '%STAGEDIR%\*' -DestinationPath '%ROOT%out\!ZIP_NAME!' -Force"
    echo   out\!ZIP_NAME!  pluginVersion=!ZIP_PVER!
    echo !ZIP_NAME!=!ZIP_PVER!>>"%ROOT%out\plugins.versions.txt"
    exit /b 0

:CopySatellites
    for /d %%d in ("%~1\*") do (
        if exist "%%d\*.resources.dll" (
            mkdir "%~2\%%~nxd" 2>nul
            copy /y "%%d\*" "%~2\%%~nxd\"
        )
    )
    exit /b
