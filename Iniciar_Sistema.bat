@echo off
color 0B
echo Iniciando verificacao do ambiente Project Clock...
powershell.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0\RunProjectClock.ps1"
if %errorlevel% neq 0 (
    pause
)
