// include Fake libs
#r "../packages/FAKE/tools/FakeLib.dll"
#load "./TaskDefinitionHelper.fsx"
#load "./AppVeyorEx.fsx"

open System
open Fake
open Fake.ReleaseNotesHelper
open Fake.Testing.NUnit3
open BlackFox

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
        let appVeyorBuildVersion = int appVeyorBuildVersion
        let nugetVer = sprintf "%s-appveyor%04i" fromFile.NugetVersion appVeyorBuildVersion
        let asmVer = System.Version.Parse(fromFile.AssemblyVersion)
        let asmVer = System.Version(asmVer.Major, asmVer.Minor, asmVer.Build, (int appVeyorBuildVersion))
        ReleaseNotes.New(asmVer.ToString(), nugetVer, fromFile.Date, fromFile.Notes)
    else
        fromFile

AppVeyorEx.updateBuild (fun info -> { info with Version = Some release.AssemblyVersion })

Task "Init" [] <| fun _ ->
    CreateDir artifactsDir

// Targets
Task "Clean" ["Init"] <| fun _ ->
    CleanDirs [artifactsDir]

Task "Build" ["Init"; "?Clean"] <| fun _ ->
    MSBuild null "Build" ["Configuration", configuration] projects
        |> ignore

Task "RunTests" [ "Build"] <| fun _ ->
    let nunitPath = rootDir </> @"packages" </> "NUnit.ConsoleRunner" </> "tools" </> "nunit3-console.exe"
    let testAssemblies = artifactsDir </> "bin" </> "*.Tests" </> configuration </> "*.Tests.dll"
    let testResults = artifactsDir </> "TestResults.xml"

    !! testAssemblies
      |> NUnit3 (fun p ->
          {p with
             ToolPath = nunitPath
             TimeOut = TimeSpan.FromMinutes 20.
             DisposeRunners = true
             ResultSpecs = [testResults] })

    AppVeyor.UploadTestResultsFile AppVeyor.NUnit3 testResults

Task "NuGet" ["Build"] <| fun _ ->
    Paket.Pack <| fun p ->
        { p with
            OutputPath = artifactsDir
            Version = release.NugetVersion
            ReleaseNotes = toLines release.Notes
            WorkingDir = librarySrcDir
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

Task "CI" ["Clean"; "RunTests"; "Zip"; "NuGet"] DoNothing

// start build
RunTaskOrDefault "Build"
