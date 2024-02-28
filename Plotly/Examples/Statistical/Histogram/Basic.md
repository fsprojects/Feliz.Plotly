# Feliz.Plotly - Histograms

Taken from [Plotly - Histograms](https://plot.ly/javascript/histograms/)

```fsharp:plotly-chart-histogram-basic
[<RequireQualifiedAccess>]
module Samples.Histogram.Basic

open Feliz
open Feliz.Plotly
open System

let rng = Random()

let chart () : ReactElement =
    Plotly.plot [
        plot.traces [
            traces.histogram [
                histogram.x ([ 0 .. 499 ] |> List.map (fun _ -> rng.NextDouble()))
            ]
        ]
    ]

```
