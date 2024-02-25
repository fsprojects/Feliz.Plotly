open System
open System.IO

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.JavaScript
open Fake.Tools
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

let rootDirectory = __SOURCE_DIRECTORY__ @@ ".."
// Read additional information from the release notes document
let release = ReleaseNotes.load (rootDirectory @@ "RELEASE_NOTES.md")

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
let publicDir  = rootDirectory @@ "public"
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

let initTargets () =
    // Set name of 'configuration' variable to 'Release'
    FakeVar.set "configuration" "Release"

    Target.create "All" ignore
    Target.create "Dev" ignore
    Target.create "Release" ignore
    Target.create "Publish" ignore

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

    Target.create "Clean" <| fun _ ->
        let clean() =
            !! (__SOURCE_DIRECTORY__  @@ "tests/**/bin")
            ++ (__SOURCE_DIRECTORY__  @@ "tests/**/obj")
            ++ (__SOURCE_DIRECTORY__  @@ "tools/bin")
            ++ (__SOURCE_DIRECTORY__  @@ "tools/obj")
            ++ (__SOURCE_DIRECTORY__  @@ "src/**/bin")
            ++ (__SOURCE_DIRECTORY__  @@ "src/**/obj")
            |> Seq.toList
            |> List.append [bin; temp; objFolder]
            |> Shell.cleanDirs

        TaskRunner.runWithRetries clean 10

    Target.create "CleanDocs" <| fun _ ->
        let clean() =
            !! (publicDir @@ "*.md")
            ++ (publicDir @@ "*bundle.*")
            ++ (publicDir @@ "**/README.md")
            ++ (publicDir @@ "**/RELEASE_NOTES.md")
            ++ (publicDir @@ "index.html")
            |> List.ofSeq
            |> List.iter Shell.rm

        TaskRunner.runWithRetries clean 10

    Target.create "CopyDocFiles" <| fun _ ->
        [ publicDir @@ "Plotly/README.md", rootDirectory @@ "README.md"
          publicDir @@ "Plotly/RELEASE_NOTES.md", rootDirectory @@ "RELEASE_NOTES.md"
          publicDir @@ "index.html", __SOURCE_DIRECTORY__ @@ "docs/index.html" ]
        |> List.iter (fun (target, source) -> Shell.copyFile target source)

    Target.create "PrepDocs" ignore

    Target.create "PostBuildClean" <| fun _ ->
        let clean() =
            !! srcGlob
            -- (__SOURCE_DIRECTORY__ @@ "src/**/*.shproj")
            |> Seq.map (
                (fun f -> (Path.getDirectory f) @@ "bin" @@ configuration())
                >> (fun f -> Directory.EnumerateDirectories(f) |> Seq.toList )
                >> (fun fL -> fL |> List.map (fun f -> Directory.EnumerateDirectories(f) |> Seq.toList)))
            |> (Seq.concat >> Seq.concat)
            |> Seq.iter Directory.delete

        TaskRunner.runWithRetries clean 10

    Target.create "PostPublishClean" <| fun _ ->
        let clean() =
            !! (__SOURCE_DIRECTORY__ @@ "src/**/bin" @@ configuration() @@ "/**/publish")
            |> Seq.iter Directory.delete

        TaskRunner.runWithRetries clean 10

    // --------------------------------------------------------------------------------------
    // Restore tasks

    let restoreSolution () =
        solutionFile
        |> DotNet.restore id

    Target.create "Restore" <| fun _ ->
        TaskRunner.runWithRetries restoreSolution 5

    Target.create "YarnInstall" <| fun _ ->
        let setParams (defaults: Yarn.YarnParams) : Yarn.YarnParams =
            { defaults with
                Yarn.YarnParams.YarnFilePath = (__SOURCE_DIRECTORY__ @@ "packages/tooling/Yarnpkg.Yarn/content/bin/yarn.cmd")
            }

        Yarn.install setParams

    // --------------------------------------------------------------------------------------
    // Build tasks

    let buildProject (project: string) : unit =
        let setParams (defaults:MSBuildParams) : MSBuildParams =
            { defaults with
                Verbosity = Some(Quiet)
                Targets = ["Build"]
                Properties =
                    [
                        "Optimize", "True"
                        "DebugSymbols", "True"
                        "Configuration", configuration()
                        "Version", release.AssemblyVersion
                        "GenerateDocumentationFile", "true"
                        "DependsOnNETStandard", "true"
                    ] }

        TaskRunner.runWithRetries (fun _ -> MSBuild.build setParams project) 10

    Target.create "RunGenerators" <| fun _ ->
        let runGenerator (path: string) =
            CreateProcess.fromCommand(setCmd path [])
            |> CreateProcess.withTimeout TimeSpan.MaxValue
            |> CreateProcess.ensureExitCodeWithMessage $"Generator {path} failed."
            |> Proc.run
            |> ignore

        Trace.trace "Running generators..."

        !! genGlob
        |> List.ofSeq
        |> List.iter (fun project ->
            buildProject project

            !! ((FileInfo.ofPath project).Directory.FullName @@ "bin" @@ configuration() @@ "**/*.Generator.*.exe")
            |> List.ofSeq
            |> List.tryHead
            |> Option.iter (fun path -> TaskRunner.runWithRetries (fun _ -> runGenerator path) 5))

    Target.create "Build" <| fun _ ->
        restoreSolution ()

        !! libGlob
        -- genGlob
        |> List.ofSeq
        |> List.iter buildProject

    // --------------------------------------------------------------------------------------
    // Publish net core applications

    Target.create "PublishDotNet" <| fun _ ->
        let runPublish (project: string) (framework: string) =
            let setParams (defaults: MSBuildParams) : MSBuildParams =
                { defaults with
                    Verbosity = Some(Quiet)
                    Targets = ["Publish"]
                    Properties =
                        [
                            "Optimize", "True"
                            "DebugSymbols", "True"
                            "Configuration", configuration()
                            "Version", release.AssemblyVersion
                            "GenerateDocumentationFile", "true"
                            "TargetFramework", framework
                        ]
                }

            MSBuild.build setParams project

        !! libGlob
        -- genGlob
        |> Seq.map
            ((fun f -> (((Path.getDirectory f) @@ "bin" @@ configuration()), f) )
            >>
            (fun f ->
                Directory.EnumerateDirectories(fst f)
                |> Seq.filter (fun frFolder -> frFolder.Contains("netcoreapp"))
                |> Seq.map (fun frFolder -> DirectoryInfo(frFolder).Name), snd f))
        |> Seq.iter (fun (l,p) -> l |> Seq.iter (runPublish p))

    // --------------------------------------------------------------------------------------
    // Run the unit test binaries

    Target.create "RunTests" <| fun _ ->
        let globbingPattern = !! ("tests/**/bin" @@ configuration() @@ "**" @@ "*Tests.exe")

        for path in globbingPattern do
            CreateProcess.fromCommand(setCmd path [])
            |> CreateProcess.withTimeout TimeSpan.MaxValue
            |> CreateProcess.ensureExitCodeWithMessage "Tests failed."
            |> Proc.run
            |> ignore

    Target.create "PackageJson" <| fun _ ->
        let setValues (current: Json.JsonPackage) =
            { current with
                Name = Json.Str.toKebabCase project |> Some
                Version = release.NugetVersion |> Some
                Description = summary |> Some
                Homepage = repo |> Some
                Repository =
                    { Json.RepositoryValue.Type = "git" |> Some
                      Json.RepositoryValue.Url = repo |> Some
                      Json.RepositoryValue.Directory = None }
                    |> Some
                Bugs =
                    { Json.BugsValue.Url =
                        @"https://github.com/fsprojects/Feliz.Plotly/issues/new/choose" |> Some } |> Some
                License = "MIT" |> Some
                Author = author |> Some
                Private = true |> Some }

        Json.setJsonPkg setValues

    // --------------------------------------------------------------------------------------
    // Documentation targets

    Target.createFinal "KillProcess" <| fun _ ->
        Process.killAllByName "node.exe"
        Process.killAllByName "Node.js"

    Target.create "Start" <| fun _ ->
        let buildApp = async { Yarn.exec "start" id }
        let launchBrowser =
            let url = "http://localhost:8080"
            async {
                do! Async.Sleep 15000
                try
                    if Environment.isLinux then
                        Shell.Exec(
                            sprintf "URL=\"%s\"; xdg-open $URL ||\
                                sensible-browser $URL || x-www-browser $URL || gnome-open $URL" url)
                        |> ignore
                    else Shell.Exec("open", args = url) |> ignore
                with _ -> failwith "Opening browser failed."
            }

        Target.activateFinal "KillProcess"

        [ buildApp; launchBrowser ]
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore

    Target.create "DemoRaw" <| fun _ ->
        Yarn.exec "compile-demo-raw" id

    Target.create "PublishPages" <| fun _ ->
        Yarn.exec "publish-docs" id

    // --------------------------------------------------------------------------------------
    // Build and release NuGet targets

    Target.create "NuGet" <| fun _ ->
        Paket.pack(fun p ->
            { p with
                OutputPath = bin
                Version = release.NugetVersion
                ReleaseNotes = Fake.Core.String.toLines release.Notes
                ProjectUrl = repo
                MinimumFromLockFile = true
                IncludeReferencedProjects = true })

    Target.create "NuGetPublish" <| fun _ ->
        Paket.push(fun p ->
            { p with
                ApiKey =
                    match getEnvFromAllOrNone "NUGET_KEY" with
                    | Some key -> key
                    | None -> failwith "The NuGet API key must be set in a NUGET_KEY environment variable"
                WorkingDir = bin })

    // --------------------------------------------------------------------------------------
    // Release Scripts

    let gitPush msg =
        Git.Staging.stageAll ""
        Git.Commit.exec "" msg
        Git.Branches.push ""

    Target.create "GitPush" <| fun p ->
        p.Context.Arguments
        |> List.choose (fun s ->
            match s.StartsWith("--Msg=") with
            | true -> Some(s.Substring 6)
            | false -> None)
        |> List.tryHead
        |> function
        | Some(s) -> s
        | None -> (sprintf "Bump version to %s" release.NugetVersion)
        |> gitPush

    Target.create "GitTag" <| fun _ ->
        Git.Branches.tag "" release.NugetVersion
        Git.Branches.pushTag "" "origin" release.NugetVersion

    Target.create "PublishDocs" <| fun _ ->
        gitPush "Publishing docs"


let buildTargetTree () =
    let (==>!) x y = x ==> y |> ignore
    let (?=>!) x y = x ?=> y |> ignore

    "Clean"
    ==> "AssemblyInfo"
    ==> "Restore"
    ==> "PackageJson"
    ==> "YarnInstall"
    ==> "RunGenerators"
    ==> "Build"
    ==> "PostBuildClean"
    ==>! "CopyBinaries"

    "Build" ==>! "RunTests"

    "Build"
    ==> "PostBuildClean"
    ==> "PublishDotNet"
    ==> "PostPublishClean"
    ==>! "CopyBinaries"

    // "Restore" ==>! "Lint"
    // "Restore" ==>! "Format"

    // "Format"
    // // ?=> "Lint"
    // ?=> "Build"
    // ?=> "RunTests"
    // ?=>! "CleanDocs"

    "All"
    ==> "GitPush"
    ?=>! "GitTag"

    "All" <== [
        // "Lint"
        "RunTests"
        "CopyBinaries"
    ]

    "CleanDocs"
    ==> "CopyDocFiles"
    ==>! "PrepDocs"

    "All"
    ==> "NuGet"
    ==>! "NuGetPublish"

    "PrepDocs"
    ==> "PublishPages"
    ==>! "PublishDocs"

    "All"
    ==> "PrepDocs"
    ==>! "DemoRaw"

    "All"
    ==> "PrepDocs"
    ==>! "Start"

    "All" ==>! "PublishPages"

    "ConfigDebug" ?=>! "Clean"
    "ConfigRelease" ?=>! "Clean"

    "Dev" <== ["All"; "ConfigDebug"; "Start"]

    "Release" <== ["All"; "NuGet"; "ConfigRelease"]

    "Publish" <== ["Release"; "ConfigRelease"; "NuGetPublish"; "PublishDocs"; "GitTag"; "GitPush" ]


[<EntryPoint>]
let main argv =
    printfn $"Arguments:\n{argv}"
    argv
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build.fsx"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext

    initTargets ()
    buildTargetTree ()

    // Target.runOrDefaultWithArguments "Default"
    Target.runOrDefaultWithArguments "Dev"
    0