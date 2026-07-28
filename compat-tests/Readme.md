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

## Why the library is built with `--compressmetadata-`

Every row of the matrix passes, but only because
[BlackFox.MasterOfFoo.fsproj](../src/BlackFox.MasterOfFoo/BlackFox.MasterOfFoo.fsproj)
passes `--compressmetadata-` to the compiler. Without it, SDK 3.1, 5.0 and 6.0
cannot consume the package at all, failing with `The namespace or module
'BlackFox' is not defined` even though NuGet resolves the assembly and passes it
to the compiler.

F# stores its own metadata — types, modules, inlined code — in a manifest
resource beside the IL, and since F# 8 the compiler compresses it by default,
naming the resource `FSharpSignatureCompressedData`. Older compilers only look
for the uncompressed `FSharpSignatureData`, find nothing, and conclude the
assembly contains no F# metadata. The flag emits the uncompressed resource,
which every compiler since F# 4.5 understands.

This is a property of the compiler that builds the package, not of the
FSharp.Core version pinned by the consumer, so it cannot be worked around from
the consumer's side. It costs about 29 KB of assembly size (84 KB to 113 KB).

Shipping several DLLs would not have helped: NuGet picks a DLL by the consumer's
target framework, not by their compiler version, and every target framework in
one build is compiled by the same `fsc` anyway.

## Running

```bash
./build.sh DockerCompat
```

This packs the library first, then walks the matrix and writes
`artifacts/CompatReport.md`. Every combination currently passes and is marked
`IsMandatory`, so any of them breaking fails the build. A combination that is
known not to work yet can be set to `IsMandatory = false` to be reported
without blocking.

To try a single combination by hand, after `./build.sh NuGet`:

```bash
docker build --platform linux/amd64 -f compat-tests/Dockerfile --build-arg DOTNET_SDK_TAG=10.0 --build-arg FSHARP_CORE_VERSION=10.1.302 --build-arg COMPAT_TFM=net10.0 --build-arg MASTEROFFOO_VERSION=2.1.1 -t masteroffoo-compat:current .
```

```bash
docker run --rm --platform linux/amd64 masteroffoo-compat:current Binary60
```

## Updating the golden files

One golden file per variant is shared by every combination: FSharp.Core 4.5.0
through 10.1.302 produce byte-identical output today, including for `%A`. If a
future FSharp.Core changes how it renders something, that shows up as an
`output differs` failure and the files may need to be split per version.

Regenerate them from the mandatory combination only, and review the diff — a
change here means the library's output changed:

```bash
for v in Floor45 Interpolation50 Binary60; do docker run --rm --platform linux/amd64 masteroffoo-compat:current "$v" > "compat-tests/Golden/$v.txt"; done
```
