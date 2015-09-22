namespace AbbeyRoad

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

module WinForms =
    open System.Windows.Forms

    let showMatInWinForm mat =
        let pictureBox =
            new OpenCvSharp.UserInterface.PictureBoxIpl (
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.CenterImage,
                ImageIpl = mat )
        let form = new Form(Width = 800, Height = 600)
        form.Controls.Add(pictureBox)
        System.Windows.Forms.Application.Run(form)

//FIXME lots of OpenCV types are IDisposable, possibly there are lots of memory leaks at the moment
module ImageProcessing =   
    open System.IO
    open Microsoft.FSharp.Reflection
    open OpenCvSharp    

    let saveMatAsFile (mat:Mat) =
        let path = Path.Combine(@"C:\Users\James\Documents\tmp", DateTime.Now.ToString("MM-dd-HH-mm-ss") + ".png")
        mat.SaveImage(path)

    type Key = { Label: Note; Points: Point[] }

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
            (Point(439, 205), Point(417, 200))
            (Point(455, 205), Point(431, 221))
            (Point(473, 206), Point(451, 222))
            (Point(489, 207), Point(467, 223))
            (Point(506, 209), Point(489, 224))
            (Point(533, 209), Point(514, 228))
        |]

    let keys =
        coordsTopAndBottom
        |> Seq.pairwise
        |> Seq.zip (FSharpType.GetUnionCases typeof<Note>)
        |> Seq.map (fun (label, ((topRight, bottomRight), (topLeft, bottomLeft))) ->
            { Label = FSharpValue.MakeUnion(label, [||]) :?> Note; Points = [| topLeft; topRight; bottomRight; bottomLeft |] })
      
    let cropWebcamImage (screenshotBytes:byte[]) (iframeRect:System.Drawing.Rectangle) =
        let uncropped = Mat.FromImageData(screenshotBytes, ImreadModes.GrayScale)
        uncropped.SubMat(Rect(iframeRect.Left, iframeRect.Top, iframeRect.Width, iframeRect.Height))

    //assumes polygons slopes down from left to right
    let createMaskImage (topLeft:Point) (topRight:Point) (bottomLeft:Point) (bottomRight:Point) =
        let contours = new Collections.Generic.List<Collections.Generic.IEnumerable<Point>>()
        let mask =
            Mat.Zeros(
                topLeft.Y - bottomRight.Y,
                bottomRight.X - topLeft.X,
                MatType.CV_8UC3).ToMat()
        mask.DrawContours(
                contours :> Collections.Generic.IEnumerable<Collections.Generic.IEnumerable<Point>>,
                -1,
                Scalar(255., 255., 255.),
                -1)
        mask

    let combineWithMaskImage (wholeImage:Mat) topLeft topRight bottomLeft bottomRight =
        use mask = createMaskImage topLeft topRight bottomLeft bottomRight
        let regionOfInterest = wholeImage.SubMat(Rect())
        Cv2.BitwiseAnd(InputArray.Create(mask), InputArray.Create(regionOfInterest), OutputArray.Create(mask))

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
    let getActivePolygons (screenshot:byte[]) (iframeRect:System.Drawing.Rectangle) =
        let webcamImage = cropWebcamImage screenshot iframeRect
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

    let startCapturingImages() =
        BrowserAutomation.start ()
        let iframeRect = BrowserAutomation.iframeRect()
        Async.StartAsTask (loop [] iframeRect) |> ignore

    let testing () =
        BrowserAutomation.start ()
        let iframeRect = BrowserAutomation.iframeRect()
        let screenshot = BrowserAutomation.screenshot()
        let webcamImage = ImageProcessing.cropWebcamImage screenshot iframeRect
        WinForms.showMatInWinForm(webcamImage)
        OpenCvSharp.Cv2.WaitKey();

    [<EntryPoint>]
    let main argv = 
        testing() |> ignore
        (*
        startCapturingImages ()
        WebServer.start ()
        *)
        0