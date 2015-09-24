namespace AbbeyRoad

open AbbeyRoad

module Main =
    [<Literal>]
    let framePeriod = 100

    let rec loop previousActiveKeys iframeRect = async {
        let screenshot = BrowserAutomation.screenshot()
        let activeKeys = ImageProcessing.getActiveKeys screenshot iframeRect
        let onPressKeys = Set(activeKeys) - Set(previousActiveKeys)
        WebServer.broadcastKeys onPressKeys activeKeys |> Async.RunSynchronously |> ignore        
        do! Async.Sleep(framePeriod)
        return! loop activeKeys iframeRect
    }

    let startCapturingImages() =
        BrowserAutomation.start ()
        let iframeRect = BrowserAutomation.iframeRect()
        Async.StartAsTask (loop [] iframeRect) |> ignore

    [<EntryPoint>]
    let main argv = 
        startCapturingImages ()
        WebServer.start ()
        0
