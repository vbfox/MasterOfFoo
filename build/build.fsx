// include Fake libs
#r "../packages/build/FAKE/tools/FakeLib.dll"
#r "System.Xml.Linq"
#load "../paket-files/build/vbfox/FoxSharp/src/BlackFox.FakeUtils/TypedTaskDefinitionHelper.fs"
#load "./AppVeyorEx.fsx"

open System
open System.Xml.Linq
open Fake
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.Testing.Expecto
open BlackFox
open BlackFox.TypedTaskDefinitionHelper

let configuration = environVarOrDefault "configuration" "Release"

let from s = { BaseDirectory = s; Includes = []; Excludes = [] }

let rootDir = System.IO.Path.GetFullPath(__SOURCE_DIRECTORY__ </> "..")
let srcDir = rootDir </> "src"
let artifactsDir = rootDir </> "artifacts"
let librarySrcDir = srcDir </> "BlackFox.MasterOfFoo"
let libraryBinDir = artifactsDir </> "BlackFox.MasterOfFoo" </> configuration
let projects = from srcDir ++ "**/*.*proj"

/// The profile where the project is posted
let gitOwner = "vbfox"
let gitHome = "https://github.com/" + gitOwner

/// The name of the project on GitHub
let gitName = "MasterOfFoo"

/// The url for the raw files hosted
let gitRaw = environVarOrDefault "gitRaw" ("https://raw.github.com/" + gitOwner)

let inline versionPartOrZero x = if x < 0 then 0 else x

let release =
    let fromFile = LoadReleaseNotes (rootDir </> "Release Notes.md")
    if buildServer = AppVeyor then
        let appVeyorBuildVersion = int appVeyorBuildVersion
        let nugetVer = sprintf "%s-appveyor%04i" fromFile.NugetVersion appVeyorBuildVersion
        let asmVer = System.Version.Parse(fromFile.AssemblyVersion)
        let asmVer =
            System.Version(
                versionPartOrZero asmVer.Major,
                versionPartOrZero asmVer.Minor,
                versionPartOrZero asmVer.Build,
                versionPartOrZero appVeyorBuildVersion)
        ReleaseNotes.New(asmVer.ToString(), nugetVer, fromFile.Date, fromFile.Notes)
    else
        fromFile

let mutable dotnetExePath = "dotnet"

let installDotNetCore = task "InstallDotNetCore" [] {
    dotnetExePath <- DotNetCli.InstallDotNetSDK (DotNetCli.GetDotNetSDKVersionFromGlobalJson())
}

AppVeyorEx.updateBuild (fun info -> { info with Version = Some release.AssemblyVersion })

let writeVersionProps() =
    let doc =
        XDocument(
            XElement(XName.Get("Project"),
                XElement(XName.Get("PropertyGroup"),
                    XElement(XName.Get "Version", release.NugetVersion),
                    XElement(XName.Get "PackageReleaseNotes", toLines release.Notes))))
    let path = artifactsDir </> "Version.props"
    System.IO.File.WriteAllText(path, doc.ToString())

let init = task "Init" [] {
    CreateDir artifactsDir
}

let clean = task "Clean" [init] {
    let objDirs = projects |> Seq.map(fun p -> System.IO.Path.GetDirectoryName(p) </> "obj") |> List.ofSeq
    CleanDirs (artifactsDir :: objDirs)
}

let generateVersionInfo = task "GenerateVersionInfo" [init; clean.IfNeeded] {
    writeVersionProps ()
    CreateFSharpAssemblyInfo (artifactsDir </> "Version.fs") [Attribute.Version release.AssemblyVersion]
}

let build = task "Build" [installDotNetCore; generateVersionInfo; clean.IfNeeded] {
    DotNetCli.Build
      (fun p ->
           { p with
                WorkingDir = srcDir
                Configuration = configuration
                ToolPath = dotnetExePath })
}

module ExpectoDotNetCli =
    open System.Diagnostics

    let Expecto (setParams : ExpectoParams -> ExpectoParams) (assemblies : string seq) =
        let args = setParams ExpectoParams.DefaultParams
        use __ = assemblies |> separated ", " |> traceStartTaskUsing "Expecto"
        let runAssembly testAssembly =
            let argsString = sprintf "\"%s\" %s" testAssembly (string args)
            let processTimeout = TimeSpan.MaxValue // Don't set a process timeout.  The timeout is per test.
            let workingDir =
                if isNotNullOrEmpty args.WorkingDirectory
                then args.WorkingDirectory else DirectoryName testAssembly
            let exitCode =
                let info = ProcessStartInfo(dotnetExePath)
                info.WorkingDirectory <- workingDir
                info.Arguments <- argsString
                info.UseShellExecute <- false
                // Pass environment variables to the expecto console process in order to let it detect it's running on TeamCity
                // (it checks TEAMCITY_PROJECT_NAME <> null specifically).
                for name, value in environVars EnvironmentVariableTarget.Process do
                    info.EnvironmentVariables.[string name] <- string value
                use proc = Process.Start(info)
                proc.WaitForExit() // Don't set a process timeout. The timeout is per test.
                proc.ExitCode
            testAssembly, exitCode
        let res =
            assemblies
            |> Seq.map runAssembly
            |> Seq.filter( snd >> (<>) 0)
            |> Seq.toList
        match res with
        | [] -> ()
        | failedAssemblies ->
            failedAssemblies
            |> List.map (fun (testAssembly,exitCode) -> sprintf "Expecto test of assembly '%s' failed. Process finished with exit code %d." testAssembly exitCode)
            |> String.concat System.Environment.NewLine
            |> FailedTestsException |> raise

let runTests = task "RunTests" [build] {
    let nunitPath = rootDir </> @"packages" </> "NUnit.ConsoleRunner" </> "tools" </> "nunit3-console.exe"
    let testAssemblies = artifactsDir </> "bin" </> "*.Tests" </> configuration </> "*.Tests.dll"
    let testResults = artifactsDir </> "TestResults.xml"

    [artifactsDir </> "BlackFox.MasterOfFoo.Tests" </> configuration </> "netcoreapp2.0" </> "BlackFox.MasterOfFoo.Tests.dll"]
        |> ExpectoDotNetCli.Expecto (fun p ->
            { p with
                FailOnFocusedTests = true
            })
}

let nupkgDir = artifactsDir </> "BlackFox.MasterOfFoo" </> configuration

let nuget = task "NuGet" [build] {
    DotNetCli.Pack
      (fun p ->
           { p with
                WorkingDir = librarySrcDir
                Configuration = configuration
                ToolPath = dotnetExePath })
    let nupkg =
        nupkgDir
            </> (sprintf "BlackFox.MasterOfFoo.%s.nupkg" release.NugetVersion)

    AppVeyor.PushArtifacts [nupkg]
}

let publishNuget = task "PublishNuget" [nuget] {
    let key =
        match environVarOrNone "nuget-key" with
        | Some(key) -> key
        | None -> getUserPassword "NuGet key: "

    Paket.Push <| fun p ->  { p with WorkingDir = nupkgDir; ApiKey = key }
}

let zipFile = artifactsDir </> (sprintf "BlackFox.MasterOfFoo-%s.zip" release.NugetVersion)

let zip = task "Zip" [build] {
    from libraryBinDir
        ++ "**/*.dll"
        ++ "**/*.xml"
        -- "**/FSharp.Core.*"
        |> Zip libraryBinDir zipFile
    AppVeyor.PushArtifacts [zipFile]
}

#load "../paket-files/build/fsharp/FAKE/modules/Octokit/Octokit.fsx"

let gitHubRelease = task "GitHubRelease" [zip] {
    let user =
        match getBuildParam "github-user" with
        | s when not (String.IsNullOrWhiteSpace s) -> s
        | _ -> getUserInput "GitHub Username: "
    let pw =
        match getBuildParam "github-pw" with
        | s when not (String.IsNullOrWhiteSpace s) -> s
        | _ -> getUserPassword "GitHub Password or Token: "

    // release on github
    Octokit.createClient user pw
    |> Octokit.createDraft gitOwner gitName release.NugetVersion (release.SemVer.PreRelease <> None) release.Notes
    |> Octokit.uploadFile zipFile
    |> Octokit.releaseDraft
    |> Async.RunSynchronously
}

let gitRelease = task "GitRelease" [] {
    let remote =
        Git.CommandHelper.getGitResult "" "remote -v"
        |> Seq.filter (fun (s: string) -> s.EndsWith("(push)"))
        |> Seq.tryFind (fun (s: string) -> s.Contains(gitOwner + "/" + gitName))
        |> function None -> gitHome + "/" + gitName | Some (s: string) -> s.Split().[0]

    Git.Branches.tag "" release.NugetVersion
    Git.Branches.pushTag "" remote release.NugetVersion
}

let defaultTask = EmptyTask "Default" [runTests]
EmptyTask "Release" [clean; gitRelease; gitHubRelease; publishNuget]
EmptyTask "CI" [clean; runTests; zip; nuget]

RunTaskOrDefault defaultTask
