@echo off
setlocal

set PAKET_VERSION=3.20.2
.paket\paket.bootstrapper.exe -s %PAKET_VERSION%
if errorlevel 1 (
  exit /b %errorlevel%
)

.paket\paket.exe %*
if errorlevel 1 (
  exit /b %errorlevel%
)
