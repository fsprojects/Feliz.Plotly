# Feliz.Plotly - Parallel Categories

Taken from [Plotly - Parallel Categories](https://plot.ly/javascript/parallel-categories-diagram/)

```fsharp:plotly-chart-parallelcategories-multicolor
[<RequireQualifiedAccess>]
module Samples.ParallelCategories.MultiColor

open Fable.Core
open Fable.SimpleHttp
open Feliz
open Feliz.Plotly

type TitanicData =
    { Headers: string []
      Survived: int []
      PClass: int []
      Sex: string [] }
    member this.AddDataSet (data: string []) : TitanicData =
        { this with
            Survived = [| yield! this.Survived; (data[1] |> int) |]
            PClass = [| yield! this.PClass; (data[2] |> int) |]
            Sex = [| yield! this.Sex; data[5] |]
        }

module TitanicData =
    let empty : TitanicData =
        { Headers = [||]
          Survived = [||]
          PClass = [||]
          Sex = [||] }

let render (data: TitanicData) : ReactElement =
    Plotly.plot [
        plot.traces [
            traces.parcats [
                parcats.dimensions [
                    dimensions.dimension [
                        dimension.label "Class"
                        dimension.values data.PClass
                        dimension.categoryorder.categoryAscending
                    ]
                    dimensions.dimension [
                        dimension.label "Gender"
                        dimension.values data.Sex
                    ]
                    dimensions.dimension [
                        dimension.label "Outcome"
                        dimension.values data.Survived
                        dimension.categoryarray [ 0; 1 ]
                        dimension.ticktext [ "perished"; "survived" ]
                    ]
                ]
                parcats.line [
                    line.color data.Survived
                    line.colorscale (
                        [ color.lightSteelBlue; color.mediumSeaGreen ]
                        |> color.colorscale.sequential
                    )
                ]
                parcats.hoveron.color
                parcats.hoverinfo [
                    parcats.hoverinfo.count
                    parcats.hoverinfo.probability
                ]
                parcats.labelfont [
                    labelfont.size 14
                ]
                parcats.arrangement.freeform
            ]
        ]
        plot.layout [
            layout.width 600
        ]
    ]

[<ReactComponent>]
let Chart (centeredSpinner: ReactElement) : ReactElement =
    let isLoading, setLoading = React.useState false
    let error, setError = React.useState<Option<string>> None
    let content, setContent = React.useState TitanicData.empty
    let path = "https://raw.githubusercontent.com/plotly/datasets/master/titanic.csv"

    let loadDataset() =
        setLoading(true)
        async {
            let! (statusCode, responseText) = Http.get path
            setLoading(false)
            if statusCode = 200 then
                let fullData =
                    responseText.Trim().Split('\n')
                    |> Array.map _.Split(',')

                fullData
                |> Array.tail
                |> Array.fold (fun (state: TitanicData) (values: string []) -> state.AddDataSet values) content
                |> fun newContent -> { newContent with Headers = fullData |> Array.head }
                |> setContent
                setError(None)
            else
                setError(Some $"Status {statusCode}: could not load {path}")
        }
        |> Async.StartImmediate

    React.useEffect(loadDataset, [| path :> obj |])

    match isLoading, error with
    | true, _ -> centeredSpinner
    | false, None -> render content
    | _, Some error ->
        Html.h1 [
            prop.style [ style.color.crimson ]
            prop.text error
        ]

```
