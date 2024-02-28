# Feliz.Plotly - Splom

Taken from [Plotly - Splom](https://plot.ly/javascript/splom/)

```fsharp:plotly-chart-splom-iris
[<RequireQualifiedAccess>]
module Samples.Splom.Iris

open Fable.SimpleHttp
open Feliz
open Feliz.Plotly

type IrisData =
    { Headers: string []
      SepalLength: float []
      SepalWidth: float []
      PetalLength: float []
      PetalWidth: float []
      Class: string [] }
    member this.AddDataSet (data: string []) : IrisData =
        { this with
            SepalLength = [| yield! this.SepalLength; (data[0] |> float) |]
            SepalWidth =  [| yield! this.SepalWidth; (data[1] |> float) |]
            PetalLength = [| yield! this.PetalLength; (data[2] |> float) |]
            PetalWidth =  [| yield! this.PetalWidth; (data[3] |> float) |]
            Class =       [| yield! this.Class; data[4] |]
        }

module IrisData =
    let empty : IrisData =
        { Headers = [||]
          SepalLength = [||]
          SepalWidth = [||]
          PetalLength = [||]
          PetalWidth = [||]
          Class = [||] }

let render (data: IrisData) : ReactElement =
    let plotColors =
        data.Class
        |> Array.map (fun c ->
            match c.Trim() with
            | "Iris-setosa" -> 0.
            | "Iris-versicolor" -> 0.5
            | "Iris-virginica" -> 1.
            | _ -> failwith "Check your data")

    let plotColorcale =
        [ ["0.0"; "#19d3f3"]
          ["0.333"; "#19d3f3"]
          ["0.333"; "#e763fa"]
          ["0.666"; "#e763fa"]
          ["0.666"; "#636efa"]
          ["1.0"; "#636efa"] ]

    let layoutXAxis =
        [ xaxis.showline false
          xaxis.zeroline false
          xaxis.gridcolor "#ffff"
          xaxis.ticklen 4 ]

    let layoutYAxis =
        [ yaxis.showline false
          yaxis.zeroline false
          yaxis.gridcolor "#ffff"
          yaxis.ticklen 4 ]

    Plotly.plot [
        plot.traces [
            traces.splom [
                splom.dimensions [
                    dimensions.dimension [
                        dimension.label "sepal length"
                        dimension.values data.SepalLength
                    ]
                    dimensions.dimension [
                        dimension.label "sepal width"
                        dimension.values data.SepalWidth
                    ]
                    dimensions.dimension [
                        dimension.label "petal length"
                        dimension.values data.PetalLength
                    ]
                    dimensions.dimension [
                        dimension.label "petal width"
                        dimension.values data.PetalWidth
                    ]
                ]
                splom.text data.Class
                splom.marker [
                    marker.color plotColors
                    marker.colorscale plotColorcale
                    marker.size 7
                    marker.line [
                        line.color color.white
                        line.width 0.5
                    ]
                ]
            ]
        ]
        plot.layout [
            layout.title [
                title.text "Iris Data set"
            ]
            layout.height 800
            layout.width 800
            layout.autosize false
            layout.hovermode.closest
            layout.dragmode.select
            layout.plotBgcolor (color.rgba(240, 240, 240, 0.95))
            layout.xaxis layoutXAxis
            layout.xaxis (2, layoutXAxis)
            layout.xaxis (3, layoutXAxis)
            layout.xaxis (4, layoutXAxis)

            layout.yaxis layoutYAxis
            layout.yaxis (2, layoutYAxis)
            layout.yaxis (3, layoutYAxis)
            layout.yaxis (4, layoutYAxis)
        ]
    ]

[<ReactComponent>]
let Chart (centeredSpinner: ReactElement) : ReactElement =
    let isLoading, setLoading = React.useState false
    let error, setError = React.useState<Option<string>> None
    let content, setContent = React.useState IrisData.empty
    let path = "https://raw.githubusercontent.com/plotly/datasets/master/iris-data.csv"

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
                |> Array.fold (fun (state: IrisData) (values: string []) -> state.AddDataSet values) content
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
