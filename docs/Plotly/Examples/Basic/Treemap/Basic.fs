﻿[<RequireQualifiedAccess>]
module Samples.Treemap.Basic

open Feliz
open Feliz.Plotly

let chart () : ReactElement =
    Plotly.plot [
        plot.traces [
            traces.treemap [
                treemap.labels [ "Eve"; "Cain"; "Seth"; "Enos"; "Noam"; "Abel"; "Awan"; "Enoch"; "Azura" ]
                treemap.parents [ ""; "Eve"; "Eve"; "Seth"; "Seth"; "Eve"; "Eve"; "Awan"; "Eve" ]
            ]
        ]
    ]
