namespace AbbeyRoad

open AbbeyRoad

module Main =
    [<Literal>]
    let framePeriod = 20

    let rec loop previousActiveKeys iframeRect previousKeyImages = async {
        let screenshot = BrowserAutomation.screenshot()
        let activeKeys, newKeyImages = ImageProcessing.getActiveKeys screenshot iframeRect previousKeyImages
        let onPressKeys = Set(activeKeys) - Set(previousActiveKeys)
        WebServer.broadcastKeys onPressKeys activeKeys |> Async.RunSynchronously |> ignore        
        do! Async.Sleep(framePeriod)
        return! loop activeKeys iframeRect newKeyImages
    }

    let startCapturingImages() =
        BrowserAutomation.start ()
        let iframeRect = BrowserAutomation.iframeRect()
        Async.StartAsTask (loop [] iframeRect [||]) |> ignore

    [<EntryPoint>]
    let main argv = 
        startCapturingImages ()
        WebServer.start ()
        0
