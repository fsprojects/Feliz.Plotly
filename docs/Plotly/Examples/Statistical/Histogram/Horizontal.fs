[<RequireQualifiedAccess>]
module Samples.Histogram.Horizontal

open Feliz
open Feliz.Plotly
open System

let rng = Random()

let chart () : ReactElement =
    Plotly.plot [
        plot.traces [
            traces.histogram [
                histogram.y ([ 0 .. 499 ] |> List.map (fun _ -> rng.NextDouble()))
                histogram.marker [
                    marker.color color.pink
                ]
            ]
        ]
    ]
