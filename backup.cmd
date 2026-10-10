@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "%~dp0scripts\backup.ps1"
set "launcherExitCode=%errorlevel%"
pause
exit /b %launcherExitCode%
