# Compatibility tests

Checks that the packaged library behaves like `sprintf` across .NET SDK and
FSharp.Core versions. Unlike the Expecto suite, which uses a project reference
and whatever FSharp.Core the repository resolves, these tests consume the built
`.nupkg` from inside a container whose SDK and FSharp.Core versions are pinned,
so a run reflects what a real consumer's toolchain produces.

## How it works

`Programs/` holds one console program per feature era. Each case formats a value
twice — once through the library, once through the FSharp.Core of that
combination — and prints the result, marking any difference with `MISMATCH`. So
FSharp.Core itself is the oracle, and `Golden/*.txt` pins the output so that
silent drift also shows up.

| Variant | Adds | Needs |
|---|---|---|
| `Floor45` | every specifier, flag, width/precision and star form | FSharp.Core 4.5.0 |
| `Interpolation50` | string interpolation, typed and `.NET`-style holes | F# 5.0, FSharp.Core 5.0 |
| `Binary60` | the `%B` binary specifier | F# 6.0, FSharp.Core 6.0 |

Each variant includes the previous ones, so running `Binary60` covers everything.

One image is built per combination and reused for all of its variants: the
variant is chosen when the container runs, not when the image is built. Every
`docker` call pins `linux/amd64`, because the older SDK images are only
published for that architecture — on an arm64 host those rows run emulated and
are noticeably slower.

The combinations live in
[`CompatMatrix.fs`](../src/BlackFox.MasterOfFoo.Build/CompatMatrix.fs) and the
orchestration in
[`DockerCompat.fs`](../src/BlackFox.MasterOfFoo.Build/DockerCompat.fs).

## Known result: SDK 3.1, 5.0 and 6.0 cannot consume the package

Those rows currently fail to compile with `The namespace or module 'BlackFox' is
not defined`, even though NuGet resolves and passes the assembly to the
compiler. The package is built with the .NET 10 SDK, whose F# compiler embeds
its metadata only as `FSharpSignatureCompressedData` — a form introduced in F#
8. Older compilers look for the uncompressed `FSharpSignatureData` resource,
find nothing, and so see no F# metadata in the assembly at all.

This is a property of the compiler that builds the package, not of the pinned
FSharp.Core version, and it sets the real floor for consumers at SDK 8.0
regardless of the `FSharp.Core >= 4.5.0` dependency the package declares. The
rows are kept as tolerated failures so the boundary stays measured rather than
assumed: if the package ever gets built by an older compiler, they should start
passing on their own.

`modern-sdk-old-core` passing shows the FSharp.Core floor itself is fine — a
current SDK held down to FSharp.Core 4.5.0 works.

## Running

```bash
./build.sh DockerCompat
```

This packs the library first, then walks the matrix and writes
`artifacts/CompatReport.md`. Only combinations marked `IsMandatory` fail the
build; the rest are reported so that known gaps stay visible without blocking.

To try a single combination by hand, after `./build.sh NuGet`:

```bash
docker build --platform linux/amd64 -f compat-tests/Dockerfile --build-arg DOTNET_SDK_TAG=10.0 --build-arg FSHARP_CORE_VERSION=10.1.302 --build-arg COMPAT_TFM=net10.0 --build-arg MASTEROFFOO_VERSION=2.1.1 -t masteroffoo-compat:current .
```

```bash
docker run --rm --platform linux/amd64 masteroffoo-compat:current Binary60
```

## Updating the golden files

One golden file per variant is shared by every combination: FSharp.Core 8.0.403
and 10.1.302 produce byte-identical output today, including for `%A`. If a
future FSharp.Core changes how it renders something, that shows up as an
`output differs` failure and the files may need to be split per version.

Regenerate them from the mandatory combination only, and review the diff — a
change here means the library's output changed:

```bash
for v in Floor45 Interpolation50 Binary60; do docker run --rm --platform linux/amd64 masteroffoo-compat:current "$v" > "compat-tests/Golden/$v.txt"; done
```
