module AbbeyRoad.BrowserAutomation

open canopy
open OpenQA.Selenium
open System.Drawing

(*
Browser automation code using Canopy/Selenium

Provides functions to take a screenshot of a browser window and work out the location and dimensions of the embedded
iframe within the page  *

Canopy is built on top of Selenium, and this uses almost no Canopy-specific functionality at all
*)

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

