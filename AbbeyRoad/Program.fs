module AbbeyRoad.Program

open canopy
open System
open OpenQA.Selenium
open OpenCV.Net
open System.IO

(*
--> code to get screenshot bytes, decode, grayscale, then re-encode and save as jpeg is not working

--> once that works, need to create unpressed note images:
- by hand work out x y coords for each polygon for each note, then create example images for each bar from a screenshot of empty crossing, or maybe programatically create black and white image using only coords from real image

-> then extract polygons of interest
--> either ROI or sub rectangles or deep copied sub rectangles, extract box containing each white bar
--- select polygons within each rectangle
--> possibly by creating black mask image, then filling white contour polygon on region of interest, then or-ing with original rectangle
- use some sort of built-in OpenCV function to compare each polygon within frame to each example polygon - maybe just euclidian distance?

-> play sound matching pressed note

-> finally add loop to take screenshots and analyse continuously

*)

let flashElement () =
    element ".earthcam-embed-container"

let cropFrame (uncropped:Mat) =
    let iframe = element "iframe"
    let width = iframe.Size.Width
    let height = iframe.Size.Height
    let x = iframe.Location.X
    let y = iframe.Location.Y

    uncropped.GetSubRect(new Rect(x, y, width, height))

let getFrame() =
    let screenShotBytes = (browser :?> ITakesScreenshot).GetScreenshot().AsByteArray    
    let buffer = Mat.FromArray(screenShotBytes)
    let uncropped = CV.DecodeImageM(buffer, LoadImageFlags.Grayscale)
    cropFrame uncropped  

let saveImage mat =
    let path = Path.Combine(@"C:\Users\James\Documents\tmp", DateTime.Now.ToString("MM-dd-HH-mm-ss") + ".png")
    CV.SaveImage(path, mat)

let fullScreenshot() =
    screenshot @"C:\Users\James\Documents\tmp" (DateTime.Now.ToString("MM-dd-HH-mm-ss")) |> ignore

let go () =
    start firefox

    url "http://www.abbeyroad.com/crossing"

    //sleep 2

    let frame = getFrame()
    saveImage frame   

let loadImage =
    (*Mat image = imread("src_image_path");*)
    0

let selectRectangle x y width height =
    (*
    Rect roi = Rect(x, y, w, h); Mat image_roi = image(roi);*)
    0

let selectPolygon =
    //
    0

[<EntryPoint>]
let main argv = 
    go()
    0
