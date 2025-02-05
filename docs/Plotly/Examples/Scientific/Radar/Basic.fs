[<RequireQualifiedAccess>]
module Samples.Radar.Basic

open Feliz
open Feliz.Plotly

let chart () : ReactElement =
    Plotly.plot [
        plot.traces [
            traces.scatterpolar [
                scatterpolar.r [ 39; 28; 8; 7; 28; 39 ]
                scatterpolar.theta [ "A"; "B"; "C"; "D"; "E"; "A" ]
                scatterpolar.fill.toself
            ]
        ]
        plot.layout [
            layout.polar [
                polar.radialaxis [
                    radialaxis.visible true
                    radialaxis.range [ 0; 50 ]
                ]
            ]
            layout.showlegend false
        ]
    ]
