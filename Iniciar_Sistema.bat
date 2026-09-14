@echo off
:: Garante que o diretorio atual seja a pasta do projeto
cd /d "%~dp0"

color 0B
title Iniciador do Project Clock
echo Iniciando verificacao do ambiente Project Clock...
powershell.exe -ExecutionPolicy Bypass -NoProfile -File ".\RunProjectClock.ps1"
echo.
echo O Script de inicializacao foi finalizado (ou ocorreu um erro que impediu o PowerShell de rodar).
pause
