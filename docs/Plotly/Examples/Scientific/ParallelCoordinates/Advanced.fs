[<RequireQualifiedAccess>]
module Samples.ParallelCoordinates.Advanced

open Fable.SimpleHttp
open Feliz
open Feliz.Plotly

type PlotData =
    { Headers: string []
      ColorValue: int []
      BlockHeight: int []
      BlockWidth: int []
      CycMaterial: float []
      BlockMaterial: int []
      TotalWeight: int []
      AssemblyPW: int []
      HstW: int []
      MinHW: int []
      MinWD: int []
      RFBlock: int [] }
    member this.AddDataSet (data: string []) : PlotData =
        { this with
            ColorValue    = [| yield! this.ColorValue; (data[0] |> int) |]
            BlockHeight   = [| yield! this.BlockHeight; (data[1] |> int) |]
            BlockWidth    = [| yield! this.BlockWidth; (data[2] |> int) |]
            CycMaterial   = [| yield! this.CycMaterial; (data[3] |> float) |]
            BlockMaterial = [| yield! this.BlockMaterial; (data[4] |> int) |]
            TotalWeight = [| yield! this.TotalWeight; (data[5] |> int) |]
            AssemblyPW  = [| yield! this.AssemblyPW; (data[6] |> int) |]
            HstW  = [| yield! this.HstW; (data[7] |> int) |]
            MinHW = [| yield! this.MinHW; (data[8] |> int) |]
            MinWD = [| yield! this.MinWD; (data[9] |> int) |]
            RFBlock = [| yield! this.RFBlock; (data[10] |> int) |]
        }

module PlotData =
    let empty : PlotData =
        { Headers = [||]
          ColorValue = [||]
          BlockHeight = [||]
          BlockWidth = [||]
          CycMaterial = [||]
          BlockMaterial = [||]
          TotalWeight = [||]
          AssemblyPW = [||]
          HstW = [||]
          MinHW = [||]
          MinWD = [||]
          RFBlock = [||] }

let render (data: PlotData) : ReactElement =
    Plotly.plot [
        plot.traces [
            traces.parcoords [
                parcoords.line [
                    line.showscale true
                    line.reversescale true
                    line.color data.ColorValue
                    line.colorscale color.colorscale.jet
                    line.cmin -4000
                    line.cmax -100
                ]
                parcoords.dimensions [
                    dimensions.dimension [
                        dimension.constraintrange [ 100000; 150000 ]
                        dimension.range [ 32000 ; 227900 ]
                        dimension.label "Block height"
                        dimension.values data.BlockHeight
                    ]
                    dimensions.dimension [
                        dimension.range [ 0; 700000 ]
                        dimension.label "Block width"
                        dimension.values data.BlockWidth
                    ]
                    dimensions.dimension [
                        dimension.tickvals [ 0.; 0.5; 1.; 2.; 3. ]
                        dimension.ticktext [ "A"; "AB"; "B"; "Y"; "Z" ]
                        dimension.label "Cylinder material"
                        dimension.values data.CycMaterial
                    ]
                    dimensions.dimension [
                        dimension.tickvals [ 0 .. 3 ]
                        dimension.range [ -1; 4 ]
                        dimension.label "Block material"
                        dimension.values data.BlockMaterial
                    ]
                    dimensions.dimension [
                        dimension.range [ 134; 3154 ]
                        dimension.label "Total weight"
                        dimension.visible true
                        dimension.values data.TotalWeight
                    ]
                    dimensions.dimension [
                        dimension.range [ 9; 19984 ]
                        dimension.label "Assembly penalty weight"
                        dimension.values data.AssemblyPW
                    ]
                    dimensions.dimension [
                        dimension.range [ 49000; 568000 ]
                        dimension.label "Height st width"
                        dimension.values data.HstW
                    ]
                    dimensions.dimension [
                        dimension.range [ -28000; 196430 ]
                        dimension.label "Min height width"
                        dimension.values data.MinHW
                    ]
                    dimensions.dimension [
                        dimension.range [ 98453; 501789 ]
                        dimension.label "Min width diameter"
                        dimension.values data.MinWD
                    ]
                    dimensions.dimension [
                        dimension.range [ 1417; 107154 ]
                        dimension.label "RF Block"
                        dimension.values data.RFBlock
                    ]
                ]
            ]
        ]
        plot.layout [
            layout.width 1300
        ]
    ]

[<ReactComponent>]
let Chart (centeredSpinner: ReactElement) : ReactElement =
    let isLoading, setLoading = React.useState false
    let error, setError = React.useState<Option<string>> None
    let content, setContent = React.useState PlotData.empty
    let path = "https://raw.githubusercontent.com/bcdunbar/datasets/master/parcoords_data.csv"

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
