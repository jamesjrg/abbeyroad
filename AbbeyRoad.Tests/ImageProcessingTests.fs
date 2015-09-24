module AbbeyRoad.Tests.ImageProcessingTests

open Fuchu
open Swensen.Unquote.Assertions

open AbbeyRoad.Types
open AbbeyRoad.ImageProcessing
open OpenCvSharp 

[<Tests>]
let tests =
    testList "ImageProcessing tests"
        [
            testCase "when all pixels are one color, calculates RGB entropy" (fun () -> 
                let key = keys.[0]                
                let width = key.TopRight.X - key.BottomLeft.X
                let height = key.BottomRight.Y - key.TopLeft.Y
                let blackMat = Mat.Zeros(width, height, MatType.CV_8UC3).ToMat()
                let hist = getHistogram blackMat 16
                let actual = Entropy.calcRgbHistogramEntropy hist 16 key
                0. =! actual)

            testCase "when pixels evenly distributed between colors, calculates RGB entropy" (fun () -> 
                0. =! 0.)
        ]