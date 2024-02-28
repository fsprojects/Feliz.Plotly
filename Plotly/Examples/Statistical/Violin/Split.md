# Feliz.Plotly - Violin Plot

Taken from [Plotly - Violin Plot](https://plot.ly/javascript/violin/)

```fsharp:plotly-chart-violin-split
[<RequireQualifiedAccess>]
module Samples.Violin.Split

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
                violin.x data.Day
                violin.y data.TotalBill
                violin.legendgroup "Yes"
                violin.scalegroup "Yes"
                violin.name "Yes"
                violin.side.negative
                violin.box [
                    box.visible.true'
                ]
                violin.line [
                    line.color color.blue
                    line.width 2
                ]
                violin.meanline [
                    meanline.visible true
                ]
            ]
            traces.violin [
                violin.x data.Day
                violin.y data.TotalBill
                violin.legendgroup "No"
                violin.scalegroup "No"
                violin.name "No"
                violin.side.positive
                violin.box [
                    box.visible.true'
                ]
                violin.line [
                    line.color color.green
                    line.width 2
                ]
                violin.meanline [
                    meanline.visible true
                ]
            ]
        ]
        plot.layout [
            layout.title [
                title.text "Split Violin Plot"
            ]
            layout.xaxis [
                xaxis.range [ -1; 4 ]
            ]
            layout.yaxis [
                yaxis.zeroline false
            ]
            layout.violingap 0
            layout.violingroupgap 0
            layout.violinmode.overlay
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
                setError(Some (sprintf "Status %d: could not load %s" statusCode path))
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
