# Feliz.Plotly - Choropleth Maps

Taken from [Plotly - Choropleth Maps](https://plot.ly/javascript/choropleth-maps)

```fsharp:plotly-chart-maps-choropleth
[<RequireQualifiedAccess>]
module Samples.Maps.Choropleth

open Fable.SimpleHttp
open Feliz
open Feliz.Plotly

type Precipitation =
    { Headers: string []
      Location: string []
      Alcohol: float [] }

    member this.AddDataSet (data: string []) : Precipitation =
        { this with
            Location = [| yield! this.Location; data[0] |]
            Alcohol  = [| yield! this.Alcohol; (data[1] |> float) |] }

module Precipitation =
    let empty : Precipitation =
        { Headers = [||]
          Location = [||]
          Alcohol = [||] }

let render (data: Precipitation) : ReactElement =
    Plotly.plot [
        plot.traces [
            traces.choropleth [
                choropleth.locationmode.countryNames
                choropleth.locations data.Location
                choropleth.z data.Alcohol
                choropleth.text data.Location
                choropleth.autocolorscale true
            ]
        ]
        plot.layout [
            layout.title [
                title.text "Pure alcohol consumption<br>among adults (age 15+) in 2010"
            ]
            layout.geo [
                geo.projection [
                    projection.type'.robinson
                ]
            ]
        ]
    ]

[<ReactComponent>]
let Chart (centeredSpinner: ReactElement) : ReactElement =
    let isLoading, setLoading = React.useState false
    let error, setError = React.useState<Option<string>> None
    let content, setContent = React.useState Precipitation.empty
    let path = "https://raw.githubusercontent.com/plotly/datasets/master/2010_alcohol_consumption_by_country.csv"

    let loadDataset() =
        setLoading(true)
        async {
            let! (statusCode, responseText) = Http.get path
            setLoading(false)
            if statusCode = 200 then
                let fullData =
                    responseText.Trim().Split('\n')
                    |> Array.map  _.Split(',')

                fullData
                |> Array.tail
                |> Array.fold (fun (state: Precipitation) (values: string []) -> state.AddDataSet values) content
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
