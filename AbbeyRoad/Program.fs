module AbbeyRoad.Program

open System
open System.IO
open System.Threading

(* coordinates
top left black : 310, 200, tr black: 332, 200
tr white: 347, 201, tr black 361, 200
tr white:  379, 202, tr black: 394, 202
tr white: 410, 203, tr black: 423, 204
tr white: 439, 205, tr black: 455, 205
tr white: 473, 206, tr black: 489, 207
tr white: 506, 209 tr black: 533, 209

bottom left black: 275, 214, bottom right black: 296, 214
br black:
 *)

(*

at startup need unpressed note images for comparison:
- by hand work out x y coords for each polygon for each note, then create example images for each bar from a screenshot of empty crossing, or maybe programatically create black and white image using only coords from real image

-> then extract polygons of interest
--> either ROI or sub rectangles or deep copied sub rectangles, extract box containing each white bar
--- select polygons within each rectangle
--> possibly by creating black mask image, then filling white contour polygon on region of interest, then or-ing with original rectangle

Function to calculate active polygons:


-> play sound matching pressed note

-> iron out issues in looping version of code

-> add various synth sound options in addition to piano

*)

type Note =
    | A
    | B

type Point =
    { X: int
      Y: int }

module BrowserAutomation = 
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

module ImageProcessing =    
    open OpenCV.Net
    open System.Drawing      

    let saveImage mat =
        let path = Path.Combine(@"C:\Users\James\Documents\tmp", DateTime.Now.ToString("MM-dd-HH-mm-ss") + ".png")
        CV.SaveImage(path, mat)

    let cropWebcamImage (screenshotBytes:byte[]) (iframeRect:Rectangle) =
        let buffer = Mat.FromArray(screenshotBytes)
        let uncropped = CV.DecodeImageM(buffer, LoadImageFlags.Grayscale)
        uncropped.GetSubRect(Rect(iframeRect.Left, iframeRect.Top, iframeRect.Width, iframeRect.Height))

    //assumes polygon slants down and right
    let createMaskImage (topLeft:Point) (topRight:Point) (bottomLeft:Point) (bottomRight:Point) =
        let mask = Mat.Zeros(topLeft.Y - bottomRight.Y, bottomRight.X - topLeft.X, Depth.U8, 3)
        CV.DrawContours(mask, contour, black, white, maxLevel, -1, LineFlags.Something)
        mask

    //assumes polygon slants down and right
    let combineWithMaskImage (wholeImage:Mat) topLeft topRight bottomLeft bottomRight
        let mask = createMaskImage topLeft topRight bottomLeft bottomRight
        let regionOfInterest = wholeImage.GetSubRect(Rect())
        mask & regionOfInterest        

    (*
    different comparison ideas:
    1. simple euclidian distance
    2. simple histogram comparison (4 different comparisons included in OpenCV) - though very sensitive to lighting changes
    3. histogram comparison, combined with adjusting brightness of example empty images to match current time of day & weather
    4. don't compare to example images at all, but rather just test entropy of histogram for each polygon - high entropy means the 
    plain colour of the background is likely mixed up with an object on top of it
    *)
    let getActivePolygons (screenshotBytes:byte[]) (iframeRect:Rectangle) =
        let webcamImage = cropWebcamImage screenshotBytes iframeRect
        []

module SoundPlayer =
    open System.Media

    let audioPath file =
        Path.Combine("audio", file) + ".wav"

    let piano =
        function
        | A -> audioPath "piano_a"
        | B -> audioPath "piano_b"

    let synth1 =
        function
        | A -> audioPath "synth1_a"
        | B -> audioPath "synth1_b"

    let soundplayer = new SoundPlayer()

    let playNotes notes =
        ()

module PianoLogic =
    let getNotes activePolygons previousActivePolygons =
        []

let rec loop previousActivePolygons iframeRect =
    Thread.Sleep(40)
    let screenshot = BrowserAutomation.screenshot()    
    let activePolygons = ImageProcessing.getActivePolygons screenshot iframeRect
    let notes = PianoLogic.getNotes activePolygons previousActivePolygons
    SoundPlayer.playNotes notes    
    //loop activePolygons iframeRect

[<EntryPoint>]
let main argv = 
    BrowserAutomation.start ()
    let iframeRect = BrowserAutomation.iframeRect()
    loop [] iframeRect
    0
