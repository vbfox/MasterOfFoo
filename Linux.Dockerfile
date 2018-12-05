FROM vbfox/fable-build:stretch-aspnet-2.2.100-node-10.14.1

WORKDIR /build

# Initialize paket packages
COPY paket.dependencies paket.lock paket.exe paket.sh ./
RUN ./paket.sh restore

# Initialize NuGet managed packages
COPY src/BlackFox.MasterOfFoo/BlackFox.MasterOfFoo.fsproj ./src/BlackFox.MasterOfFoo/
RUN dotnet restore src/BlackFox.MasterOfFoo/BlackFox.MasterOfFoo.fsproj
COPY src/TestApp/BlackFox.MasterOfFoo.TestApp.fsproj src/TestApp/paket.references ./src/TestApp/
RUN dotnet restore src/TestApp/BlackFox.MasterOfFoo.TestApp.fsproj

# Run build
COPY . ./
RUN ./build.sh CI
