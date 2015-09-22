module AbbeyRoad.Program

open System

type Note =
    | Black1
    | White1
    | Black2
    | White2
    | Black3
    | White3
    | Black4
    | White4
    | Black5
    | White5
    | Black6
    | White6
    | Black7

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

//FIXME lots of OpenCV types are IDisposable, possibly there are lots of memory leaks at the moment
module ImageProcessing =   
    open System.IO 
    open OpenCV.Net
    open System.Drawing  
    
    type Point =
        { X: int
          Y: int }    

    let saveMat mat =
        let path = Path.Combine(@"C:\Users\James\Documents\tmp", DateTime.Now.ToString("MM-dd-HH-mm-ss") + ".png")
        CV.SaveImage(path, mat)

    let cropWebcamImage (screenshotBytes:byte[]) (iframeRect:Rectangle) =
        use buffer = Mat.FromArray(screenshotBytes)
        let uncropped = CV.DecodeImageM(buffer, LoadImageFlags.Grayscale)
        uncropped.GetSubRect(Rect(iframeRect.Left, iframeRect.Top, iframeRect.Width, iframeRect.Height))

    let createMaskImage (topLeft:Point) (topRight:Point) (bottomLeft:Point) (bottomRight:Point) =
        let mask = Mat.Zeros(Math.Abs(topLeft.Y - bottomRight.Y), Math.Abs(bottomRight.X - topLeft.X), Depth.U8, 3)
        CV.DrawContours(mask, contours, Scalar(0., 0., 0.), Scalar(255., 255., 255.), 1, -1)
        mask

    let combineWithMaskImage (wholeImage:Mat) topLeft topRight bottomLeft bottomRight
        use mask = createMaskImage topLeft topRight bottomLeft bottomRight
        let regionOfInterest = wholeImage.GetSubRect(Rect())
        mask & regionOfInterest        

    let euclidianDistance () =
        ()

    let histogramEntropy () =
        ()

    let compareHistograms (empty:Histogram) (actual:Histogram) comparisonMethod =
        empty.Compare(actual, comparisonMethod)

    let getHistogram mat =
        let histogram = new Histogram(int dims, int[] sizes, HistogramType type, float[][] ranges = null, bool uniform = true)
        histogram.CalcArrHist()

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

module PianoLogic =
    let getNotes activePolygons previousActivePolygons =
        []

module Main =
    [<Literal>]
    let framePeriod = 10000

    let rec loop previousActivePolygons iframeRect = async {
        let screenshot = BrowserAutomation.screenshot()    
        let activePolygons = ImageProcessing.getActivePolygons screenshot iframeRect
        let notes = PianoLogic.getNotes activePolygons previousActivePolygons
        do! Async.Sleep(framePeriod)
        return! loop activePolygons iframeRect
    }

module WebServer =
    open Suave
    open Suave.Http
    open Suave.Sockets
    open Suave.WebSocket
    open Suave.Web
    open Suave.Http.Files
    open Suave.Sockets.Control

    let giveMusic (webSocket : WebSocket) =
        fun cx -> socket {
      let loop = ref true
      while !loop do
        let! msg = webSocket.read()
        match msg with
        | (Text, data, true) ->
          let str = Utils.UTF8.toString data
          do! webSocket.send Text data true
        | (Ping, _, _) ->
          do! webSocket.send Pong [||] true
        | (Close, _, _) ->
          do! webSocket.send Close [||] true
          loop := false
        | _ -> ()
      }

    let app : Types.WebPart =
      choose [
        Applicatives.path "/givemethemusic" >>= handShake giveMusic
        Applicatives.GET >>= choose [ Applicatives.path "/" >>= file "index.htm"; browseHome ];
        RequestErrors.NOT_FOUND "Found no handlers."
        ]

    let start () =
        let config = defaultConfig
        printfn "Starting on %d" config.bindings.Head.socketBinding.port
        startWebServer config app

[<EntryPoint>]
let main argv = 
    BrowserAutomation.start ()    
    let iframeRect = BrowserAutomation.iframeRect()
    let Async.StartAsTask (Main.loop [] iframeRect)
    WebServer.start()
    0
