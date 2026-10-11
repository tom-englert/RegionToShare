@echo off
rem Step 1: WinGetUpdate.cmd <package-version> <release-tag>
rem   Creates and validates the manifests in .\winget-manifest\<package-version> for review, nothing is submitted.
rem   e.g. WinGetUpdate.cmd 1.12.6500 1.12.0
rem Step 2: WinGetUpdate.cmd <package-version> --submit
rem   Submits the reviewed manifests as a PR to winget-pkgs.
rem <package-version> is the assembly version of the released build, <release-tag> the GitHub release tag that holds RegionToShare.zip.
setlocal

set OUT=%~dp0winget-manifest\%~1
set MANIFEST=%~dp0winget-manifest\%~1\manifests\t\TomEnglert\RegionToShare\%~1

if "%~2"=="" goto :usage

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

set URL=https://github.com/tom-englert/RegionToShare/releases/download/%~2/RegionToShare.zip

if exist "%OUT%" rmdir /s /q "%OUT%"

wingetcreate update TomEnglert.RegionToShare --version %~1 --urls %URL% --out "%OUT%" || exit /b 1
winget validate --manifest "%MANIFEST%" || exit /b 1

echo.
echo Manifests created and validated in %MANIFEST%
echo Review them, optionally test with: winget install --manifest "%MANIFEST%"
echo Then submit with: %~nx0 %~1 --submit
exit /b 0

:usage
echo Usage: %~nx0 ^<package-version^> ^<release-tag^>
echo        %~nx0 ^<package-version^> --submit
exit /b 1

