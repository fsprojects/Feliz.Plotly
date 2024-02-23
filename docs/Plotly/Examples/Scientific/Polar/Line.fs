[<RequireQualifiedAccess>]
module Samples.Polar.Line

open Fable.SimpleHttp
open Feliz
open Feliz.Plotly

type PolarData =
    { Headers: string []
      Y: int []
      X: float []
      X2: float []
      X3: float []
      X4: float []
      X5: float [] }
    member this.AddDataSet (data: string []) : PolarData =
        { this with
            Y  = [| yield! this.Y; (data[0] |> int) |]
            X  = [| yield! this.X; (data[1] |> float) |]
            X2 = [| yield! this.X2; (data[2] |> float) |]
            X3 = [| yield! this.X3; (data[3] |> float) |]
            X4 = [| yield! this.X4; (data[4] |> float) |]
            X5 = [| yield! this.X5; (data[5] |> float) |]
        }

module PolarData =
    let empty : PolarData =
        { Headers = [||]
          Y = [||]
          X = [||]
          X2 = [||]
          X3 = [||]
          X4 = [||]
          X5 = [||] }

let render (data: PolarData) : ReactElement =
    Plotly.plot [
        plot.traces [
            traces.scatterpolar [
                scatterpolar.r data.X
                scatterpolar.theta data.Y
                scatterpolar.mode.lines
                scatterpolar.name "Figure8"
                scatterpolar.line [
                    line.shape.spline
                    line.color color.peru
                ]
            ]
            traces.scatterpolar [
                scatterpolar.r data.X2
                scatterpolar.theta data.Y
                scatterpolar.mode.lines
                scatterpolar.name "Cardioid"
                scatterpolar.line [
                    line.shape.spline
                    line.color color.darkViolet
                ]
            ]
            traces.scatterpolar [
                scatterpolar.r data.X3
                scatterpolar.theta data.Y
                scatterpolar.mode.lines
                scatterpolar.name "Hypercardioid"
                scatterpolar.line [
                    line.shape.spline
                    line.color color.deepSkyBlue
                ]
            ]
            traces.scatterpolar [
                scatterpolar.r data.X4
                scatterpolar.theta data.Y
                scatterpolar.mode.lines
                scatterpolar.name "Subcardioid"
                scatterpolar.line [
                    line.shape.spline
                    line.color color.orangeRed
                ]
            ]
            traces.scatterpolar [
                scatterpolar.r data.X5
                scatterpolar.theta data.Y
                scatterpolar.mode.lines
                scatterpolar.name "Supercardioid"
                scatterpolar.line [
                    line.shape.spline
                    line.color color.green
                ]
            ]
        ]
        plot.layout [
            layout.title [
                title.text "Mic Patterns"
            ]
            layout.font [
                font.family "Arial, sans-serif"
                font.size 12
                font.color "#000"
            ]
            layout.showlegend true
            layout.polar [
                polar.radialaxis [
                    radialaxis.range [ 0.; 1.1 ]
                ]
            ]
        ]
    ]

[<ReactComponent>]
let Chart (centeredSpinner: ReactElement) : ReactElement =
    let isLoading, setLoading = React.useState false
    let error, setError = React.useState<Option<string>> None
    let content, setContent = React.useState PolarData.empty
    let path = "https://raw.githubusercontent.com/plotly/datasets/master/polar_dataset.csv"

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
                |> Array.fold (fun (state: PolarData) (values: string []) -> state.AddDataSet values) content
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
