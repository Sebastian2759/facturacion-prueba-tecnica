@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "%~dp0scripts\diagnose.ps1"
set "launcherExitCode=%errorlevel%"
pause
exit /b %launcherExitCode%
