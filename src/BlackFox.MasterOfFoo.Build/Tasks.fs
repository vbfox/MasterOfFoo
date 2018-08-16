module BlackFox.MasterOfFoo.Build.Tasks

open Fake.Api
open Fake.BuildServer
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing
open Fake.IO.Globbing.Operators
open Fake.IO.FileSystemOperators
open Fake.Tools

open BlackFox
open BlackFox.Fake
open System.Xml.Linq

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
    let nupkgDir = artifactsDir </> "BlackFox.MasterOfFoo" </> configuration
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
        if BuildServer.buildServer <> BuildServer.LocalBuild then
            let buildVersion = int BuildServer.buildVersion
            let nugetVer = sprintf "%s-appveyor%04i" fromFile.NugetVersion buildVersion
            let asmVer = System.Version.Parse(fromFile.AssemblyVersion)
            let asmVer =
                System.Version(
                    versionPartOrZero asmVer.Major,
                    versionPartOrZero asmVer.Minor,
                    versionPartOrZero asmVer.Build,
                    versionPartOrZero buildVersion)
            ReleaseNotes.ReleaseNotes.New(asmVer.ToString(), nugetVer, fromFile.Date, fromFile.Notes)
        else
            fromFile

    Trace.setBuildNumber release.AssemblyVersion

    let writeVersionProps() =
        let doc =
            XDocument(
                XElement(XName.Get("Project"),
                    XElement(XName.Get("PropertyGroup"),
                        XElement(XName.Get "Version", release.NugetVersion),
                        XElement(XName.Get "PackageReleaseNotes", String.toLines release.Notes))))
        let path = artifactsDir </> "Version.props"
        System.IO.File.WriteAllText(path, doc.ToString())

    let init = BuildTask.create "Init" [] {
        Directory.create artifactsDir
    }

    let clean = BuildTask.create "Clean" [init] {
        let objDirs = projects |> Seq.map(fun p -> System.IO.Path.GetDirectoryName(p) </> "obj") |> List.ofSeq
        Shell.cleanDirs (artifactsDir :: objDirs)
    }

    let generateVersionInfo = BuildTask.create "GenerateVersionInfo" [init; clean.IfNeeded] {
        writeVersionProps ()
        AssemblyInfoFile.createFSharp (artifactsDir </> "Version.fs") [AssemblyInfo.Version release.AssemblyVersion]
    }

    let build = BuildTask.create "Build" [generateVersionInfo; clean.IfNeeded] {
        DotNet.build
          (fun p -> { p with Configuration = fakeConfiguration })
          solutionFile
    }

    let runTests = BuildTask.create "RunTests" [build] {
        [artifactsDir </> "BlackFox.MasterOfFoo.Tests" </> configuration </> "netcoreapp2.0" </> "BlackFox.MasterOfFoo.Tests.dll"]
            |> ExpectoDotNetCli.run (fun p ->
                { p with
                    PrintVersion = false
                    FailOnFocusedTests = true
                })
    }

    let nuget = BuildTask.create "NuGet" [build] {
        DotNet.pack
            (fun p -> { p with Configuration = fakeConfiguration })
            libraryProjectFile
        let nupkgFile =
            nupkgDir
                </> (sprintf "BlackFox.MasterOfFoo.%s.nupkg" release.NugetVersion)

        Trace.publish ImportData.BuildArtifact nupkgFile
    }

    let publishNuget = BuildTask.create "PublishNuget" [nuget] {
        let key =
            match Environment.environVarOrNone "nuget-key" with
            | Some(key) -> key
            | None -> UserInput.getUserPassword "NuGet key: "

        Paket.push <| fun p ->  { p with WorkingDir = nupkgDir; ApiKey = key }
    }

    let zipFile = artifactsDir </> (sprintf "BlackFox.MasterOfFoo-%s.zip" release.NugetVersion)

    let zip = BuildTask.create "Zip" [build] {
        let comment = sprintf "MasterOfFoo v%s" release.NugetVersion
        from libraryBinDir
            ++ "**/*.dll"
            ++ "**/*.xml"
            -- "**/FSharp.Core.*"
            |> Zip.createZip libraryBinDir zipFile comment 9 false

        Trace.publish ImportData.BuildArtifact zipFile
    }

    let gitHubRelease = BuildTask.create "GitHubRelease" [zip] {
        let user =
            match Environment.environVarOrNone "github-user" with
            | Some s -> s
            | _ -> UserInput.getUserInput "GitHub Username: "
        let pw =
            match Environment.environVarOrNone "github-pw" with
            | Some s -> s
            | _ -> UserInput.getUserPassword "GitHub Password or Token: "

        // release on github
        GitHub.createClient user pw
        |> GitHub.draftNewRelease
            gitOwner
            gitName
            release.NugetVersion
            (release.SemVer.PreRelease <> None)
            (release.Notes)
        |> GitHub.uploadFile zipFile
        |> GitHub.publishDraft
        |> Async.RunSynchronously
    }

    let gitRelease = BuildTask.create "GitRelease" [] {
        let remote =
            Git.CommandHelper.getGitResult "" "remote -v"
            |> Seq.filter (fun (s: string) -> s.EndsWith("(push)"))
            |> Seq.tryFind (fun (s: string) -> s.Contains(gitOwner + "/" + gitName))
            |> function None -> gitHome + "/" + gitName | Some (s: string) -> s.Split().[0]

        Git.Branches.tag "" release.NugetVersion
        Git.Branches.pushTag "" remote release.NugetVersion
    }

    let _releaseTask = BuildTask.createEmpty "Release" [clean; gitRelease; gitHubRelease; publishNuget]
    let _ciTask = BuildTask.createEmpty "CI" [clean; runTests; zip; nuget]

    BuildTask.createEmpty "Default" [runTests]
