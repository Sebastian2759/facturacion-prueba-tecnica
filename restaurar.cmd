@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "%~dp0scripts\restore.ps1" %*
set "launcherExitCode=%errorlevel%"
pause
exit /b %launcherExitCode%
