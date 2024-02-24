open System
open System.IO


// let plotlyExamplePaths = Directory.EnumerateFiles(@"C:\Users\james\source\repos\plotly-examples\plotly-examples\src\examples", "*.fsx", SearchOption.AllDirectories)
let plotyExamplesPath = Path.Join([| __SOURCE_DIRECTORY__; "docs"; "Plotly"; "Examples"; |])
let plotlyMdsPath = Path.Join([| __SOURCE_DIRECTORY__; "public"; "Plotly"; "Examples"; |])

let plotlyExamplePaths = Directory.EnumerateFiles(plotyExamplesPath, "*.fs", SearchOption.AllDirectories) |> Seq.sort
let plotlyMdPaths = Directory.EnumerateFiles(plotlyMdsPath, "*.md", SearchOption.AllDirectories) |> Seq.sort

let plotlyExamples =
    [ for plotlyExamplePath in plotlyExamplePaths do
        let fileInfo = FileInfo(plotlyExamplePath)

        fileInfo
    ] |> List.sortBy (fun fileInfo -> fileInfo.FullName)

let plotlyMds =
    [ for plotlyMdPath in plotlyMdPaths do
        let fileInfo = FileInfo(plotlyMdPath)
        fileInfo
    ] |> List.sortBy (fun fileInfo -> fileInfo.FullName)


let plotlyMdsWithExamples = List.zip plotlyMds plotlyExamples

for (plotlyMd, plotlyExample) in plotlyMdsWithExamples do
    let mdText = plotlyMd.OpenText().ReadToEnd()
    let mdLines = mdText.Split([|'\n'|])
    let fsText = plotlyExample.OpenText().ReadToEnd()
    let fsLines = fsText.Split([|'\n'|])

    let fsharpCodeIndex = mdLines |> Array.findIndex _.Contains("```fsharp")

    let updatedMdLines = [
        yield! mdLines[0 .. fsharpCodeIndex]
        yield! fsLines
        "```"
        ""
    ]

    let updatedMdText = updatedMdLines |> String.concat "\n"

    File.WriteAllText(plotlyMd.FullName, updatedMdText)

// for plotlyMd in plotlyMds do
//     let plotlyMdPath = plotlyMd.FullName

//     let mdText = File.ReadAllText(plotlyMdPath)

//     let mdLines = mdText.Split([|'\n'|], StringSplitOptions.RemoveEmptyEntries)

//     let mdLinesWithFSharp = mdLines |> Array.filter (fun line -> line.Contains("```fsharp"))
//     let fsharpCodeIndex = mdLines |> Array.findIndex _.Contains("```fsharp")

//     printfn $"{plotlyMd.FullName}"


