module AbbeyRoad.BrowserAutomation

open canopy
open OpenQA.Selenium
open System.Drawing

let flashElement () =
    element ".earthcam-embed-container"

let iframeRect () =
    let iframe = element "iframe"
    let width = iframe.Size.Width
    let height = iframe.Size.Height
    let x = iframe.Location.X
    let y = iframe.Location.Y

    Rectangle(x, y, width, height)

let start () =
    start firefox
    url "http://www.abbeyroad.com/crossing"

let screenshot () =
    (browser :?> ITakesScreenshot).GetScreenshot().AsByteArray 

