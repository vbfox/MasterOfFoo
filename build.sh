#!/bin/bash

./paket.sh restore || { exit $?; }

dotnet run --project src/BlackFox.MasterOfFoo.Build/BlackFox.MasterOfFoo.Build.fsproj $@
