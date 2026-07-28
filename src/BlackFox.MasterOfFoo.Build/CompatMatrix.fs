module BlackFox.MasterOfFoo.Build.CompatMatrix

/// The program variants in compat-tests/Programs, each a superset of the
/// previous one. Which ones a combination can run depends on how old its
/// compiler and FSharp.Core are.
type Variant =
    /// Everything FSharp.Core 4.5.0 already supported.
    | Floor45
    /// Adds string interpolation (F# 5.0, FSharp.Core 5.0, RFC FS-1001).
    | Interpolation50
    /// Adds the %B binary specifier (F# 6.0, FSharp.Core 6.0, RFC FS-1100).
    | Binary60

module Variant =
    /// Names the Program.<name>.fs and Golden/<name>.txt files, and is the
    /// argument compat-tests/entrypoint.sh expects.
    let name variant =
        match variant with
        | Floor45 -> "Floor45"
        | Interpolation50 -> "Interpolation50"
        | Binary60 -> "Binary60"

type CompatCombo =
    {
        /// Also used as the Docker image tag, so keep it tag-safe.
        Label: string
        /// Tag of the mcr.microsoft.com/dotnet/sdk image, which decides the
        /// F# compiler version as well as the runtime.
        DotnetSdkTag: string
        /// Pinned exactly, independently of what the SDK bundles.
        FSharpCoreVersion: string
        /// Hardcoded rather than derived: the mapping from an SDK tag is not
        /// mechanical (3.1 -> netcoreapp3.1, 5.0 -> net5.0).
        TargetFramework: string
        Variants: Variant list
    }

/// Most rows are period-matched, pairing an SDK with the FSharp.Core that
/// shipped alongside it, so a row reflects a real consumer's toolchain rather
/// than an arbitrary mix.
///
/// Every row passes, so any failure is a regression and fails the build.
/// Reaching back to SDK 3.1 depends on the library being built with
/// --compressmetadata-; see compat-tests/Readme.md.
let matrix =
    [
        // Oldest FSharp.Core the package claims to support.
        {
            Label = "floor-3.1"
            DotnetSdkTag = "3.1"
            FSharpCoreVersion = "4.5.0"
            TargetFramework = "netcoreapp3.1"
            Variants = [ Floor45 ]
        }
        // Where string interpolation became available.
        {
            Label = "interp-5.0"
            DotnetSdkTag = "5.0"
            FSharpCoreVersion = "5.0.2"
            TargetFramework = "net5.0"
            Variants = [ Floor45; Interpolation50 ]
        }
        // Where %B became available.
        {
            Label = "binary-6.0"
            DotnetSdkTag = "6.0"
            FSharpCoreVersion = "6.0.7"
            TargetFramework = "net6.0"
            Variants = [ Floor45; Interpolation50; Binary60 ]
        }
        {
            Label = "lts-8.0"
            DotnetSdkTag = "8.0"
            FSharpCoreVersion = "8.0.403"
            TargetFramework = "net8.0"
            Variants = [ Floor45; Interpolation50; Binary60 ]
        }
        // The inverse of the period-matched rows: a current toolchain held down
        // to the package's FSharp.Core floor, which is what happens when an old
        // pin arrives through a transitive dependency.
        {
            Label = "modern-sdk-old-core"
            DotnetSdkTag = "10.0"
            FSharpCoreVersion = "4.5.0"
            TargetFramework = "net10.0"
            Variants = [ Floor45 ]
        }
        // What the repository itself builds and tests against.
        {
            Label = "current-repo"
            DotnetSdkTag = "10.0"
            FSharpCoreVersion = "10.1.302"
            TargetFramework = "net10.0"
            Variants = [ Floor45; Interpolation50; Binary60 ]
        }
    ]
