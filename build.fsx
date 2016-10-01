// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"
#load "./build/TaskDefinitionHelper.fsx"
open Fake

let configuration = environVarOrDefault "configuration" "Release"
let rootDir = __SOURCE_DIRECTORY__
let artifactsDir = rootDir </> "artifacts"
let binDir = artifactsDir </> "bin"
let libraryBinDir = binDir </> "BlackFox.MasterOfFoo"

// Filesets
let appReferences  =
    !! "/**/*.csproj"
      ++ "/**/*.fsproj"

// version info
let version = "0.1"  // or retrieve from CI server

// Targets
Target "Clean" (fun _ ->
    CleanDirs [artifactsDir]
)

Target "Build" (fun _ ->
    // compile all projects below src/app/
    MSBuildDebug null "Build" appReferences
        |> Log "AppBuild-Output: "
)

Target "Deploy" (fun _ ->
    !! (libraryBinDir + "/**/*.*")
        -- "*.zip"
        |> Zip libraryBinDir (artifactsDir </> ("ApplicationName." + version + ".zip"))
)

// Build order
"Clean"
  ==> "Build"
  ==> "Deploy"

// start build
RunTargetOrDefault "Build"
