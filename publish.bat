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
echo Preparing GitHub Release assets and SHA256SUMS.txt...
if not exist "publish\release" mkdir "publish\release"
copy /Y "publish\self-contained\IPScan.exe" "publish\release\IPScan-win-x64-standalone.exe" >nul
copy /Y "publish\framework-dependent\IPScan.exe" "publish\release\IPScan-win-x64-lite.exe" >nul
powershell -NoProfile -Command "$files = @('IPScan-win-x64-lite.exe', 'IPScan-win-x64-standalone.exe'); $lines = foreach ($f in $files) { $h = (Get-FileHash -Path ('publish\release\' + $f) -Algorithm SHA256).Hash.ToLowerInvariant(); \"$h  $f\" }; $lines | Out-File -FilePath 'publish\release\SHA256SUMS.txt' -Encoding ascii"

echo.
echo ==============================================
echo Publish complete!
echo Output locations:
echo   - Self-contained:       publish\self-contained\IPScan.exe
echo   - Framework-dependent:  publish\framework-dependent\IPScan.exe
echo   - GitHub Release pack:  publish\release\ (lite, standalone, SHA256SUMS.txt)
echo ==============================================

