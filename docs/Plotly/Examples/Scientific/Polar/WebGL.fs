[<RequireQualifiedAccess>]
module Samples.Polar.WebGL

open Fable.SimpleHttp
open Feliz
open Feliz.Plotly

type PlotData =
    { Headers: string []
      TrialR : float []
      TrialT : float []
      Trial2R : float []
      Trial2T : float []
      Trial3R : float []
      Trial3T : float []
      Trial4R : float []
      Trial4T : float []
      Trial5R : float []
      Trial5T : float []
      Trial6R : float []
      Trial6T : float [] }
    member this.AddDataSet (data: string []) : PlotData =
        { this with
            TrialR  = [| yield! this.TrialR; (data[0] |> float) |]
            TrialT  = [| yield! this.TrialT; (data[1] |> float) |]
            Trial2R = [| yield! this.Trial2R; (data[2] |> float) |]
            Trial2T = [| yield! this.Trial2T; (data[3] |> float) |]
            Trial3R = [| yield! this.Trial3R; (data[4] |> float) |]
            Trial3T = [| yield! this.Trial3T; (data[5] |> float) |]
            Trial4R = [| yield! this.Trial4R; (data[6] |> float) |]
            Trial4T = [| yield! this.Trial4T; (data[7] |> float) |]
            Trial5R = [| yield! this.Trial5R; (data[8] |> float) |]
            Trial5T = [| yield! this.Trial5T; (data[9] |> float) |]
            Trial6R = [| yield! this.Trial6R; (data[10] |> float) |]
            Trial6T = [| yield! this.Trial6T; (data[11] |> float) |]
        }

module PlotData =
    let empty : PlotData =
        { Headers = [||]
          TrialR = [||]
          TrialT = [||]
          Trial2R = [||]
          Trial2T = [||]
          Trial3R = [||]
          Trial3T = [||]
          Trial4R = [||]
          Trial4T = [||]
          Trial5R = [||]
          Trial5T = [||]
          Trial6R = [||]
          Trial6T = [||] }

let render (data: PlotData) : ReactElement =
    Plotly.plot [
        plot.traces [
            traces.scatterpolargl [
                scatterpolargl.r data.TrialR
                scatterpolargl.theta data.TrialT
                scatterpolargl.mode.markers
                scatterpolargl.name "Trial 1"
                scatterpolargl.marker [
                    marker.color (color.rgb(27, 158, 119))
                    marker.size 15
                    marker.line [
                        line.color color.white
                    ]
                    marker.opacity 0.7
                ]
            ]
            traces.scatterpolargl [
                scatterpolargl.r data.Trial2R
                scatterpolargl.theta data.Trial2T
                scatterpolargl.mode.markers
                scatterpolargl.name "Trial 2"
                scatterpolargl.marker [
                    marker.color (color.rgb(217, 95, 2))
                    marker.size 20
                    marker.line [
                        line.color color.white
                    ]
                    marker.opacity 0.7
                ]
            ]
            traces.scatterpolargl [
                scatterpolargl.r data.Trial3R
                scatterpolargl.theta data.Trial3T
                scatterpolargl.mode.markers
                scatterpolargl.name "Trial 3"
                scatterpolargl.marker [
                    marker.color (color.rgb(117, 112, 179))
                    marker.size 12
                    marker.line [
                        line.color color.white
                    ]
                    marker.opacity 0.7
                ]
            ]
            traces.scatterpolargl [
                scatterpolargl.r data.Trial4R
                scatterpolargl.theta data.Trial4T
                scatterpolargl.mode.markers
                scatterpolargl.name "Trial 4"
                scatterpolargl.marker [
                    marker.color (color.rgb(231, 41, 138))
                    marker.size 22
                    marker.line [
                        line.color color.white
                    ]
                    marker.opacity 0.7
                ]
            ]
            traces.scatterpolargl [
                scatterpolargl.r data.Trial5R
                scatterpolargl.theta data.Trial5T
                scatterpolargl.mode.markers
                scatterpolargl.name "Trial 5"
                scatterpolargl.marker [
                    marker.color (color.rgb(102, 166, 30))
                    marker.size 19
                    marker.line [
                        line.color color.white
                    ]
                    marker.opacity 0.7
                ]
            ]
            traces.scatterpolargl [
                scatterpolargl.r data.Trial6R
                scatterpolargl.theta data.Trial6T
                scatterpolargl.mode.markers
                scatterpolargl.name "Trial 6"
                scatterpolargl.marker [
                    marker.color (color.rgb(230, 171, 2))
                    marker.size 10
                    marker.line [
                        line.color color.white
                    ]
                    marker.opacity 0.7
                ]
            ]
        ]
        plot.layout [
            layout.title [
                title.text "Hobbs-Pearson Trials"
                title.font [
                    font.size 20
                ]
            ]
            layout.showlegend false
            layout.polar [
                polar.bgcolor (color.rgb(223, 223, 223))
                polar.angularaxis [
                    angularaxis.tickwidth 2
                    angularaxis.linewidth 3
                    angularaxis.layer.belowTraces
                ]
                polar.radialaxis [
                    radialaxis.side.counterclockwise
                    radialaxis.showline true
                    radialaxis.linewidth 2
                    radialaxis.tickwidth 2
                    radialaxis.gridcolor color.white
                    radialaxis.gridwidth 2
                ]
            ]
            layout.paperBgcolor (color.rgb(223, 223, 223))
        ]
    ]

[<ReactComponent>]
let Chart (centeredSpinner: ReactElement) : ReactElement =
    let isLoading, setLoading = React.useState false
    let error, setError = React.useState<Option<string>> None
    let content, setContent = React.useState PlotData.empty
    let path = "https://raw.githubusercontent.com/plotly/datasets/master/hobbs-pearson-trials.csv"

    let loadDataset() =
        setLoading(true)
        async {
            let! (statusCode, responseText) = Http.get path
            setLoading(false)
            if statusCode = 200 then
                let fullData =
                    responseText.Trim().Split('\n')
                    |> Array.map (_.Replace(".00E+05","") >> _.Split(','))

                fullData
                |> Array.tail
                |> Array.fold (fun (state: PlotData) (values: string []) -> state.AddDataSet values) content
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
