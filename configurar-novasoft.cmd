@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "%~dp0scripts\configure-novasoft.ps1" %*
set "result=%errorlevel%"
if not "%result%"=="0" pause
exit /b %result%
