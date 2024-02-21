# Feliz.Plotly [![Nuget](https://img.shields.io/nuget/v/Feliz.Plotly.svg?maxAge=0&colorB=brightgreen)](https://www.nuget.org/packages/Feliz.Plotly)

Fable bindings for [plotly.js](https://github.com/plotly/plotly.js) and [react-plotly.js](https://github.com/plotly/react-plotly.js) with [Feliz](https://github.com/Zaid-Ajaj/Feliz) style api for use within React applications. This repo continues [Shmew's](https://github.com/Shmew/) excellent work, forked from [here](https://github.com/Shmew/Feliz.Plotly).

Lets you build visualizations in an easy, discoverable, and safe fashion.

See the full documentation with live examples [here](https://fsprojects.github.io/Feliz.Plotly/).

A quick look:

```fs
open Feliz
open Feliz.Plotly

Plotly.plot [
    plot.traces [
        traces.scatter [
            scatter.x [ 1; 2; 3; 4 ]
            scatter.y [ 10; 15; 13; 17 ]
            scatter.mode.markers
        ]
        traces.scatter [
            scatter.x [ 2; 3; 4; 5 ]
            scatter.y [ 16; 5; 11; 9 ]
            scatter.mode.lines
        ]
        traces.scatter [
            scatter.x [ 1; 2; 3; 4 ]
            scatter.y [ 12; 9; 15; 12 ]
            scatter.mode [
                scatter.mode.lines
                scatter.mode.markers
            ]
        ]
    ]
]
```

## Architecture / Code Layout

This repo has three main projects:

1. `Feliz.Generator.Plotly` - Used to generate the `Feliz.Plotly` project.
2. `Feliz.Plotly` - The generated project resulting from running the generator
3. `App` - This is the application used to display documentation for `Feliz.Plotly`.

## Development / Contributing

This repository makes extensive use of `vscode`'s development containers feature. You can use this with Github codespaces to boot up a development environment in your browser.