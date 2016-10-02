// include Fake libs
#r "../packages/FAKE/tools/FakeLib.dll"
#load "./TaskDefinitionHelper.fsx"
#load "./CmdLine.fs"

open Fake
open Fake.ReleaseNotesHelper
open BlackFox
open BlackFox.CommandLine

let configuration = environVarOrDefault "configuration" "Release"

let from s = { BaseDirectory = s; Includes = []; Excludes = [] }

let rootDir = System.IO.Path.GetFullPath(__SOURCE_DIRECTORY__ </> "..")
let srcDir = rootDir </> "src"
let artifactsDir = rootDir </> "artifacts"
let binDir = artifactsDir </> "bin"
let librarySrcDir = srcDir </> "BlackFox.MasterOfFoo"
let libraryBinDir = binDir </> "BlackFox.MasterOfFoo" </> configuration
let projects = from srcDir ++ "**/*.*proj"
let release =
    let fromFile = LoadReleaseNotes (rootDir </> "Release Notes.md")
    if buildServer = AppVeyor then
        let nugetVer = sprintf "%s-appveyor.%s" fromFile.NugetVersion appVeyorBuildVersion
        let asmVer = System.Version.Parse(fromFile.AssemblyVersion)
        let asmVer = System.Version(asmVer.Major, asmVer.Minor, asmVer.Build, (int appVeyorBuildVersion))
        ReleaseNotes.New(asmVer.ToString(), nugetVer, fromFile.Date, fromFile.Notes)
    else
        fromFile

module AppVeyorEx =
    open Fake
    open System.IO

    let private sendToAppVeyor args =
        ExecProcess (fun info ->
            info.FileName <- "appveyor"
            info.Arguments <- args) (System.TimeSpan.MaxValue)
        |> ignore

    type BuildInfo = {
        Version: string option
        Message: string option
        CommitId: string option
        Committed: System.DateTimeOffset option
        AuthorName: string option
        AuthorEmail: string option
        CommitterName: string option
        CommitterEmail: string option
    }

    let defaultBuildInfo = {
        Version = None
        Message = None
        CommitId = None
        Committed = None
        AuthorName = None
        AuthorEmail = None
        CommitterName = None
        CommitterEmail = None
    }

    let updateBuild (setBuildInfo : BuildInfo -> BuildInfo) =
        let appendAppVeyor opt (name: string) (transform: _ -> string) cmdLine =
            match opt with
            | Some(value) ->
                cmdLine
                |> CmdLine.append name
                |> CmdLine.append (transform value)
            | None -> cmdLine
        if buildServer = BuildServer.AppVeyor then
            let info = setBuildInfo defaultBuildInfo
            let cmdLine =
                CmdLine.empty
                |> CmdLine.append "UpdateBuild"
                |> appendAppVeyor info.Version "-Version" id
                |> appendAppVeyor info.Message "-Message" id
                |> appendAppVeyor info.CommitId "-CommitId" id
                |> appendAppVeyor info.Committed "-Committed" (fun d -> d.ToString("MMddyyyy-HHmm"))
                |> appendAppVeyor info.AuthorName "-AuthorName" id
                |> appendAppVeyor info.AuthorEmail "-AuthorEmail" id
                |> appendAppVeyor info.CommitterName "-CommitterName" id
                |> appendAppVeyor info.CommitterEmail "-CommitterEmail" id
                |> CmdLine.toString

            sendToAppVeyor cmdLine

Task "Init" [] <| fun _ ->
    printf "%A" release
    AppVeyorEx.updateBuild (fun info -> { info with Version = Some release.AssemblyVersion })
    CreateDir artifactsDir

// Targets
Task "Clean" ["Init"] <| fun _ ->
    CleanDirs [artifactsDir]

Task "Build" ["Init"; "?Clean"] <| fun _ ->
    MSBuild null "Build" ["Configuration", configuration] projects
        |> ignore

Task "NuGet" ["Build"] <| fun _ ->
    Paket.Pack <| fun p ->
        { p with
            OutputPath = artifactsDir
            Version = release.NugetVersion
            ReleaseNotes = toLines release.Notes
            WorkingDir = libraryBinDir
            BuildConfig = configuration
            BuildPlatform = "AnyCPU"}
    AppVeyor.PushArtifacts (from artifactsDir ++ "*.nupkg")

Task "Zip" ["Build"] <| fun _ ->
    let zipFile = artifactsDir </> (sprintf "MasterOfFoo-%s.zip" release.NugetVersion)
    from libraryBinDir
        ++ "**/*.dll"
        ++ "**/*.xml"
        -- "**/FSharp.Core.*"
        |> Zip libraryBinDir zipFile
    AppVeyor.PushArtifacts [zipFile]

Task "CI" ["Clean"; "Zip"; "NuGet"] DoNothing

// start build
RunTaskOrDefault "Build"
