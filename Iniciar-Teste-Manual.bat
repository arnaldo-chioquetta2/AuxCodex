@echo off
setlocal
set "BASE=%~dp0"
set "EXE=%BASE%bin\Debug\net9.0-windows\AuxCodex.exe"
set "CONFIG=%BASE%TesteManual"
if not exist "%EXE%" (
    echo Executavel nao encontrado: "%EXE%"
    echo Compile a solucao antes de iniciar a homologacao.
    pause
    exit /b 1
)
start "AuxCodex - Teste Manual" "%EXE%" --config-dir "%CONFIG%"
endlocal
