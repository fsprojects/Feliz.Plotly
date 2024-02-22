[<RequireQualifiedAccess>]
module Samples.Events.DisableZoom

open Fable.SimpleHttp
open Feliz
open Feliz.Plotly
open System

type AppleStocks =
    { Headers: string []
      Date: DateTime []
      Open: float []
      High: float []
      Low: float []
      Close: float []
      Volume: float []
      Adjusted: float []
      Down: float []
      MovingAvg: float []
      Up: float []
      Direction: string [] }

    member this.AddDataSet (data: string []) : AppleStocks =
        { this with
            Date = [| yield! this.Date; (data[0] |> DateTime.Parse) |]
            Open = [| yield! this.Open; (data[1] |> float) |]
            High = [| yield! this.High; (data[2] |> float) |]
            Low =  [| yield! this.Low;  (data[3] |> float) |]
            Close =  [| yield! this.Close;  (data[4] |> float) |]
            Volume = [| yield! this.Volume; (data[5] |> float) |]
            Adjusted =  [| yield! this.Adjusted;  (data[6] |> float) |]
            Down =      [| yield! this.Down;      (data[7] |> float) |]
            MovingAvg = [| yield! this.MovingAvg; (data[8] |> float) |]
            Up =        [| yield! this.Up;        (data[9] |> float) |]
            Direction = [| yield! this.Direction; data[10] |] }

module AppleStocks =
    let empty : AppleStocks =
        { Headers = [||]
          Date = [||]
          Open = [||]
          High = [||]
          Low = [||]
          Close = [||]
          Volume = [||]
          Adjusted = [||]
          Down = [||]
          MovingAvg = [||]
          Up = [||]
          Direction = [||] }

let render (data: AppleStocks) : ReactElement =
    Plotly.plot [
        plot.traces [
            traces.scatter [
                scatter.mode.lines
                scatter.name "AAPL High"
                scatter.x data.Date
                scatter.y data.High
                scatter.line [
                    line.color "#17BECF"
                ]
            ]
            traces.scatter [
                scatter.mode.lines
                scatter.name "AAPL Low"
                scatter.x data.Date
                scatter.y data.Low
                scatter.line [
                    line.color "#7F7F7F"
                ]
            ]
        ]
        plot.layout [
            layout.title [
                title.text "Zoom disabled"
            ]
            layout.xaxis [
                xaxis.fixedrange true // Disables zoom on x-axis
            ]
            layout.yaxis [
                yaxis.fixedrange true // Disables zoom on y-axis
            ]
        ]
    ]

[<ReactComponent>]
let chart (centeredSpinner: ReactElement) : ReactElement =
    let isLoading, setLoading = React.useState false
    let error, setError = React.useState<Option<string>> None
    let content, setContent = React.useState AppleStocks.empty
    let path = "https://raw.githubusercontent.com/plotly/datasets/master/finance-charts-apple.csv"

    let loadDataset () =
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
                |> Array.fold (fun (state: AppleStocks) (values: string []) -> state.AddDataSet values) content
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