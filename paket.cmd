@echo off

.paket\paket.bootstrapper.exe 3.20.2
if errorlevel 1 (
  exit /b %errorlevel%
)

.paket\paket.exe %*
if errorlevel 1 (
  exit /b %errorlevel%
)
