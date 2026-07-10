@echo off
echo === TimeTracker Build ===
echo.

echo [1/2] Publishing...
dotnet publish src/TimeTracker.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish

if %ERRORLEVEL% neq 0 (
    echo Build FAILED!
    pause
    exit /b 1
)

echo.
echo [2/2] Done!
echo Executable: publish\TimeTracker.exe
echo.
pause
