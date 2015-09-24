namespace AbbeyRoad

open AbbeyRoad

module Main =
    [<Literal>]
    let framePeriod = 100

    let rec loop previousActiveKeys iframeRect = async {
        let screenshot = BrowserAutomation.screenshot()
        let activeKeys = ImageProcessing.getActiveKeys screenshot iframeRect
        let onPressKeys = Set(activeKeys) - Set(previousActiveKeys)
        let! writeResults = WebServer.broadcastKeys onPressKeys activeKeys
        do! Async.Sleep(framePeriod)
        return! loop activeKeys iframeRect
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
        let x = ImageProcessing.createMaskedPolygon webcamImage ImageProcessing.keys.[0]
        ImageProcessing.DevTools.showMatInWinForm(x)        

    [<EntryPoint>]
    let main argv = 
        //testing() |> ignore
        //startCapturingImages ()
        WebServer.start ()
        0
