module AbbeyRoad.WebServer

open Suave
open Suave.Http
open Suave.WebSocket
open Suave.Web
open Suave.Http.Files
open Suave.Sockets.Control
open Newtonsoft.Json
open Microsoft.FSharp.Reflection
open System
open System.Text
open System.IO

(*
The web server for sending pressed keys over web sockets as well as serving the static web content

//FIXME:
- might need to do more than just assess failed reads to handle web sockets disconnecting
- could use a data structure with quicker removal to store clients
*)

let monitor = new Object()
let mutable clients = []

let removeClient webSocket = 
    lock monitor (fun () -> clients <- List.filter (fun x -> not <| x.Equals(webSocket)) clients)

let giveMusic (webSocket : WebSocket) =
    fun cx -> socket {
        lock monitor (fun () -> clients <- webSocket :: clients)
        let loop = ref true
        while !loop do
        let! msg = webSocket.read()
        match msg with
        | (Text, data, true) ->
            ()
        | (Ping, _, _) ->
            do! webSocket.send Pong [||] true
        | (Close, _, _) ->
            do! webSocket.send Close [||] true
            removeClient webSocket
            loop := false
        | _ -> ()
    }

let getUnionCaseName (e:'a) = ( FSharpValue.GetUnionFields(e, typeof<'a>) |> fst ).Name

let broadcastKeys (newKeys:Types.Key seq) (heldKeys:Types.Key seq) =
    let data = Map.ofSeq [("newKeys", Seq.map getUnionCaseName newKeys); ("heldKeys", Seq.map getUnionCaseName heldKeys)]
    let json = JsonConvert.SerializeObject(data)
    let sends = clients |> Seq.map (fun client -> async {
        let! writeResult = client.send Text (Encoding.UTF8.GetBytes json) true
        match writeResult with
        | Choice2Of2 error ->
            printf "client lost with error: %A" error
            removeClient(client)
        | _ -> () })
    Async.Parallel sends

let app : Types.WebPart =
    //unfortunately Suave's "browse" function to serve static files doesn't support a relative base path
    let staticFilesPath =
        Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            "web")
    choose [
        Applicatives.path "/givemethemusic" >>= handShake giveMusic
        Applicatives.GET >>= choose [ Applicatives.path "/" >>= file "web/index.htm"; browse staticFilesPath ];
        RequestErrors.NOT_FOUND "Found no handlers."
    ]

let start () =
    let config = defaultConfig
    startWebServer config app
