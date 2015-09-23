module AbbeyRoad.WebServer

open Suave
open Suave.Http
open Suave.Sockets
open Suave.WebSocket
open Suave.Web
open Suave.Http.Files
open Suave.Sockets.Control

let giveMusic (webSocket : WebSocket) =
    fun cx -> socket {
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

let broadcastNotes onPressNotes heldNotes =
    ()

let app : Types.WebPart =
    choose [
    Applicatives.path "/givemethemusic" >>= handShake giveMusic
    Applicatives.GET >>= choose [ Applicatives.path "/" >>= file "web/index.htm"; browse "web" ];
    RequestErrors.NOT_FOUND "Found no handlers."
    ]

let start () =
    let config = defaultConfig
    startWebServer config app
