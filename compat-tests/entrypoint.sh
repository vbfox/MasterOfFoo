#!/bin/sh
# Builds and runs one program variant inside an image already pinned to a
# (dotnet SDK, FSharp.Core, TFM) combination.
#
# Only the program's own stdout goes to stdout; build output is sent to stderr
# so the caller can compare stdout against a golden file byte for byte.
set -eu

VARIANT="${1:?usage: <ProgramVariant>}"

cd /compat/Programs

dotnet build -c Release --nologo \
    -p:ProgramVariant="$VARIANT" \
    -p:CompatTargetFramework="$COMPAT_TFM" \
    -p:CompatFSharpCoreVersion="$FSHARP_CORE_VERSION" \
    -p:CompatMasterOfFooVersion="$MASTEROFFOO_VERSION" \
    -o "/compat/out/$VARIANT" 1>&2

exec dotnet "/compat/out/$VARIANT/CompatProgram.dll"
