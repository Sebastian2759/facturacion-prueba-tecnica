@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "%~dp0scripts\start.ps1" %*
set "launcherExitCode=%errorlevel%"
if errorlevel 1 pause
exit /b %launcherExitCode%
