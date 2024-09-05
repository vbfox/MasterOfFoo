#!/bin/bash

dotnet tool restore --verbosity minimal
dotnet paket $@
