﻿[<RequireQualifiedAccess>]
module Samples.ThreeDimensional.Surface

open Fable.SimpleHttp
open Feliz
open Feliz.Plotly

let render (data: float [] []) : ReactElement =
    Plotly.plot [
        plot.traces [
            traces.surface [
                surface.z data
            ]
        ]
        plot.layout [
            layout.title "Mt Bruno Elevation"
            layout.autosize false
            layout.width 500
            layout.height 500
            layout.margin [
                margin.l 65
                margin.r 50
                margin.b 65
                margin.t 90
            ]
        ]
    ]

[<ReactComponent>]
let Chart (centeredSpinner: ReactElement) : ReactElement =
    let isLoading, setLoading = React.useState false
    let error, setError = React.useState<Option<string>> None
    let content, setContent = React.useState [||]
    let path = "https://raw.githubusercontent.com/plotly/datasets/master/api_docs/mt_bruno_elevation.csv"

    let loadDataset() =
        setLoading(true)
        async {
            let! (statusCode, responseText) = Http.get path
            setLoading(false)
            if statusCode = 200 then
                let fullData =
                    responseText.Trim().Split('\n')
                    |> Array.map (fun s -> s.Split(','))

                fullData
                |> Array.tail
                |> Array.map (Array.tail >> (Array.map float))
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