@echo off
rem Step 1: WinGetUpdate.cmd <package-version> [<release-tag>]  (tag defaults to the version)
rem   Creates and validates the manifests in .\winget-manifest\<package-version> for review, nothing is submitted.
rem   e.g. WinGetUpdate.cmd 1.12.6500 1.12.0
rem Step 2: WinGetUpdate.cmd <package-version> --submit
rem   Submits the reviewed manifests as a PR to winget-pkgs.
rem <package-version> is the assembly version of the released build, <release-tag> the GitHub release tag that holds RegionToShare.exe.
setlocal

set VERSION=%~1
set OUT=%~dp0winget-manifest\%VERSION%
set MANIFEST=%OUT%\manifests\t\TomEnglert\RegionToShare\%VERSION%

if "%VERSION%"=="" goto :usage

where wingetcreate >nul 2>&1
if errorlevel 1 (
    echo wingetcreate not found, installing...
    winget install --id Microsoft.WingetCreate -e --accept-package-agreements --accept-source-agreements || exit /b 1
    echo Please restart this script in a new terminal so wingetcreate is on the PATH.
    exit /b 1
)

if /i "%~2"=="--submit" (
    if not exist "%MANIFEST%" (
        echo No manifests found in %MANIFEST%, run step 1 first.
        exit /b 1
    )
    wingetcreate submit "%MANIFEST%"
    exit /b
)

set TAG=%~2
if "%TAG%"=="" set TAG=%VERSION%

if exist "%OUT%" rmdir /s /q "%OUT%"


# set URL=https://github.com/tom-englert/RegionToShare/releases/download/%TAG%/RegionToShare.zip
# wingetcreate update TomEnglert.RegionToShare --version %VERSION% --urls %URL% --out "%OUT%" || exit /b 1

pwsh -NoProfile -File "%~dp0WinGetCreateManifest.ps1" -Version %VERSION% -Tag %TAG% -OutDir "%MANIFEST%" || exit /b 1

winget validate --manifest "%MANIFEST%" || exit /b 1

echo.
echo Manifests created and validated in %MANIFEST%
echo Review them, optionally test with: winget install --manifest "%MANIFEST%"
echo Then submit with: %~nx0 %VERSION% --submit
exit /b 0

:usage
echo Usage: %~nx0 ^<package-version^> [^<release-tag^>]
echo        %~nx0 ^<package-version^> --submit
exit /b 1

