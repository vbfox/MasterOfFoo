module BlackFox.MasterOfFoo.Build.Tasks

open Fake.Core
open Fake.BuildServer
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing
open Fake.IO.Globbing.Operators
open Fake.IO.FileSystemOperators
open Fake.Tools

open BlackFox
open BlackFox.TypedTaskDefinitionHelper
open BlackFox.CommandLine
open System.Xml.Linq
open Fake.Core

let createAndGetDefault () =
    let configuration = Environment.environVarOrDefault "configuration" "Release"
    let fakeConfiguration =
        match configuration.Trim().ToLowerInvariant() with
        | "release" -> DotNet.BuildConfiguration.Release
        | "debug" -> DotNet.BuildConfiguration.Debug
        | _ -> DotNet.BuildConfiguration.Custom configuration

    let from s =
        { LazyGlobbingPattern.BaseDirectory = s; Includes = []; Excludes = [] }
        :> IGlobbingPattern

    let rootDir = System.IO.Path.GetFullPath(__SOURCE_DIRECTORY__ </> ".." </> "..")
    let srcDir = rootDir </> "src"
    let artifactsDir = rootDir </> "artifacts"
    let libraryProjectFile = srcDir </> "BlackFox.MasterOfFoo" </> "BlackFox.MasterOfFoo.fsproj"
    let libraryBinDir = artifactsDir </> "BlackFox.MasterOfFoo" </> configuration
    let solutionFile = srcDir </> "MasterOfFoo.sln"
    let projects =
        from srcDir
        ++ "**/*.*proj"
        -- "*.Build/*"

    /// The profile where the project is posted
    let gitOwner = "vbfox"
    let gitHome = "https://github.com/" + gitOwner

    /// The name of the project on GitHub
    let gitName = "MasterOfFoo"

    /// The url for the raw files hosted
    let gitRaw = Environment.environVarOrDefault "gitRaw" ("https://raw.github.com/" + gitOwner)

    let inline versionPartOrZero x = if x < 0 then 0 else x

    let release =
        let fromFile = ReleaseNotes.load (rootDir </> "Release Notes.md")
        if BuildServer.buildServer = BuildServer.AppVeyor then
            let appVeyorBuildVersion = int AppVeyor.Environment.BuildVersion
            let nugetVer = sprintf "%s-appveyor%04i" fromFile.NugetVersion appVeyorBuildVersion
            let asmVer = System.Version.Parse(fromFile.AssemblyVersion)
            let asmVer =
                System.Version(
                    versionPartOrZero asmVer.Major,
                    versionPartOrZero asmVer.Minor,
                    versionPartOrZero asmVer.Build,
                    versionPartOrZero appVeyorBuildVersion)
            ReleaseNotes.ReleaseNotes.New(asmVer.ToString(), nugetVer, fromFile.Date, fromFile.Notes)
        else
            fromFile

    let mutable dotnetExePath = "dotnet"

    AppVeyorEx.updateBuild (fun info -> { info with Version = Some release.AssemblyVersion })

    let writeVersionProps() =
        let doc =
            XDocument(
                XElement(XName.Get("Project"),
                    XElement(XName.Get("PropertyGroup"),
                        XElement(XName.Get "Version", release.NugetVersion),
                        XElement(XName.Get "PackageReleaseNotes", String.toLines release.Notes))))
        let path = artifactsDir </> "Version.props"
        System.IO.File.WriteAllText(path, doc.ToString())

    let init = task "Init" [] {
        Directory.create artifactsDir
    }

    let clean = task "Clean" [init] {
        for x in projects do
            Trace.tracefn "%s" x

        let objDirs = projects |> Seq.map(fun p -> System.IO.Path.GetDirectoryName(p) </> "obj") |> List.ofSeq
        for x in objDirs do
            Trace.tracefn "%s" x
        Shell.cleanDirs (artifactsDir :: objDirs)
    }

    let generateVersionInfo = task "GenerateVersionInfo" [init; clean.IfNeeded] {
        writeVersionProps ()
        AssemblyInfoFile.createFSharp (artifactsDir </> "Version.fs") [AssemblyInfo.Version release.AssemblyVersion]
    }

    let build = task "Build" [generateVersionInfo; clean.IfNeeded] {
        DotNet.build
          (fun p -> { p with Configuration = fakeConfiguration })
          solutionFile
    }

    let runTests = task "RunTests" [build] {
        [artifactsDir </> "BlackFox.MasterOfFoo.Tests" </> configuration </> "netcoreapp2.0" </> "BlackFox.MasterOfFoo.Tests.dll"]
            |> ExpectoDotNetCli.run (fun p ->
                { p with
                    PrintVersion = false
                    ParallelWorkers = System.Environment.ProcessorCount
                    FailOnFocusedTests = true
                })
    }

    let nupkgDir = artifactsDir </> "BlackFox.MasterOfFoo" </> configuration

    let nuget = task "NuGet" [build] {
        DotNet.pack
            (fun p -> { p with Configuration = fakeConfiguration })
            libraryProjectFile 
        let nupkgFile =
            nupkgDir
                </> (sprintf "BlackFox.MasterOfFoo.%s.nupkg" release.NugetVersion)

        Trace.publish ImportData.BuildArtifact nupkgFile
    }

    let publishNuget = task "PublishNuget" [nuget] {
        let key =
            match Environment.environVarOrNone "nuget-key" with
            | Some(key) -> key
            | None -> UserInput.getUserPassword "NuGet key: "

        Paket.push <| fun p ->  { p with WorkingDir = nupkgDir; ApiKey = key }
    }

    let zipFile = artifactsDir </> (sprintf "BlackFox.MasterOfFoo-%s.zip" release.NugetVersion)

    let zip = task "Zip" [build] {
        let comment = sprintf "MasterOfFoo v%s" release.NugetVersion
        from libraryBinDir
            ++ "**/*.dll"
            ++ "**/*.xml"
            -- "**/FSharp.Core.*"
            |> Zip.createZip libraryBinDir zipFile comment 9 false
        
        Trace.publish ImportData.BuildArtifact zipFile
    }

    (*
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
    *)

    let gitRelease = task "GitRelease" [] {
        let remote =
            Git.CommandHelper.getGitResult "" "remote -v"
            |> Seq.filter (fun (s: string) -> s.EndsWith("(push)"))
            |> Seq.tryFind (fun (s: string) -> s.Contains(gitOwner + "/" + gitName))
            |> function None -> gitHome + "/" + gitName | Some (s: string) -> s.Split().[0]

        Git.Branches.tag "" release.NugetVersion
        Git.Branches.pushTag "" remote release.NugetVersion
    }

    let _releaseTask = EmptyTask "Release" [clean; gitRelease; (*gitHubRelease;*) publishNuget]
    let _ciTask = EmptyTask "CI" [clean; runTests; zip; nuget]

    EmptyTask "Default" [runTests]
