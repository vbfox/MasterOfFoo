@echo off

paket.exe restore -s
if errorlevel 1 (
  exit /b %errorlevel%
)

pushd src\BlackFox.MasterOfFoo.Build\
dotnet run %*
set _errorlevel=%errorlevel%
popd

exit /b %_errorlevel%
