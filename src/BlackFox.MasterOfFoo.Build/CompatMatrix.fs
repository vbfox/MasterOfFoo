module BlackFox.MasterOfFoo.Build.CompatMatrix

/// The program variants in compat-tests/Programs, each a superset of the
/// previous one. Which ones a combination can run depends on how old its
/// compiler and FSharp.Core are.
module Variants =
    /// Everything FSharp.Core 4.5.0 already supported.
    let floor45 = "Floor45"
    /// Adds string interpolation (F# 5.0, FSharp.Core 5.0, RFC FS-1001).
    let interpolation50 = "Interpolation50"
    /// Adds the %B binary specifier (F# 6.0, FSharp.Core 6.0, RFC FS-1100).
    let binary60 = "Binary60"

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
        Variants: string list
        /// Only mandatory combinations can fail the build. The others are
        /// reported so regressions stay visible while they get fixed.
        IsMandatory: bool
    }

/// Most rows are period-matched, pairing an SDK with the FSharp.Core that
/// shipped alongside it, so a row reflects a real consumer's toolchain rather
/// than an arbitrary mix.
///
/// The SDK 3.1, 5.0 and 6.0 rows currently fail: a package built by the .NET 10
/// SDK carries its F# metadata only in the compressed form F# 8 introduced, so
/// compilers older than that see no F# metadata in the assembly. See
/// compat-tests/Readme.md.
let matrix =
    [
        // Oldest FSharp.Core the package claims to support.
        {
            Label = "floor-3.1"
            DotnetSdkTag = "3.1"
            FSharpCoreVersion = "4.5.0"
            TargetFramework = "netcoreapp3.1"
            Variants = [ Variants.floor45 ]
            IsMandatory = false
        }
        // Where string interpolation became available.
        {
            Label = "interp-5.0"
            DotnetSdkTag = "5.0"
            FSharpCoreVersion = "5.0.2"
            TargetFramework = "net5.0"
            Variants = [ Variants.floor45; Variants.interpolation50 ]
            IsMandatory = false
        }
        // Where %B became available.
        {
            Label = "binary-6.0"
            DotnetSdkTag = "6.0"
            FSharpCoreVersion = "6.0.7"
            TargetFramework = "net6.0"
            Variants = [ Variants.floor45; Variants.interpolation50; Variants.binary60 ]
            IsMandatory = false
        }
        {
            Label = "lts-8.0"
            DotnetSdkTag = "8.0"
            FSharpCoreVersion = "8.0.403"
            TargetFramework = "net8.0"
            Variants = [ Variants.floor45; Variants.interpolation50; Variants.binary60 ]
            IsMandatory = false
        }
        // The inverse of the period-matched rows: a current toolchain held down
        // to the package's FSharp.Core floor, which is what happens when an old
        // pin arrives through a transitive dependency.
        {
            Label = "modern-sdk-old-core"
            DotnetSdkTag = "10.0"
            FSharpCoreVersion = "4.5.0"
            TargetFramework = "net10.0"
            Variants = [ Variants.floor45 ]
            IsMandatory = false
        }
        // What the repository itself builds and tests against.
        {
            Label = "current-repo"
            DotnetSdkTag = "10.0"
            FSharpCoreVersion = "10.1.302"
            TargetFramework = "net10.0"
            Variants = [ Variants.floor45; Variants.interpolation50; Variants.binary60 ]
            IsMandatory = true
        }
    ]
