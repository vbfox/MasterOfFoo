/// Runs the compat-tests matrix: one Docker image per (SDK, FSharp.Core, TFM)
/// combination, one container run per program variant, each compared against a
/// golden file.
module BlackFox.MasterOfFoo.Build.DockerCompat

open System
open System.IO

open Fake.Core
open Fake.IO.FileSystemOperators

open BlackFox.MasterOfFoo.Build.CompatMatrix

/// Old SDK images (3.1, 5.0, 6.0) only exist for amd64, so everything is pinned
/// to it to keep local runs and CI identical. On an arm64 host this means
/// emulation.
let private platform = "linux/amd64"

let private imageTag (combo: CompatCombo) = "masteroffoo-compat:" + combo.Label

type Outcome =
    | Pass
    /// Failed, with a short reason and the detail worth keeping in the report.
    | Fail of reason: string * details: string

type VariantResult =
    {
        Combo: CompatCombo
        Variant: string
        Outcome: Outcome
    }

    member this.Failed =
        match this.Outcome with
        | Pass -> false
        | Fail _ -> true

let private run (workingDir: string) (exe: string) (args: string list) =
    CreateProcess.fromRawCommand exe args
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.redirectOutput
    |> Proc.run

/// Keeps only the tail of a command's output: enough to see the compiler error
/// that explains a failure without burying the report.
let private lastLines count (text: string) =
    if String.IsNullOrWhiteSpace text then
        "(no output)"
    else
        let lines = text.Replace("\r\n", "\n").TrimEnd().Split('\n')
        lines
        |> Array.skip (max 0 (lines.Length - count))
        |> String.concat "\n"

let private normalize (text: string) = text.Replace("\r\n", "\n").TrimEnd()

let private buildImage (rootDir: string) (nupkgDir: string) (packageVersion: string) (combo: CompatCombo) =
    Trace.tracefn "Building image %s (SDK %s, FSharp.Core %s, %s)" (imageTag combo) combo.DotnetSdkTag combo.FSharpCoreVersion combo.TargetFramework

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
                imageTag combo
                "."
            ]

    if result.ExitCode = 0 then
        Ok()
    else
        Error(lastLines 25 (result.Result.Output + result.Result.Error))

let private runVariant (rootDir: string) (goldenDir: string) (combo: CompatCombo) (variant: string) =
    let result = run rootDir "docker" [ "run"; "--rm"; "--platform"; platform; imageTag combo; variant ]

    if result.ExitCode <> 0 then
        // The container builds the program before running it, so a failure is
        // usually the compiler rejecting the variant or the package.
        let output = result.Result.Error + result.Result.Output
        let reason = if output.Contains "error FS" then "compile failed" else "run failed"
        Fail(reason, lastLines 25 output)
    else
        let goldenFile = goldenDir </> (variant + ".txt")

        if not (File.Exists goldenFile) then
            Fail("no golden file", goldenFile)
        else
            let expected = normalize (File.ReadAllText goldenFile)
            let actual = normalize result.Result.Output

            if expected = actual then
                Pass
            else
                let expectedLines = expected.Split('\n')
                let actualLines = actual.Split('\n')

                let diff =
                    Seq.init (max expectedLines.Length actualLines.Length) id
                    |> Seq.choose (fun i ->
                        let e = if i < expectedLines.Length then expectedLines.[i] else "(missing)"
                        let a = if i < actualLines.Length then actualLines.[i] else "(missing)"
                        if e = a then None else Some(sprintf "line %d:\n  expected: %s\n  actual:   %s" (i + 1) e a))
                    |> Seq.truncate 10
                    |> String.concat "\n"

                Fail("output differs", diff)

let private writeReport (reportFile: string) (results: VariantResult list) =
    let lines = ResizeArray<string>()
    lines.Add "# Compatibility matrix report"
    lines.Add ""
    lines.Add "| Combination | SDK | FSharp.Core | TFM | Variant | Result |"
    lines.Add "|---|---|---|---|---|---|"

    for r in results do
        let status =
            match r.Outcome with
            | Pass -> "pass"
            | Fail(reason, _) -> "**FAIL: " + reason + "**"

        lines.Add(
            sprintf
                "| %s | %s | %s | %s | %s | %s |"
                r.Combo.Label
                r.Combo.DotnetSdkTag
                r.Combo.FSharpCoreVersion
                r.Combo.TargetFramework
                r.Variant
                status
        )

    let failures = results |> List.filter (fun r -> r.Failed)

    if not failures.IsEmpty then
        lines.Add ""
        lines.Add "## Failure details"

        for r in failures do
            match r.Outcome with
            | Pass -> ()
            | Fail(reason, details) ->
                lines.Add ""
                lines.Add(sprintf "### %s / %s — %s" r.Combo.Label r.Variant reason)
                lines.Add ""
                lines.Add "```"
                lines.Add details
                lines.Add "```"

    Directory.CreateDirectory(Path.GetDirectoryName reportFile) |> ignore
    File.WriteAllText(reportFile, String.concat "\n" lines + "\n")

/// `nupkgDir` is relative to `rootDir`: it is passed into the Docker build,
/// where paths are resolved against the build context.
let runMatrix (rootDir: string) (nupkgDir: string) (packageVersion: string) (reportFile: string) (matrix: CompatCombo list) =
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
                | Ok() ->
                    for variant in combo.Variants do
                        let outcome = runVariant rootDir goldenDir combo variant

                        match outcome with
                        | Pass -> Trace.tracefn "  %s / %s: pass" combo.Label variant
                        | Fail(reason, _) -> Trace.traceImportantfn "  %s / %s: FAIL (%s)" combo.Label variant reason

                        {
                            Combo = combo
                            Variant = variant
                            Outcome = outcome
                        }
        ]

    writeReport reportFile results

    Trace.tracefn ""
    Trace.tracefn "Compatibility matrix summary"

    for r in results do
        let status =
            match r.Outcome with
            | Pass -> "pass"
            | Fail(reason, _) -> "FAIL - " + reason

        Trace.tracefn "  %-22s %-16s %s" r.Combo.Label r.Variant status

    Trace.tracefn ""
    Trace.tracefn "Report written to %s" reportFile

    let failures = results |> List.filter (fun r -> r.Failed)

    if not failures.IsEmpty then
        for r in failures do
            Trace.traceErrorfn "Combination failed: %s / %s" r.Combo.Label r.Variant

        failwithf "%d compatibility check(s) failed; see %s for details" failures.Length reportFile
