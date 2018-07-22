#!/bin/bash

./paket.sh restore || { exit $?; }

pushd src/BlackFox.MasterOfFoo.Build/
dotnet run $@
popd
