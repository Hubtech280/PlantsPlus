@echo off
setlocal
cd /d "%~dp0"
if "%~1"=="" (
  echo Usage: BUILD_RELEASE.bat "C:\Path\To\Game Files"
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\build_release.ps1" -GameDirectory "%~1"
exit /b %errorlevel%
