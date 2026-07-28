# Runs the compat programs against one (dotnet SDK, FSharp.Core, TFM)
# combination, consuming the library as a NuGet package rather than a project
# reference. Built from the repository root so the packed nupkg is in context.
ARG DOTNET_SDK_TAG=8.0
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_TAG}

ARG FSHARP_CORE_VERSION
ARG MASTEROFFOO_VERSION
ARG COMPAT_TFM
ARG NUPKG_DIR=artifacts/BlackFox.MasterOfFoo/Release

ENV FSHARP_CORE_VERSION=${FSHARP_CORE_VERSION} \
    MASTEROFFOO_VERSION=${MASTEROFFOO_VERSION} \
    COMPAT_TFM=${COMPAT_TFM} \
    DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

WORKDIR /compat

COPY compat-tests/Programs/ /compat/Programs/
COPY compat-tests/nuget.config /compat/Programs/nuget.config
COPY ${NUPKG_DIR}/*.nupkg /nupkgs/
COPY compat-tests/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# The package graph depends on the TFM and the two pinned versions but not on
# the program variant, so restoring here makes every later `docker run` a fast,
# offline no-op restore.
RUN cd /compat/Programs && dotnet restore \
    -p:CompatTargetFramework="$COMPAT_TFM" \
    -p:CompatFSharpCoreVersion="$FSHARP_CORE_VERSION" \
    -p:CompatMasterOfFooVersion="$MASTEROFFOO_VERSION"

ENTRYPOINT ["/entrypoint.sh"]
CMD ["Floor45"]
