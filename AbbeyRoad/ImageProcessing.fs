module AbbeyRoad.ImageProcessing

open AbbeyRoad.Types
open Microsoft.FSharp.Reflection
open OpenCvSharp 
open System

(*
    Image processing code using OpenCV
*)

let GRAYSCALE_MODE = true
let IMAGE_READ_MODE = if GRAYSCALE_MODE then ImreadModes.GrayScale else ImreadModes.Color
let MAT_TYPE = if GRAYSCALE_MODE then MatType.CV_8UC1 else MatType.CV_8UC3
let THRESHOLD = 130.

type KeyPolygon = {
    Label: Key
    TopLeft: Point
    TopRight: Point
    BottomRight: Point
    BottomLeft: Point }

(*
These are inclusive coordinates
FWIW OpenCV exludes second range value for matrix operations which FWIW isn't currently accounted for, but these are only approximate anyway
*)
let coordsTopAndBottom =
    [|
        (Point(310, 200), Point(275, 214))
        (Point(332, 200), Point(296, 214))
        (Point(347, 201), Point(316, 216))
        (Point(361, 200), Point(329, 217))
        (Point(379, 202), Point(348, 217))
        (Point(394, 202), Point(364, 218))
        (Point(410, 203), Point(383, 218))
        (Point(423, 204), Point(394, 219))
        (Point(439, 205), Point(417, 220))
        (Point(455, 205), Point(431, 221))
        (Point(473, 206), Point(451, 222))
        (Point(489, 207), Point(467, 223))
        (Point(506, 209), Point(489, 224))
        (Point(533, 209), Point(514, 228))
    |]

let keys =
    coordsTopAndBottom
    |> Seq.pairwise
    |> Seq.zip (FSharpType.GetUnionCases typeof<Key>)
    |> Seq.map (fun (label, ((topLeft, bottomLeft), (topRight, bottomRight))) ->
        {   Label = FSharpValue.MakeUnion(label, [||]) :?> Key;
            TopLeft = topLeft;
            TopRight = topRight;
            BottomRight = bottomRight;
            BottomLeft = bottomLeft })
    |> Array.ofSeq
     
let cropWebcamImage (screenshotBytes:byte[]) (iframeRect:System.Drawing.Rectangle) =
    use uncropped = Mat.FromImageData(screenshotBytes, IMAGE_READ_MODE)
    uncropped.SubMat(Rect(iframeRect.Left, iframeRect.Top, iframeRect.Width, iframeRect.Height))

let createMaskImage (key:KeyPolygon) = 
    let mask =
        Mat.Zeros(
            key.BottomRight.Y - key.TopLeft.Y,
            key.TopRight.X - key.BottomLeft.X,
            MAT_TYPE).ToMat()
    let contours =
        [key.TopLeft; key.TopRight; key.BottomRight; key.BottomLeft]
        |> Seq.map (fun p -> Point(p.X - key.BottomLeft.X, p.Y - key.TopLeft.Y))
    mask.DrawContours(
            [contours] :> Collections.Generic.IEnumerable<Collections.Generic.IEnumerable<Point>>,
            -1,
            Scalar(255., 255., 255.),
            -1)
    mask

let thresholdImageForKey (image:Mat) (key:KeyPolygon) =
    use regionOfInterest = image.SubMat(key.TopLeft.Y, key.BottomRight.Y, key.BottomLeft.X, key.TopRight.X)
    let output = OutputArray.Create(new Mat())
    Cv2.Threshold(InputArray.Create(regionOfInterest), output, THRESHOLD, 255., ThresholdTypes.BinaryInv) |> ignore
    output.GetMat()

//assumes polygons slopes down from right to left
let createMaskedPolygon (wholeImage:Mat) (key:KeyPolygon) =
    let mask = createMaskImage key
    use regionOfInterest = wholeImage.SubMat(key.TopLeft.Y, key.BottomRight.Y, key.BottomLeft.X, key.TopRight.X)
    Cv2.BitwiseAnd(InputArray.Create(mask), InputArray.Create(regionOfInterest), OutputArray.Create(mask))
    mask

let findChangedKeyImages
    (previousKeyImages : Mat [])
    (newKeyImages : Mat []) =
    
        Seq.zip3 previousKeyImages newKeyImages keys
        |> Seq.choose (fun (prev, curr, key) -> 
            let output = Mat()
            Cv2.Absdiff(InputArray.Create(prev), InputArray.Create(curr), OutputArray.Create(output))

            let sum =
                seq {
                    for row in 0..output.Rows do
                    for column in 0..output.Cols do
                    yield output.Get<byte>(row, column) |> int }
                |> Seq.sum

            if sum > 4000 then
                Some (key.Label, sum)
            else
                None)

module DevTools =
    open System.IO
    open System.Windows.Forms

    let timeFunction f =
        let stopWatch = System.Diagnostics.Stopwatch.StartNew()
        for x in 1..100000 do
            f () |> ignore
        stopWatch.Stop()
        printfn "Elapsed ms: %f" stopWatch.Elapsed.TotalMilliseconds

    let testImageNames = [
        "empty-rain"
        "empty-cloudy-day"
        "people-by-day-1"
        "people-by-day-2"
        "van-day"
    ]

    let loadTestImages () = 
        testImageNames
        |> Seq.map (fun name ->
            let filename = sprintf @"C:\repos\oss\abbeyroad\images\%s.png" name
            let mat = new Mat(filename, IMAGE_READ_MODE)
            (name, mat))
        |> Map.ofSeq

    let roisForImage imageName =
        let image = loadTestImages().[imageName]
        keys
        |> Seq.map (fun key ->
            key.Label, image.SubMat(key.TopLeft.Y, key.BottomRight.Y, key.BottomLeft.X, key.TopRight.X))
        |> Map.ofSeq

    let thresholdImagesForImage imageName =
        let image = loadTestImages().[imageName]
        keys
        |> Seq.map (fun key -> key.Label, thresholdImageForKey image key)
        |> Map.ofSeq

    let maskImagesForImage imageName =
        let image = loadTestImages().[imageName]
        keys |> Seq.map (fun key -> key.Label, createMaskedPolygon image key) |> Map.ofSeq

    let saveMatAsFile (mat:Mat) =
        let path = Path.Combine(@"C:\Users\James\Documents\tmp\abbeyroad", DateTime.Now.ToString("MM-dd-HH-mm-ss") + ".png")
        mat.SaveImage(path)

    let showMatInWinForm mat =
        let pictureBox =
            new OpenCvSharp.UserInterface.PictureBoxIpl (
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.CenterImage,
                ImageIpl = mat )
        let form = new Form(Width = 800, Height = 600)
        form.Controls.Add(pictureBox)
        form.Show()

    //set current directory for loading native dlls, assuming option set to run fsi in 64 bit mode
    let makeItWorkInFsi() = 
        System.Environment.CurrentDirectory <- @"C:\repos\oss\abbeyroad\AbbeyRoad\bin\Debug\dll\x64"

    let printMat (mat:Mat) dims =
        for r in 0..dims - 1 do
            for g in 0..dims - 1 do
                for b in 0..dims - 1 do
                    let x = mat.Get<float32>(b, g, r)
                    if x > 0.f then printf "%d %d %d: %fd\n" r g b x

    let getRandomKeys () =
        let rnd = System.Random()
        List.init (rnd.Next(8)) (fun _ ->
            let unionCaseInfo = FSharpType.GetUnionCases typeof<Key>
            FSharpValue.MakeUnion(unionCaseInfo.[(rnd.Next(unionCaseInfo.Length))], [||]) :?> Key)

let getActiveKeys
    (screenshot : byte[])
    (iframeRect : System.Drawing.Rectangle)
    (previousKeyImages : Mat[]) =

    use webcamImage = cropWebcamImage screenshot iframeRect
    let keyImages = keys |> Array.map (fun key -> createMaskedPolygon webcamImage key)

    let activeKeys =
        if previousKeyImages.Length > 0 then
            findChangedKeyImages keyImages previousKeyImages |> List.ofSeq
        else
            []

    //vague attempt to avoid spamming with cars and crowds of people
    let activeKeys = if activeKeys.Length < 4 then activeKeys |> List.map fst else []
    activeKeys, keyImages
