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
        /// Name of the combo, the docker label is derived from it
        Label: string

        /// Tag of the mcr.microsoft.com/dotnet/sdk image, which decides the
        /// F# compiler version as well as the runtime.
        DotnetSdkTag: string

        /// Pinned exactly, independently of what the SDK bundles.
        FSharpCoreVersion: string

        /// Target framework (should be the same as the one specified by the .NET SDK but using the syntax of
        /// project files (3.1 -> netcoreapp3.1, 5.0 -> net5.0).
        TargetFramework: string
        Variants: Variant list
    }

/// Pair SDKs and FSharp.Core.
/// Mostly the versions that shipped together
let matrix =
    [
        {
            Label = "floor-3.1"
            DotnetSdkTag = "3.1"
            FSharpCoreVersion = "4.5.0"
            TargetFramework = "netcoreapp3.1"
            Variants = [ Floor45 ]
        }
        {
            // String interpolation became available.
            Label = "interp-5.0"
            DotnetSdkTag = "5.0"
            FSharpCoreVersion = "5.0.2"
            TargetFramework = "net5.0"
            Variants = [ Floor45; Interpolation50 ]
        }
        {
            // %B became available.
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
        {
            // A current toolchain held down to the package's FSharp.Core floor
            Label = "modern-sdk-old-core"
            DotnetSdkTag = "10.0"
            FSharpCoreVersion = "4.5.0"
            TargetFramework = "net10.0"
            Variants = [ Floor45 ]
        }
        {
            // The current SDK/FSharp.Core used in the repository
            Label = "current-repo"
            DotnetSdkTag = "10.0"
            FSharpCoreVersion = "10.1.302"
            TargetFramework = "net10.0"
            Variants = [ Floor45; Interpolation50; Binary60 ]
        }
    ]
