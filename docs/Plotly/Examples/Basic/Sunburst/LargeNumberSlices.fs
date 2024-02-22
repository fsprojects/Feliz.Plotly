[<RequireQualifiedAccess>]
module Samples.Sunburst.LargeNumberSlices

open Fable.SimpleHttp
open Feliz
open Feliz.Plotly

type CoffeeData =
    { Ids: string []
      Labels: string []
      Parents: string [] }
    member this.AddDataSet (data: string []) : CoffeeData =
        { this with
            Ids     = [| yield! this.Ids;     data[0] |]
            Labels  = [| yield! this.Labels;  data[1] |]
            Parents = [| yield! this.Parents; data[2] |] }

module CoffeeData =
    let empty : CoffeeData =
        { Ids = [||]
          Labels = [||]
          Parents = [||] }

let render (data: CoffeeData) : ReactElement =
    Plotly.plot [
        plot.traces [
            traces.sunburst [
                sunburst.maxdepth 3
                sunburst.ids data.Ids
                sunburst.labels data.Labels
                sunburst.parents data.Parents
            ]
        ]
        plot.layout [
            layout.margin [
                margin.l 0
                margin.r 0
                margin.b 0
                margin.t 0
            ]
            layout.sunburstcolorway [
                "#636efa"
                "#EF553B"
                "#00cc96"
                "#ab63fa"
                "#19d3f3"
                "#e763fa"
                "#FECB52"
                "#FFA15A"
                "#FF6692"
                "#B6E880"
            ]
            layout.extendsunburstcolors true
        ]
    ]

[<ReactComponent>]
let chart (centeredSpinner: ReactElement) : ReactElement =
    let isLoading, setLoading = React.useState false
    let error, setError = React.useState<Option<string>> None
    let content, setContent = React.useState CoffeeData.empty
    let path = "https://raw.githubusercontent.com/plotly/datasets/master/coffee-flavors.csv"

    let loadDataset() =
        setLoading(true)
        async {
            let! (statusCode, responseText) = Http.get path
            setLoading(false)
            if statusCode = 200 then
                responseText.Trim().Split('\n')
                |> Array.map _.Split(',')
                |> Array.tail
                |> Array.fold (fun (state: CoffeeData) (values: string []) -> state.AddDataSet values) content
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
