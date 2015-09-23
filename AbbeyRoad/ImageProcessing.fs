module AbbeyRoad.ImageProcessing

open AbbeyRoad.Types
open Microsoft.FSharp.Reflection
open OpenCvSharp 
open System

//FIXME lots of OpenCV types are IDisposable, possibly eats memory at the moment

module DevTools =
    open System.IO
    open System.Windows.Forms
    open System.Runtime.InteropServices

    let loadEmptyCrossingImage() = 
        new Mat(@"C:\repos\oss\abbeyroad\images\empty-crossing-rain.png", ImreadModes.GrayScale)

    let saveMatAsFile (mat:Mat) =
        let path = Path.Combine(@"C:\Users\James\Documents\tmp", DateTime.Now.ToString("MM-dd-HH-mm-ss") + ".png")
        mat.SaveImage(path)

    let showMatInWinForm mat =
        let pictureBox =
            new OpenCvSharp.UserInterface.PictureBoxIpl (
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.CenterImage,
                ImageIpl = mat )
        let form = new Form(Width = 800, Height = 600)
        form.Controls.Add(pictureBox)
        System.Windows.Forms.Application.Run(form)

    [<DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
    extern IntPtr LoadLibrary(string lpFileName);

    let makeItWorkInFsi() =
        //NB: WindowsLibraryLoader.cs in OpenCVSharp source attempts to load some dlls explictly in static constructor, looking in
        //executingAssembly.Location \ dll \ (x86|x64)
        //however I think this is irrelevant because the main extern dll is just loaded normally using dllimport
        //the load library line shouldn't be necessary because Windows should automatically load required dlls from current path

        System.Environment.CurrentDirectory <- @"C:\repos\oss\abbeyroad\AbbeyRoad\bin\Debug\dll\x64"        
        LoadLibrary(@"C:\repos\oss\abbeyroad\packages\OpenCvSharp3-AnyCPU\NativeDlls\x64\OpenCvSharpExtern.dll") |> ignore

type KeyPolygon = { Label: Key; TopLeft: Point; TopRight: Point; BottomRight: Point; BottomLeft: Point }

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
    use uncropped = Mat.FromImageData(screenshotBytes, ImreadModes.GrayScale)
    uncropped.SubMat(Rect(iframeRect.Left, iframeRect.Top, iframeRect.Width, iframeRect.Height))

let createMaskImage (key:KeyPolygon) = 
    let mask =
        Mat.Zeros(
            key.BottomRight.Y - key.TopLeft.Y,
            key.TopRight.X - key.BottomLeft.X,
            MatType.CV_8UC1).ToMat()
    let contours =
        [key.TopLeft; key.TopRight; key.BottomRight; key.BottomLeft]
        |> Seq.map (fun p -> Point(p.X - key.BottomLeft.X, p.Y - key.TopLeft.Y))
    mask.DrawContours(
            [contours] :> Collections.Generic.IEnumerable<Collections.Generic.IEnumerable<Point>>,
            -1,
            Scalar(255., 255., 255.),
            -1)
    mask

//assumes polygons slopes down from right to left
let createMaskedPolygon (wholeImage:Mat) (key:KeyPolygon) =
    let mask = createMaskImage key
    use regionOfInterest = wholeImage.SubMat(key.TopLeft.Y, key.BottomRight.Y, key.BottomLeft.X, key.TopRight.X)
    Cv2.BitwiseAnd(InputArray.Create(mask), InputArray.Create(regionOfInterest), OutputArray.Create(mask))
    mask

let euclidianDistance () =
    ()

let histogramEntropy () =
    ()

let compareHistograms (empty:Mat) (actual:Mat) comparisonMethod =
    Cv2.CompareHist(InputArray.Create(empty), InputArray.Create(actual), comparisonMethod)

let getHistogram (src:Mat) =
    let hist = OutputArray.Create(new Mat());
    let hdims = [|256|]; // Histogram size for each dimension
    let ranges = [| new Rangef(0.f,256.f) |]; // min/max 
    Cv2.CalcHist(
        [|src|],
        [|0|],
        null,
        hist,
        1,
        hdims,
        ranges)

(*
different comparison ideas:
1. simple euclidian distance
2. simple histogram comparison (4 different comparisons included in OpenCV) - though very sensitive to lighting changes
3. histogram comparison, combined with adjusting brightness of example empty images to match current time of day & weather
4. don't compare to example images at all, but rather just test entropy of histogram for each polygon - high entropy means the 
plain colour of the background is likely mixed up with an object on top of it
*)
let getActiveKeys (screenshot:byte[]) (iframeRect:System.Drawing.Rectangle) =
    use webcamImage = cropWebcamImage screenshot iframeRect
    let rnd = System.Random()
    List.init (rnd.Next(8)) (fun _ ->
        let unionCaseInfo = FSharpType.GetUnionCases typeof<Key>
        FSharpValue.MakeUnion(unionCaseInfo.[(rnd.Next(6))], [||]) :?> Key)