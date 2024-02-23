open System
open System.IO

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators


// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "Feliz.Plotly"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "Fable bindings written in the Feliz-style for plotly.js"

// Author(s) of the project
let author = "Cody Johnson"

// File system information
let solutionFile = "Feliz.Plotly.sln"

// Github repo
let repo = "https://github.com/fsprojects/Feliz.Plotly"

// Files to skip Fantomas formatting
let excludeFantomas =
    [ __SOURCE_DIRECTORY__ @@ "src/Feliz.Plotly/Props/*.fs"
      __SOURCE_DIRECTORY__ @@ "src/Feliz.Plotly/Types.fs"
      __SOURCE_DIRECTORY__ @@ "src/Feliz.Plotly/Interop.fs"
      __SOURCE_DIRECTORY__ @@ "src/Feliz.Plotly/Plotly.fs" ]

// Files that have bindings to other languages where name linting needs to be more relaxed.
let relaxedNameLinting =
    [ __SOURCE_DIRECTORY__ @@ "src/Feliz.Plotly/**/*.fs"
      __SOURCE_DIRECTORY__ @@ "src/Feliz.Plotly/*.fs" ]

// Read additional information from the release notes document
let release = ReleaseNotes.load (__SOURCE_DIRECTORY__ @@ "RELEASE_NOTES.md")

// Helper active pattern for project types
let (|Fsproj|Csproj|Vbproj|Shproj|) (projectFileName: string) =
    match projectFileName with
    | f when f.EndsWith("fsproj") -> Fsproj
    | f when f.EndsWith("csproj") -> Csproj
    | f when f.EndsWith("vbproj") -> Vbproj
    | f when f.EndsWith("shproj") -> Shproj
    | _ -> failwith ($"Project file {projectFileName} not supported. Unknown project type.")

let srcGlob    = __SOURCE_DIRECTORY__ @@ "src/**/*.??proj"
let fsSrcGlob  = __SOURCE_DIRECTORY__ @@ "src/**/*.fs"
let fsTestGlob = __SOURCE_DIRECTORY__ @@ "tests/**/*.fs"
let bin        = __SOURCE_DIRECTORY__ @@ "bin"
let temp       = __SOURCE_DIRECTORY__ @@ "temp"
let objFolder  = __SOURCE_DIRECTORY__ @@ "obj"
let publicDir  = __SOURCE_DIRECTORY__ @@ "public"
let genGlob    = __SOURCE_DIRECTORY__ @@ "src/**/*.Generator.*.fsproj"
let libGlob    = __SOURCE_DIRECTORY__ @@ "src/**/*.fsproj"

let foldExcludeGlobs (g: IGlobbingPattern) (d: string) = g -- d
let foldIncludeGlobs (g: IGlobbingPattern) (d: string) = g ++ d

let fsSrcAndTest : IGlobbingPattern =
    !! fsSrcGlob
    ++ fsTestGlob
    -- (__SOURCE_DIRECTORY__  @@ "src/**/obj/**")
    -- (__SOURCE_DIRECTORY__  @@ "tests/**/obj/**")
    -- (__SOURCE_DIRECTORY__  @@ "src/**/AssemblyInfo.*")
    -- (__SOURCE_DIRECTORY__  @@ "src/**/**/AssemblyInfo.*")

let fsRelaxedNameLinting : IGlobbingPattern option =
    let baseGlob (s: string) =
        !! s
        -- (__SOURCE_DIRECTORY__  @@ "src/**/AssemblyInfo.*")
        -- (__SOURCE_DIRECTORY__  @@ "src/**/obj/**")
        -- (__SOURCE_DIRECTORY__  @@ "tests/**/obj/**")

    match relaxedNameLinting with
    | [h] when relaxedNameLinting.Length = 1 -> baseGlob h |> Some
    | h::t -> List.fold foldIncludeGlobs (baseGlob h) t |> Some
    | _ -> None

let setCmd (filePath: FilePath) (args: string list) : Command =
    match Environment.isWindows with
    | true  -> RawCommand(filePath, Arguments.OfArgs args)
    | false -> RawCommand("mono", Arguments.OfArgs (filePath::args))

let configuration () : string =
    FakeVar.getOrDefault "configuration" "Release"

/// Attempt to retrieve an environment variable from either the process, user, or machine
let getEnvFromAllOrNone (environmentVariable: string) : string option =
    let envOpt (envVar: string) =
        if String.isNullOrEmpty envVar then None
        else Some(envVar)

    let processVariable = Environment.GetEnvironmentVariable(environmentVariable) |> envOpt
    let userVariable = Environment.GetEnvironmentVariable(environmentVariable, EnvironmentVariableTarget.User) |> envOpt
    let machineVariable = Environment.GetEnvironmentVariable(environmentVariable, EnvironmentVariableTarget.Machine) |> envOpt

    match processVariable, userVariable, machineVariable with
    | Some(v), _, _
    | _, Some(v), _
    | _, _, Some(v)
        -> Some(v)
    | _ -> None

// Set name of 'configuration' variable to 'Release'
FakeVar.set "configuration" "Release"

// --------------------------------------------------------------------------------------
// Set configuration mode based on target

Target.create "ConfigDebug" <| fun _ ->
    FakeVar.set "configuration" "Debug"

Target.create "ConfigRelease" <| fun _ ->
    FakeVar.set "configuration" "Release"

// --------------------------------------------------------------------------------------
// Generate assembly info files with the right version & up-to-date information

Target.create "AssemblyInfo" <| fun _ ->
    let getAssemblyInfoAttributes (projectName: string) : AssemblyInfo.Attribute list =
        [ AssemblyInfo.Title projectName
          AssemblyInfo.Product project
          AssemblyInfo.Description summary
          AssemblyInfo.Version release.AssemblyVersion
          AssemblyInfo.FileVersion release.AssemblyVersion
          AssemblyInfo.Configuration <| configuration()
          AssemblyInfo.InternalsVisibleTo $"{projectName}.Tests" ]

    let getProjectDetails (projectPath: string) =
        let projectName = Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath,
          projectName,
          Path.GetDirectoryName(projectPath),
          getAssemblyInfoAttributes projectName
        )

    for projectPath in (!! srcGlob) do
        let (projectFileName, _, folderName, assemblyInfoAttributes) = getProjectDetails projectPath

        match projectFileName with
        | Fsproj -> AssemblyInfoFile.createFSharp (folderName </> "AssemblyInfo.fs") assemblyInfoAttributes
        | Csproj -> AssemblyInfoFile.createCSharp ((folderName </> "Properties") </> "AssemblyInfo.cs") assemblyInfoAttributes
        | Vbproj -> AssemblyInfoFile.createVisualBasic ((folderName </> "My Project") </> "AssemblyInfo.vb") assemblyInfoAttributes
        | Shproj -> ()

// --------------------------------------------------------------------------------------
// Copies binaries from default VS location to expected bin folder
// But keeps a subdirectory structure for each project in the
// src folder to support multiple project outputs

Target.create "CopyBinaries" <| fun _ ->
    let globbingPattern = !! libGlob -- genGlob -- (__SOURCE_DIRECTORY__ @@ "src/**/*.shproj")

    for path in globbingPattern do
        let (fromDir, toDir) = (Path.getDirectory path) @@ "bin" @@ configuration(), "bin" @@ (Path.GetFileNameWithoutExtension path)

        Shell.copyDir toDir fromDir (fun _ -> true)

// --------------------------------------------------------------------------------------
// Clean tasks

// Target.create "Clean" <| fun _ ->
//     let clean() =
//         !! (__SOURCE_DIRECTORY__  @@ "tests/**/bin")
//         ++ (__SOURCE_DIRECTORY__  @@ "tests/**/obj")
//         ++ (__SOURCE_DIRECTORY__  @@ "tools/bin")
//         ++ (__SOURCE_DIRECTORY__  @@ "tools/obj")
//         ++ (__SOURCE_DIRECTORY__  @@ "src/**/bin")
//         ++ (__SOURCE_DIRECTORY__  @@ "src/**/obj")
//         |> Seq.toList
//         |> List.append [bin; temp; objFolder]
//         |> Shell.cleanDirs

//     TaskRunner.runWithRetries clean 10


[<EntryPoint>]
let main argv =
    argv
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build.fsx"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext

    // initTargets ()
    // buildTargetTree ()

    Target.runOrDefaultWithArguments "Default"
    0