[<RequireQualifiedAccess>]
module Samples.Locales.ModuleRegistration

open Feliz
open Feliz.Plotly
open Zanaptak.TypedCssClasses

type Bulma = CssClasses<"https://cdn.jsdelivr.net/npm/bulma@0.9.4/css/bulma.min.css", Naming.PascalCase>
type FA = CssClasses<"https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css", Naming.PascalCase>

[<ReactComponent>]
let Chart () : ReactElement =
    let toggledLang,setToggledLang = React.useState false

    Plotly.useLocale "example" [
        locale.format [
            format.days [
                "Sunday-Changed"
                "Monday-Changed"
                "Tuesday-Changed"
                "Wednesday-Changed"
                "Thursday-Changed"
                "Friday-Changed"
                "Saturday-Changed"
            ]
        ]
    ]

    Plotly.useLocales [
        Locales.de
        Locales.fr
    ]

    React.fragment [
        Plotly.plot [
            plot.traces [
                traces.bar [
                    bar.x [ "giraffes"; "orangutans"; "monkeys" ]
                    bar.y [ 20; 14; 23 ]
                ]
            ]
            plot.config [
                if toggledLang then config.locale.fr
                else config.locale.de
            ]
        ]
        Html.div [
            prop.className Bulma.Control
            prop.style [
                style.paddingLeft (length.em 8)
                style.paddingBottom (length.em 1)
            ]
            prop.children [
                Html.button [
                    prop.classes [ Bulma.Button; if not toggledLang then yield! [ Bulma.IsActive; Bulma.HasBackgroundPrimary; Bulma.HasTextWhite ] ]
                    prop.onClick <| fun _ -> setToggledLang false
                    prop.text "German"
                ]
                Html.button [
                    prop.classes [ Bulma.Button; if toggledLang then yield! [ Bulma.IsActive; Bulma.HasBackgroundPrimary; Bulma.HasTextWhite ] ]
                    prop.onClick <| fun _ -> setToggledLang true
                    prop.text "French"
                ]
            ]
        ]
    ]
