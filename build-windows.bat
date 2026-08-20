@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul

echo =============================================
echo   Xenostrap - Build Single File (Windows)
echo =============================================
echo.

:: --- Config ---
set PROJECT=src\Xenostrap.App\Xenostrap.csproj
set OUTPUT=PublishedBuilds\Windows
set RUNTIME=win-x64
set CONFIG=Release

:: --- Clean old output ---
echo [1/3] Cleaning old output...
if exist "%OUTPUT%" (
    rmdir /s /q "%OUTPUT%"
    echo       Cleaned: %OUTPUT%
) else (
    echo       Nothing to clean.
)
echo.

:: --- Publish ---
echo [2/3] Publishing (SingleFile, %RUNTIME%, %CONFIG%)...
echo.
dotnet publish "%PROJECT%" ^
    -c %CONFIG% ^
    -r %RUNTIME% ^
    --self-contained false ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%OUTPUT%"

if errorlevel 1 (
    echo.
    echo [ERROR] Build FAILED! Check errors above.
    pause
    exit /b 1
)

echo.
echo [3/3] Done!
echo.
echo Output folder:
echo   %~dp0%OUTPUT%
echo.

:: --- Show output file info ---
for %%F in ("%OUTPUT%\Xenostrap.exe") do (
    echo File: %%~nxF
    echo Size: %%~zF bytes
    set /a SIZE_MB=%%~zF / 1048576
    echo      (~!SIZE_MB! MB^)
)

echo.
echo =============================================
echo   Build completed successfully!
echo =============================================
pause
