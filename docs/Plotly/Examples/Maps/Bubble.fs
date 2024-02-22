[<RequireQualifiedAccess>]
module Samples.Maps.Bubble

open Fable.SimpleHttp
open Feliz
open Feliz.Plotly

type USCities =
    { Headers: string []
      Names: string []
      Population: int []
      Lat: float []
      Long: float [] }

    member this.AddDataSet (data: string []) : USCities =
        { this with
            Names      = [| yield! this.Names; data[0] |]
            Population = [| yield! this.Population; (data[1] |> int) |]
            Lat  = [| yield! this.Lat; (data[2] |> float) |]
            Long = [| yield! this.Lat; (data[2] |> float) |]
        }

module USCities =
    let empty =
        { Headers = [||]
          Names = [||]
          Population = [||]
          Lat = [||]
          Long = [||] }

[<ReactMemoComponent>]
let render (data: USCities) : ReactElement =

    let hoverText, bubbleSize =
        React.useMemo ((fun () ->
            List.foldBack2 (fun name pop (hoverText, bubbleSize) ->
                ($"{name} pop: {pop}"::hoverText, (pop / 50000)::bubbleSize)
            ) (data.Names |> List.ofArray) (data.Population |> List.ofArray) ([], [])
        ), [| data |])

    Plotly.plot [
        plot.traces [
            traces.scattergeo [
                scattergeo.locationmode.USAStates
                scattergeo.lat data.Lat
                scattergeo.lon data.Long
                scattergeo.hoverinfo.text
                scattergeo.text hoverText
                scattergeo.marker [
                    marker.size bubbleSize
                    marker.line [
                        line.color color.black
                        line.width 2
                    ]
                ]
            ]
        ]
        plot.layout [
            layout.title [
                title.text "2014 US City Population"
            ]
            layout.showlegend false
            layout.geo [
                geo.scope.usa
                geo.projection [
                    projection.type'.albersUsa
                ]
                geo.showland true
                geo.landcolor (color.rgb(217, 217, 217))

                geo.subunitwidth 1
                geo.subunitcolor (color.rgb(255, 255, 255))

                geo.countrywidth 1
                geo.countrycolor (color.rgb(255, 255, 255))
            ]
        ]
    ]

[<ReactComponent>]
let chart (centeredSpinner: ReactElement) : ReactElement =
    let isLoading, setLoading = React.useState false
    let error, setError = React.useState<Option<string>> None
    let content, setContent = React.useState USCities.empty
    let path = "https://raw.githubusercontent.com/plotly/datasets/master/2014_us_cities.csv"

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
                |> Array.fold (fun (state: USCities) (values: string []) -> state.AddDataSet values) content
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
