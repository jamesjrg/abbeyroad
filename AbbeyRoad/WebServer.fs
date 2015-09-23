module AbbeyRoad.WebServer

open Suave
open Suave.Http
open Suave.WebSocket
open Suave.Web
open Suave.Http.Files
open Suave.Sockets.Control
open Newtonsoft.Json
open Microsoft.FSharp.Reflection
open System.Text
open System.IO

(* //FIXME this doesn't even try to handle either threading issues or people disconnecting *)
let mutable clients = []

let giveMusic (webSocket : WebSocket) =
    fun cx -> socket {
        clients <- webSocket :: clients
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

let getUnionCaseName (e:'a) = ( FSharpValue.GetUnionFields(e, typeof<'a>) |> fst ).Name

let broadcastNotes (onPressNotes:Types.Key seq) (heldNotes:Types.Key seq) =
    let data = Map.ofSeq [("onPressNotes", Seq.map getUnionCaseName onPressNotes); ("heldNotes", Seq.map getUnionCaseName heldNotes)]
    let json = JsonConvert.SerializeObject(data)
    let sends = clients |> Seq.map (fun client -> client.send Text (Encoding.UTF8.GetBytes json) true)
    Async.Parallel sends

let app : Types.WebPart =
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
