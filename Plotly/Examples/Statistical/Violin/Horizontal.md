# Feliz.Plotly - Violin Plot

Taken from [Plotly - Violin Plot](https://plot.ly/javascript/violin/)

```fsharp:plotly-chart-violin-horizontal
[<RequireQualifiedAccess>]
module Samples.Violin.Horizontal

open Fable.Core
open Fable.SimpleHttp
open Feliz
open Feliz.Plotly

type ViolinData =
    { Headers: string []
      TotalBill: float []
      Tip: float []
      Sex: string []
      Smoker: string []
      Day: string []
      Time: string []
      Size: int [] }

    member this.AddDataSet (data: string []) : ViolinData =
        { this with
            TotalBill = [| yield! this.TotalBill; (data[0] |> float) |]
            Tip = [| yield! this.Tip; (data[1] |> float) |]
            Sex = [| yield! this.Sex; data[2] |]
            Smoker = [| yield! this.Smoker; data[3] |]
            Day = [| yield! this.Day; data[4] |]
            Time = [| yield! this.Time; data[5] |]
            Size = [| yield! this.Size; (data[6] |> int) |]
        }

module ViolinData =
    let empty : ViolinData =
        { Headers = [||]
          TotalBill = [||]
          Tip = [||]
          Sex = [||]
          Smoker = [||]
          Day = [||]
          Time = [||]
          Size = [||] }

let render (data: ViolinData) : ReactElement =
    Plotly.plot [
        plot.traces [
            traces.violin [
                violin.x data.TotalBill
                violin.points.false'
                violin.box [
                    box.visible.true'
                    box.boxpoints.false'
                ]
                violin.line [
                    line.color color.black
                ]
                violin.fillcolor "#8dd3c7"
                violin.opacity 0.6
                violin.meanline [
                    meanline.visible true
                ]
                violin.x0 "Total Bill"
            ]
        ]
        plot.layout [
            layout.yaxis [
                yaxis.zeroline false
            ]
        ]
    ]

[<ReactComponent>]
let Chart (centeredSpinner: ReactElement) : ReactElement =
    let isLoading, setLoading = React.useState false
    let error, setError = React.useState<Option<string>> None
    let content, setContent = React.useState ViolinData.empty
    let path = "https://raw.githubusercontent.com/plotly/datasets/master/violin_data.csv"

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
                |> Array.fold (fun (state: ViolinData) (values: string []) -> state.AddDataSet values) content
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
