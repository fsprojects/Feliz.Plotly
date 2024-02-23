[<RequireQualifiedAccess>]
module Samples.Histogram.Normalized

open Feliz
open Feliz.Plotly
open System

let rng = Random()

let chart () : ReactElement =
    Plotly.plot [
        plot.traces [
            traces.histogram [
                histogram.x ([ 0 .. 499 ] |> List.map (fun _ -> rng.NextDouble()))
                histogram.histnorm.probability
                histogram.marker [
                    marker.color (color.rgb(255, 255, 100))
                ]
            ]
        ]
    ]
