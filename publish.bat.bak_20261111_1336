@echo off
echo ==============================================
echo Building and Publishing IPScan...
echo ==============================================

dotnet publish IPScan.csproj /p:PublishProfile=FolderProfile-SelfContained
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Self-contained publish failed.
    exit /b %ERRORLEVEL%
)

echo.
dotnet publish IPScan.csproj /p:PublishProfile=FolderProfile-FrameworkDependent
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Framework-dependent publish failed.
    exit /b %ERRORLEVEL%
)

echo.
echo ==============================================
echo Publish complete!
echo Output locations:
echo   - Self-contained:       publish\self-contained\IPScan.exe
echo   - Framework-dependent:  publish\framework-dependent\IPScan.exe
echo ==============================================

