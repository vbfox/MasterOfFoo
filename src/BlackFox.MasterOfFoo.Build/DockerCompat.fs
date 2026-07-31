/// Runs the compat-tests matrix: one Docker image per (SDK, FSharp.Core, TFM)
/// combination, one container run per program variant, each compared against a
/// golden file.
module BlackFox.MasterOfFoo.Build.DockerCompat

open System
open System.IO
open System.Text.RegularExpressions

open Fake.Core
open Fake.IO.FileSystemOperators

open BlackFox.MasterOfFoo.Build.CompatMatrix

/// Old SDK images (3.1, 5.0, 6.0) only exist for amd64
let private platform = "linux/amd64"

type Outcome =
    | Pass
    /// Failed, with a short reason and the detail worth keeping in the report.
    | Fail of reason: string * details: string

type VariantResult =
    {
        Combo: CompatCombo
        Variant: Variant
        Outcome: Outcome
    }

    member this.Failed =
        match this.Outcome with
        | Pass -> false
        | Fail _ -> true

let private toValidDockerTag (s: string) =
    let mutable result = s

    let startWithInvalidChar = Regex "^[^A-Za-z0-9_]"
    result <- startWithInvalidChar.Replace(result, "_")

    let isInvalidChar = Regex "[^A-Za-z0-9_.-]"
    result <- isInvalidChar.Replace(result, "_")

    if result.Length = 0 then
        result <- "_"
    else if result.Length > 128 then
        result <- result.Substring(0, 128)    
    
    result

let private imageLabel (combo: CompatCombo) = "masteroffoo-compat:" + (toValidDockerTag combo.Label)

let private run (workingDir: string) (exe: string) (args: string list) =
    CreateProcess.fromRawCommand exe args
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.redirectOutput
    |> Proc.run

/// Keeps only the tail of a command's output
let private lastLogLines count (text: string) =
    if String.IsNullOrWhiteSpace text then
        "(no output)"
    else
        let lines = text.Replace("\r\n", "\n").TrimEnd().Split('\n')
        lines
        |> Array.skip (max 0 (lines.Length - count))
        |> String.concat "\n"

let private normalizeLineEndings (text: string) = text.Replace("\r\n", "\n").TrimEnd()

let private buildImage (rootDir: string) (nupkgDir: string) (packageVersion: string) (combo: CompatCombo) =
    let label = imageLabel combo
    Trace.tracefn "Building image %s (SDK %s, FSharp.Core %s, %s)" label combo.DotnetSdkTag combo.FSharpCoreVersion combo.TargetFramework

    let result =
        run
            rootDir
            "docker"
            [
                "build"
                "--platform"
                platform
                "-f"
                "compat-tests/Dockerfile"
                "--build-arg"
                "DOTNET_SDK_TAG=" + combo.DotnetSdkTag
                "--build-arg"
                "FSHARP_CORE_VERSION=" + combo.FSharpCoreVersion
                "--build-arg"
                "COMPAT_TFM=" + combo.TargetFramework
                "--build-arg"
                "MASTEROFFOO_VERSION=" + packageVersion
                "--build-arg"
                "NUPKG_DIR=" + nupkgDir
                "-t"
                label
                "."
            ]

    if result.ExitCode = 0 then
        Ok label
    else
        Error(lastLogLines 25 (result.Result.Output + result.Result.Error))

let private runVariant (rootDir: string) (goldenDir: string) (combo: CompatCombo) (variant: Variant) (label: string) =
    let name = Variant.name variant
    let result = run rootDir "docker" [ "run"; "--rm"; "--platform"; platform; label; name ]

    if result.ExitCode <> 0 then
        // The container builds the program before running it, so a failure is
        // usually the compiler rejecting the variant or the package.
        let output = result.Result.Error + result.Result.Output
        let reason = if output.Contains "error FS" then "compile failed" else "run failed"
        Fail(reason, lastLogLines 25 output)
    else
        let goldenFile = goldenDir </> (name + ".txt")

        if not (File.Exists goldenFile) then
            Fail("no golden file", goldenFile)
        else
            let expected = normalizeLineEndings (File.ReadAllText goldenFile)
            let actual = normalizeLineEndings result.Result.Output

            if expected = actual then
                Pass
            else
                let expectedLines = expected.Split '\n'
                let actualLines = actual.Split '\n'

                let diff =
                    Seq.init (max expectedLines.Length actualLines.Length) id
                    |> Seq.choose (fun i ->
                        let e = if i < expectedLines.Length then expectedLines.[i] else sprintf "line %d: (missing)" (i + 1)
                        let a = if i < actualLines.Length then actualLines.[i] else sprintf "line %d: (missing)" (i + 1)
                        if e = a then None else Some(sprintf "line %d:\n  expected: %s\n  actual:   %s" (i + 1) e a))
                    |> Seq.truncate 10
                    |> String.concat "\n"

                Fail("output differs", diff)

let private printResultMatrix (results: VariantResult list) =
    Trace.tracefn "Compatibility matrix summary"

    for r in results do
        let status =
            match r.Outcome with
            | Pass -> "pass"
            | Fail(reason, _) -> "FAIL - " + reason

        Trace.tracefn
            "  %-22s %-16s %-9s %-16s %s"
            r.Combo.Label
            (Variant.name r.Variant)
            ("SDK " + r.Combo.DotnetSdkTag)
            ("FSC " + r.Combo.FSharpCoreVersion)
            status

let private printFailureDetails (results: VariantResult list) =
    for r in results do
        match r.Outcome with
        | Pass -> ()
        | Fail(reason, details) ->
            Trace.traceErrorfn ""
            Trace.traceErrorfn "%s / %s failed: %s" r.Combo.Label (Variant.name r.Variant) reason
            Trace.traceErrorfn "%s" details

/// `nupkgDir` is relative to `rootDir`: it is passed into the Docker build,
/// where paths are resolved against the build context.
let runMatrix (rootDir: string) (nupkgDir: string) (packageVersion: string) (matrix: CompatCombo list) =
    let goldenDir = rootDir </> "compat-tests" </> "Golden"

    let results =
        [
            for combo in matrix do
                match buildImage rootDir nupkgDir packageVersion combo with
                | Error details ->
                    // Nothing can run, so every variant of this combination
                    // fails for the same reason.
                    for variant in combo.Variants do
                        {
                            Combo = combo
                            Variant = variant
                            Outcome = Fail("image build failed", details)
                        }
                | Ok imageLabel ->
                    for variant in combo.Variants do
                        let outcome = runVariant rootDir goldenDir combo variant imageLabel

                        match outcome with
                        | Pass -> Trace.tracefn "  %s / %s: pass" combo.Label (Variant.name variant)
                        | Fail(reason, _) ->
                            Trace.traceImportantfn "  %s / %s: FAIL (%s)" combo.Label (Variant.name variant) reason

                        {
                            Combo = combo
                            Variant = variant
                            Outcome = outcome
                        }
        ]

    Trace.tracefn ""
    printResultMatrix results

    Trace.tracefn ""
    printFailureDetails results

    let failureCount = results |> List.sumBy (fun r -> if r.Failed then 1 else 0)
    if failureCount > 0 then
        failwithf "%d compatibility check(s) failed" failureCount
